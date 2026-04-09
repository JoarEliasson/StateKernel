using StateKernel.Simulation.Signals;

namespace StateKernel.Runtime.Abstractions.Composition;

/// <summary>
/// Represents one runtime-facing signal selection consumed by runtime composition.
/// </summary>
/// <remarks>
/// This type is intentionally a narrow immutable runtime-facing input artifact. It remains in the
/// composition namespace in this baseline slice to avoid unnecessary churn in the current runtime
/// API surface, even though it may now be produced by the separate simulation-to-runtime selection
/// seam. It is not a broader scenario authoring model or a richer runtime domain value type.
/// </remarks>
public sealed class RuntimeSignalSelection
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RuntimeSignalSelection" /> type.
    /// </summary>
    /// <param name="sourceSignalId">The source simulation signal identifier selected for exposure.</param>
    /// <param name="targetNodeId">
    /// The explicit runtime node identifier override. When omitted, composition applies the
    /// baseline default node-id rule.
    /// </param>
    /// <param name="displayNameOverride">
    /// The explicit display name override. When omitted, composition uses the canonical signal
    /// identifier value.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="sourceSignalId" /> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="displayNameOverride" /> is whitespace.
    /// </exception>
    public RuntimeSignalSelection(
        SimulationSignalId sourceSignalId,
        RuntimeNodeId? targetNodeId = null,
        string? displayNameOverride = null)
    {
        ArgumentNullException.ThrowIfNull(sourceSignalId);

        if (displayNameOverride is not null && string.IsNullOrWhiteSpace(displayNameOverride))
        {
            throw new ArgumentException(
                "Runtime signal-selection display name overrides cannot be whitespace when provided.",
                nameof(displayNameOverride));
        }

        SourceSignalId = sourceSignalId;
        TargetNodeId = targetNodeId;
        DisplayNameOverride = displayNameOverride?.Trim();
    }

    /// <summary>
    /// Gets the source simulation signal identifier selected for runtime exposure.
    /// </summary>
    public SimulationSignalId SourceSignalId { get; }

    /// <summary>
    /// Gets the explicit runtime node identifier override when one is provided.
    /// </summary>
    public RuntimeNodeId? TargetNodeId { get; }

    /// <summary>
    /// Gets the trimmed display name override when one is provided.
    /// </summary>
    public string? DisplayNameOverride { get; }
}
