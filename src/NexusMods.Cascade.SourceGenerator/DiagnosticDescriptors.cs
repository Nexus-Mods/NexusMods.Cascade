using Microsoft.CodeAnalysis;

namespace NexusMods.Cascade.SourceGenerator;

public static class DiagnosticDescriptors
{
    public static readonly DiagnosticDescriptor IncorrectBase = new(
        id: "NEXUSMODS_CASCADE001",
        title: "Incorrect base type",
        messageFormat: "The type '{0}' must implement '{1}'",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);
}
