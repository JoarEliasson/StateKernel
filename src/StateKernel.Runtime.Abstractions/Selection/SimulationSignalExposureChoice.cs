using StateKernel.Simulation.Signals;

namespace StateKernel.Runtime.Abstractions.Selection;

/// <summary>
/// Represents one approved simulation-side choice to expose a signal to the runtime layer.
/// </summary>
/// <remarks>
/// This type exists to preserve the boundary between upstream simulation-side approval and the
/// runtime-facing <see cref="Composition.RuntimeSignalSelection" /> input consumed by runtime
/// composition. Its current override surface intentionally mirrors that runtime-facing type in this
/// baseline slice, but it models a separate concern.
/// </remarks>
public sealed class SimulationSignalExposureChoice
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SimulationSignalExposureChoice" /> type.
    /// </summary>
    /// <param name="sourceSignalId">The approved simulation signal identifier to expose.</param>
    /// <param name="targetNodeIdOverride">
    /// The explicit runtime node identifier override when one is approved upstream.
    /// </param>
    /// <param name="displayNameOverride">
    /// The optional display name override. A <see langword="null" /> value means no override.
    /// When a non-null value is supplied, it is trimmed before storage and must not normalize to
    /// an empty string.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="sourceSignalId" /> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="displayNameOverride" /> is whitespace after trimming.
    /// </exception>
    public SimulationSignalExposureChoice(
        SimulationSignalId sourceSignalId,
        RuntimeNodeId? targetNodeIdOverride = null,
        string? displayNameOverride = null)
    {
        ArgumentNullException.ThrowIfNull(sourceSignalId);

        if (displayNameOverride is not null && string.IsNullOrWhiteSpace(displayNameOverride))
        {
            throw new ArgumentException(
                "Simulation signal exposure-choice display name overrides cannot be whitespace when provided.",
                nameof(displayNameOverride));
        }

        SourceSignalId = sourceSignalId;
        TargetNodeIdOverride = targetNodeIdOverride;
        DisplayNameOverride = displayNameOverride?.Trim();
    }

    /// <summary>
    /// Gets the approved simulation signal identifier to expose.
    /// </summary>
    public SimulationSignalId SourceSignalId { get; }

    /// <summary>
    /// Gets the explicit runtime node identifier override when one is approved upstream.
    /// </summary>
    public RuntimeNodeId? TargetNodeIdOverride { get; }

    /// <summary>
    /// Gets the canonical trimmed display name override when one is provided.
    /// </summary>
    public string? DisplayNameOverride { get; }
}
