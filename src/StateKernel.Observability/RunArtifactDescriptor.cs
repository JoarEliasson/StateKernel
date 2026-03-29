namespace StateKernel.Observability;

/// <summary>
/// Describes a durable artifact emitted from a runtime or benchmark execution.
/// </summary>
/// <param name="Name">The logical artifact name.</param>
/// <param name="MediaType">The artifact media type.</param>
public sealed record RunArtifactDescriptor(string Name, string MediaType);
