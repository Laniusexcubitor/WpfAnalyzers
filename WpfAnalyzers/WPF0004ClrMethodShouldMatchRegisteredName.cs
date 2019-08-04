namespace WpfAnalyzers
{
    using Microsoft.CodeAnalysis;

    internal static class WPF0004ClrMethodShouldMatchRegisteredName
    {
        internal const string DiagnosticId = "WPF0004";

        internal static readonly DiagnosticDescriptor Descriptor = Descriptors.Create(
            id: DiagnosticId,
            title: "CLR method for a DependencyProperty must match registered name.",
            messageFormat: "Method '{0}' must be named '{1}'",
            category: AnalyzerCategory.DependencyProperty,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "CLR methods for accessing a DependencyProperty must have names matching the name the DependencyProperty is registered with.");
    }
}
