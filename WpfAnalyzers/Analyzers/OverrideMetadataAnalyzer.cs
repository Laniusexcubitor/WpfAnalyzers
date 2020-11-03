﻿namespace WpfAnalyzers
{
    using System.Collections.Immutable;

    using Gu.Roslyn.AnalyzerExtensions;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class OverrideMetadataAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(
            Descriptors.WPF0017MetadataMustBeAssignable,
            Descriptors.WPF0018DefaultStyleKeyPropertyOverrideMetadataArgument);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(x => Handle(x), SyntaxKind.InvocationExpression);
        }

        private static void Handle(SyntaxNodeAnalysisContext context)
        {
            if (!context.IsExcludedFromAnalysis() &&
                context.Node is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Expression: { } expression } } invocation &&
                context.ContainingSymbol is { } &&
                DependencyProperty.TryGetOverrideMetadataCall(invocation, context.SemanticModel, context.CancellationToken, out var method) &&
                context.SemanticModel.TryGetSymbol(expression, context.CancellationToken, out var candidate) &&
                BackingFieldOrProperty.TryCreateForDependencyProperty(candidate, out var fieldOrProperty) &&
                method.TryFindParameter(KnownSymbols.PropertyMetadata, out var parameter) &&
                invocation.TryFindArgument(parameter, out var metadataArg))
            {
                if (fieldOrProperty.TryGetAssignedValue(context.CancellationToken, out var value) &&
                    value is InvocationExpressionSyntax registerInvocation)
                {
                    if (RegisterInvocation.TryMatchRegister(registerInvocation, context.SemanticModel, context.CancellationToken, out var call) ||
                        RegisterInvocation.TryMatchRegisterReadOnly(registerInvocation, context.SemanticModel, context.CancellationToken, out call) ||
                        RegisterInvocation.TryMatchRegisterAttached(registerInvocation, context.SemanticModel, context.CancellationToken, out call) ||
                        RegisterInvocation.TryMatchRegisterAttachedReadOnly(registerInvocation, context.SemanticModel, context.CancellationToken, out call))
                    {
                        if (call.FindArgument(KnownSymbols.PropertyMetadata) is { } registeredMetadataArg &&
                            context.SemanticModel.TryGetType(metadataArg.Expression, context.CancellationToken, out var type) &&
                            context.SemanticModel.TryGetType(registeredMetadataArg.Expression, context.CancellationToken, out var registeredType) &&
                            !type.IsAssignableTo(registeredType, context.SemanticModel.Compilation))
                        {
                            context.ReportDiagnostic(Diagnostic.Create(Descriptors.WPF0017MetadataMustBeAssignable, metadataArg.GetLocation()));
                        }
                    }
                }
                else if (fieldOrProperty.Symbol == KnownSymbols.FrameworkElement.DefaultStyleKeyProperty &&
                         metadataArg.Expression is ObjectCreationExpressionSyntax metadataCreation)
                {
                    if (!context.SemanticModel.TryGetSymbol(metadataCreation, KnownSymbols.FrameworkPropertyMetadata, context.CancellationToken, out _))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(Descriptors.WPF0017MetadataMustBeAssignable, metadataArg.GetLocation()));
                    }
                    else if (metadataCreation.TrySingleArgument(out var typeArg) &&
                             typeArg is { Expression: TypeOfExpressionSyntax { Type: IdentifierNameSyntax { Identifier: { } identifier } } } &&
                             identifier.ValueText != context.ContainingSymbol.ContainingType.MetadataName)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(Descriptors.WPF0018DefaultStyleKeyPropertyOverrideMetadataArgument, typeArg.GetLocation()));
                    }
                }
            }
        }
    }
}
