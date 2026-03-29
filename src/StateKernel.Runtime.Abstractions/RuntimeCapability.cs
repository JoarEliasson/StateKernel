namespace StateKernel.Runtime.Abstractions;

/// <summary>
/// Identifies an optional feature that may be supported by a runtime adapter.
/// </summary>
public enum RuntimeCapability
{
    /// <summary>
    /// Indicates support for explicit read-only runtime value exposure.
    /// </summary>
    ReadOnlyValueExposure,

    /// <summary>
    /// Indicates support for explicit security and endpoint profile handling.
    /// </summary>
    SecurityProfiles,

    /// <summary>
    /// Indicates support for NodeSet-driven model import or projection.
    /// </summary>
    NodeSetImport,

    /// <summary>
    /// Indicates support for benchmark-oriented execution and telemetry workflows.
    /// </summary>
    BenchmarkExecution,
}
