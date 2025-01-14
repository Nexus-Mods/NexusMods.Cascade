using NexusMods.Cascade.Abstractions;

namespace NexusMods.Cascade;

/// <summary>
/// A pairing of an output definition and a stage definition
/// </summary>
public record struct UpstreamConnection(IStageDefinition StageDefinition, IOutputDefinition OutputDefinition);
