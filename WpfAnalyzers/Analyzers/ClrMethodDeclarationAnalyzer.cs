﻿namespace WpfAnalyzers
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Diagnostics.CodeAnalysis;
    using System.Threading;
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class ClrMethodDeclarationAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(
            Descriptors.WPF0004ClrMethodShouldMatchRegisteredName,
            Descriptors.WPF0013ClrMethodMustMatchRegisteredType,
            Descriptors.WPF0033UseAttachedPropertyBrowsableForTypeAttribute,
            Descriptors.WPF0034AttachedPropertyBrowsableForTypeAttributeArgument,
            Descriptors.WPF0042AvoidSideEffectsInClrAccessors,
            Descriptors.WPF0061DocumentClrMethod);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(x => Handle(x), SyntaxKind.MethodDeclaration);
        }

        private static void Handle(SyntaxNodeAnalysisContext context)
        {
            if (!context.IsExcludedFromAnalysis() &&
                context.Node is MethodDeclarationSyntax methodDeclaration &&
                context.ContainingSymbol is IMethodSymbol { IsStatic: true } method &&
                method.Parameters.TryElementAt(0, out var element) &&
                element.Type.IsAssignableTo(KnownSymbols.DependencyObject, context.Compilation))
            {
                if (ClrMethod.IsAttachedGet(methodDeclaration, context.SemanticModel, context.CancellationToken, out var getValueCall, out var backing))
                {
                    if (DependencyProperty.TryGetRegisteredName(backing, context.SemanticModel, context.CancellationToken, out _, out var registeredName))
                    {
                        if (!method.Name.IsParts("Get", registeredName))
                        {
                            context.ReportDiagnostic(
                                Diagnostic.Create(
                                    Descriptors.WPF0004ClrMethodShouldMatchRegisteredName,
                                    methodDeclaration.Identifier.GetLocation(),
                                    ImmutableDictionary<string, string>.Empty.Add("ExpectedName", "Get" + registeredName),
                                    method.Name,
                                    "Get" + registeredName));
                        }

                        if (method.DeclaredAccessibility.IsEither(Accessibility.Protected, Accessibility.Internal, Accessibility.Public))
                        {
                            var summaryFormat = "<summary>Helper for getting <see cref=\"{BACKING}\"/> from <paramref name=\"{PARAMETER}\"/>.</summary>";
                            var paramFormat = "<param name=\"{PARAMETER}\"><see cref=\"{PARAMETER_TYPE}\"/> to read <see cref=\"{BACKING}\"/> from.</param>";
                            var returnsFormat = "<returns>{REGISTERED_NAME} property value.</returns>";
                            if (methodDeclaration.TryGetDocumentationComment(out var comment))
                            {
                                if (comment.VerifySummary(summaryFormat, backing.Symbol.Name, element.Name) is { } summaryError)
                                {
                                    context.ReportDiagnostic(
                                        Diagnostic.Create(
                                            Descriptors.WPF0061DocumentClrMethod,
                                            summaryError.Location,
                                            ImmutableDictionary<string, string>.Empty.Add(nameof(DocComment), summaryError.Text)));
                                }

                                if (comment.VerifyParameter(paramFormat, element, element.Type.ToCrefType(), backing.Symbol.Name) is { } paramError)
                                {
                                    context.ReportDiagnostic(
                                        Diagnostic.Create(
                                            Descriptors.WPF0061DocumentClrMethod,
                                            paramError.Location,
                                            ImmutableDictionary<string, string>.Empty.Add(nameof(DocComment), paramError.Text)));
                                }

                                if (comment.VerifyReturns(returnsFormat, registeredName) is { } returnsError)
                                {
                                    context.ReportDiagnostic(
                                        Diagnostic.Create(
                                            Descriptors.WPF0061DocumentClrMethod,
                                            returnsError.Location,
                                            ImmutableDictionary<string, string>.Empty.Add(nameof(DocComment), returnsError.Text)));
                                }
                            }
                            else
                            {
                                context.ReportDiagnostic(
                                    Diagnostic.Create(
                                        Descriptors.WPF0061DocumentClrMethod,
                                        methodDeclaration.Identifier.GetLocation(),
                                        ImmutableDictionary<string, string>.Empty.Add(
                                            nameof(DocComment),
                                            $"/// {DocComment.Format(summaryFormat, backing.Symbol.Name, element.Name)}\n" +
                                            $"/// {DocComment.Format(paramFormat, element.Name, element.Type.ToCrefType(), backing.Name)}\n" +
                                            $"/// {DocComment.Format(returnsFormat, registeredName)}\n")));
                            }
                        }
                    }

                    if (DependencyProperty.TryGetRegisteredType(backing, context.SemanticModel, context.CancellationToken, out var registeredType) &&
                        !Equals(method.ReturnType, registeredType))
                    {
                        context.ReportDiagnostic(
                            Diagnostic.Create(
                                Descriptors.WPF0013ClrMethodMustMatchRegisteredType,
                                methodDeclaration.ReturnType.GetLocation(),
                                "Return type",
                                registeredType));
                    }

                    if (Gu.Roslyn.AnalyzerExtensions.Attribute.TryFind(methodDeclaration, KnownSymbols.AttachedPropertyBrowsableForTypeAttribute, context.SemanticModel, context.CancellationToken, out var attribute))
                    {
                        if (attribute.TrySingleArgument(out var argument) &&
                            argument.Expression is TypeOfExpressionSyntax typeOf &&
                            TypeOf.TryGetType(typeOf, method.ContainingType, context.SemanticModel, context.CancellationToken, out var argumentType) &&
                            !argumentType.IsAssignableTo(element.Type, context.Compilation))
                        {
                            context.ReportDiagnostic(
                                Diagnostic.Create(
                                    Descriptors.WPF0034AttachedPropertyBrowsableForTypeAttributeArgument,
                                    argument.GetLocation(),
                                    element.Type.ToMinimalDisplayString(
                                        context.SemanticModel,
                                        argument.SpanStart)));
                        }
                    }
                    else
                    {
                        context.ReportDiagnostic(
                            Diagnostic.Create(
                                    Descriptors.WPF0033UseAttachedPropertyBrowsableForTypeAttribute,
                                    methodDeclaration.Identifier.GetLocation(),
                                    element.Type.ToMinimalDisplayString(context.SemanticModel, methodDeclaration.SpanStart)));
                    }

                    if (methodDeclaration.Body is { } body &&
                        TryGetSideEffect(body, getValueCall, out var sideEffect))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(Descriptors.WPF0042AvoidSideEffectsInClrAccessors, sideEffect.GetLocation()));
                    }
                }
                else if (ClrMethod.IsAttachedSet(methodDeclaration, context.SemanticModel, context.CancellationToken, out var setValueCall, out backing))
                {
                    if (DependencyProperty.TryGetRegisteredName(backing, context.SemanticModel, context.CancellationToken, out _, out var registeredName))
                    {
                        if (!method.Name.IsParts("Set", registeredName))
                        {
                            context.ReportDiagnostic(
                                Diagnostic.Create(
                                    Descriptors.WPF0004ClrMethodShouldMatchRegisteredName,
                                    methodDeclaration.Identifier.GetLocation(),
                                    ImmutableDictionary<string, string>.Empty.Add("ExpectedName", "Set" + registeredName),
                                    method.Name,
                                    "Set" + registeredName));
                        }

                        if (method.DeclaredAccessibility.IsEither(Accessibility.Protected, Accessibility.Internal, Accessibility.Public))
                        {
                            foreach (var (location, text) in new SetDocumentationErrors(methodDeclaration, backing, registeredName))
                            {
                                context.ReportDiagnostic(
                                    Diagnostic.Create(
                                        Descriptors.WPF0061DocumentClrMethod,
                                        location,
                                        ImmutableDictionary<string, string>.Empty.Add(nameof(DocComment), text)));
                            }
                        }
                    }

                    if (DependencyProperty.TryGetRegisteredType(backing, context.SemanticModel, context.CancellationToken, out var registeredType) &&
                        method.Parameters.TryElementAt(1, out var valueParameter) &&
                        !Equals(valueParameter.Type, registeredType))
                    {
                        context.ReportDiagnostic(
                            Diagnostic.Create(
                                Descriptors.WPF0013ClrMethodMustMatchRegisteredType,
                                methodDeclaration.ParameterList.Parameters[1].Type.GetLocation(),
                                "Value type",
                                registeredType));
                    }

                    if (methodDeclaration.Body is { } body &&
                        TryGetSideEffect(body, setValueCall, out var sideEffect))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(Descriptors.WPF0042AvoidSideEffectsInClrAccessors, sideEffect.GetLocation()));
                    }
                }
            }
        }

        private static bool TryGetSideEffect(BlockSyntax body, InvocationExpressionSyntax getOrSet, [NotNullWhen(true)] out StatementSyntax? sideEffect)
        {
            foreach (var statement in body.Statements)
            {
                switch (statement)
                {
                    case ExpressionStatementSyntax { Expression: { } expression }
                        when expression == getOrSet:
                        continue;
                    case ReturnStatementSyntax { Expression: { } expression }
                        when expression == getOrSet:
                        continue;
                    case ReturnStatementSyntax { Expression: CastExpressionSyntax { Expression: { } expression } }
                        when expression == getOrSet:
                        continue;
                    case IfStatementSyntax { Condition: { } condition, Statement: ThrowStatementSyntax { }, Else: null }
                        when NullCheck.IsNullCheck(condition, null, CancellationToken.None, out _):
                        continue;
                    case IfStatementSyntax { Condition: { } condition, Statement: BlockSyntax { Statements: { Count: 0 } }, Else: null }
                        when NullCheck.IsNullCheck(condition, null, CancellationToken.None, out _):
                        continue;
                    case IfStatementSyntax { Condition: { } condition, Statement: BlockSyntax { Statements: { Count: 1 } statements }, Else: null }
                        when statements[0] is ThrowStatementSyntax &&
                             NullCheck.IsNullCheck(condition, null, CancellationToken.None, out _):
                        continue;
                    case IfStatementSyntax { Statement: null, Else: null }:
                        continue;
                    default:
                        sideEffect = statement;
                        return true;
                }
            }

            sideEffect = null;
            return false;
        }

        private struct SetDocumentationErrors : IEnumerable<(Location Location, string Text)>
        {
            private readonly MethodDeclarationSyntax method;
            private readonly BackingFieldOrProperty backing;
            private readonly string registeredName;

            internal SetDocumentationErrors(MethodDeclarationSyntax method, BackingFieldOrProperty backing, string registeredName)
            {
                this.method = method;
                this.backing = backing;
                this.registeredName = registeredName;
            }

            public IEnumerator<(Location Location, string Text)> GetEnumerator()
            {
                if (this.method.ParameterList is null ||
                    this.method.ParameterList.Parameters.Count != 2)
                {
                    yield break;
                }

                if (this.method.TryGetDocumentationComment(out var comment))
                {
                    var element = this.method.ParameterList.Parameters[0];
                    var value = this.method.ParameterList.Parameters[1];
                    if (comment.TryGetSummary(out var summary))
                    {
                        if (summary.TryMatch<XmlTextSyntax, XmlEmptyElementSyntax, XmlTextSyntax, XmlEmptyElementSyntax, XmlTextSyntax>(out var prefix, out var cref, out var middle, out var paramRef, out var suffix) &&
                            prefix.IsMatch("Helper for setting ") &&
                            middle.IsMatch(" on ") &&
                            suffix.IsMatch("."))
                        {
                            if (DocComment.VerifyCref(cref, this.backing.Name) is { } crefError)
                            {
                                yield return crefError;
                            }

                            if (DocComment.VerifyParamRef(paramRef, element) is { } paramRefError)
                            {
                                yield return paramRefError;
                            }
                        }
                        else
                        {
                            yield return (summary.GetLocation(), this.SummaryText());
                        }
                    }
                    else
                    {
                        yield return (comment.GetLocation(), this.SummaryText());
                    }
                }
                else if (this.FullText() is { } fullText)
                {
                    yield return (this.method.Identifier.GetLocation(), fullText);
                }
            }

            IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

            private string SummaryText() => $"<summary>Helper for setting <see cref=\"{this.backing.Name}\"/> on <paramref name=\"{this.method.ParameterList.Parameters[0].Identifier.ValueText}\"/>.</summary>";

            private string ElementInnerText(ParameterSyntax parameter) => $"<see cref=\"{parameter.Type}\"/> to set <see cref=\"{this.backing.Name}\"/> on.";

            private string ElementText(ParameterSyntax parameter) => $"<param name=\"{parameter.Identifier.ValueText}\">{this.ElementInnerText(parameter)}</param>";

            private string ValueInnerText() => $"{this.registeredName} property value.";

            private string ValueText(ParameterSyntax parameter) => $"<param name=\"{parameter.Identifier.ValueText}\">{this.ValueInnerText()}</param>";

            private string? FullText()
            {
                return StringBuilderPool.Borrow()
                                        .Append("/// ").AppendLine(this.SummaryText())
                                        .Append("/// ").AppendLine(this.ElementText(this.method.ParameterList.Parameters[0]))
                                        .Append("/// ").AppendLine(this.ValueText(this.method.ParameterList.Parameters[1]))
                                        .Return();
            }
        }
    }
}
