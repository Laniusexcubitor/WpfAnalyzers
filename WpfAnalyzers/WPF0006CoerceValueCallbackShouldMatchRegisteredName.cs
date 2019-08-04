namespace WpfAnalyzers
{
    using Microsoft.CodeAnalysis;

    internal static class WPF0006CoerceValueCallbackShouldMatchRegisteredName
    {
        internal const string DiagnosticId = "WPF0006";

        internal static readonly DiagnosticDescriptor Descriptor = Descriptors.Create(
            id: DiagnosticId,
            title: "Name of CoerceValueCallback should match registered name.",
            messageFormat: "Method '{0}' should be named '{1}'",
            category: AnalyzerCategory.DependencyProperty,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Name of CoerceValueCallback should match registered name.");
    }
}
