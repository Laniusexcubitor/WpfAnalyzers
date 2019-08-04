namespace WpfAnalyzers
{
    using Microsoft.CodeAnalysis;

    internal static class WPF0022DirectCastValueToExactType
    {
        internal const string DiagnosticId = "WPF0022";

        internal static readonly DiagnosticDescriptor Descriptor = Descriptors.Create(
            id: DiagnosticId,
            title: "Cast value to correct type.",
            messageFormat: "Value is of type {0}.",
            category: AnalyzerCategory.DependencyProperty,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Cast value to correct type.");
    }
}
