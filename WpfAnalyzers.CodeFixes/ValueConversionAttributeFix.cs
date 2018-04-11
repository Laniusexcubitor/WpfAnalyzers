namespace WpfAnalyzers
{
    using System.Collections.Immutable;
    using System.Composition;
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Editing;

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ValueConversionAttributeFix))]
    [Shared]
    internal class ValueConversionAttributeFix : DocumentEditorCodeFixProvider
    {
        private static readonly AttributeSyntax Attribute = SyntaxFactory
            .Attribute(SyntaxFactory.ParseName("System.Windows.Data.ValueConversionAttribute"))
            .WithSimplifiedNames();

        /// <inheritdoc/>
        public override ImmutableArray<string> FixableDiagnosticIds { get; } =
            ImmutableArray.Create(WPF0071ConverterDoesNotHaveAttribute.DiagnosticId, WPF0073ConverterDoesNotHaveAttributeUnknownTypes.DiagnosticId);

        /// <inheritdoc/>
        protected override async Task RegisterCodeFixesAsync(DocumentEditorCodeFixContext context)
        {
            var document = context.Document;
            var syntaxRoot = await document.GetSyntaxRootAsync(context.CancellationToken)
                                           .ConfigureAwait(false);
            var semanticModel = await document.GetSemanticModelAsync(context.CancellationToken)
                                              .ConfigureAwait(false);
            foreach (var diagnostic in context.Diagnostics)
            {
                var token = syntaxRoot.FindToken(diagnostic.Location.SourceSpan.Start);
                if (string.IsNullOrEmpty(token.ValueText))
                {
                    continue;
                }

                var classDeclaration = syntaxRoot.FindNode(diagnostic.Location.SourceSpan)
                                                       .FirstAncestorOrSelf<ClassDeclarationSyntax>();
                ITypeSymbol sourceType = null;
                ITypeSymbol targetType = null;
                if (classDeclaration != null &&
                    ValueConverter.TryGetConversionTypes(classDeclaration, semanticModel, context.CancellationToken, out sourceType, out targetType))
                {
                    context.RegisterCodeFix(
                        $"Add [ValueConversion(typeof({sourceType}), typeof({targetType}))].",
                        (e, _) => AddAttribute(e, classDeclaration, sourceType, targetType),
                        diagnostic);
                }
                else
                {
                    context.RegisterCodeFix(
                        $"Add [ValueConversion(typeof({sourceType?.ToString() ?? "TYPE"}), typeof({targetType?.ToString() ?? "TYPE"}))]..",
                        (e, _) => AddAttribute(e, classDeclaration, sourceType, targetType),
                        diagnostic);
                }
            }
        }

        private static void AddAttribute(DocumentEditor editor, ClassDeclarationSyntax classDeclaration, ITypeSymbol sourceType, ITypeSymbol targetType)
        {
            TypeOfExpressionSyntax TypeOf(ITypeSymbol t)
            {
                if (t != null)
                {
                    return (TypeOfExpressionSyntax)editor.Generator.TypeOfExpression(editor.Generator.TypeExpression(t));
                }

                return (TypeOfExpressionSyntax)editor.Generator.TypeOfExpression(SyntaxFactory.ParseTypeName("TYPE"));
            }

            var attributeArguments = new[]
                                     {
                                         editor.Generator.AttributeArgument(TypeOf(sourceType)),
                                         editor.Generator.AttributeArgument(TypeOf(targetType)),
                                     };
            editor.AddAttribute(
                classDeclaration,
                editor.Generator.AddAttributeArguments(
                    Attribute,
                    attributeArguments));
        }
    }
}
