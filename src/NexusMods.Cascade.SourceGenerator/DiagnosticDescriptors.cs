using Microsoft.CodeAnalysis;

namespace NexusMods.Cascade.SourceGenerator;

public static class DiagnosticDescriptors
{
    public static readonly DiagnosticDescriptor IncorrectBase = new(
        "NEXUSMODS_CASCADE001",
        "Incorrect base type",
        "The type '{0}' must implement '{1}'",
        "Usage",
        DiagnosticSeverity.Error,
        true);
}
