using StateKernel.Simulation.Signals;

namespace StateKernel.Runtime.Abstractions;

/// <summary>
/// Represents one explicit runtime exposure mapping from a simulation signal to a runtime node.
/// </summary>
/// <remarks>
/// This type is intentionally runtime exposure-facing. It is not a general OPC UA information
/// model, a NodeSet import surface, or a broader schema authoring abstraction.
/// </remarks>
public sealed class SimulationSignalProjection
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SimulationSignalProjection" /> type.
    /// </summary>
    /// <param name="sourceSignalId">The source simulation signal identifier.</param>
    /// <param name="targetNodeId">The target runtime node identifier.</param>
    /// <param name="displayName">
    /// The human-readable node display name. When omitted, the canonical signal identifier value is used.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="sourceSignalId" /> or <paramref name="targetNodeId" /> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="displayName" /> is whitespace.
    /// </exception>
    public SimulationSignalProjection(
        SimulationSignalId sourceSignalId,
        RuntimeNodeId targetNodeId,
        string? displayName = null)
    {
        ArgumentNullException.ThrowIfNull(sourceSignalId);
        ArgumentNullException.ThrowIfNull(targetNodeId);

        if (displayName is not null && string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException(
                "Runtime projection display names cannot be whitespace when provided.",
                nameof(displayName));
        }

        SourceSignalId = sourceSignalId;
        TargetNodeId = targetNodeId;
        DisplayName = displayName?.Trim() ?? sourceSignalId.Value;
    }

    /// <summary>
    /// Gets the source simulation signal identifier.
    /// </summary>
    public SimulationSignalId SourceSignalId { get; }

    /// <summary>
    /// Gets the target runtime node identifier.
    /// </summary>
    public RuntimeNodeId TargetNodeId { get; }

    /// <summary>
    /// Gets the human-readable node display name.
    /// </summary>
    public string DisplayName { get; }
}
