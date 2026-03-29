using StateKernel.Simulation.Signals;

namespace StateKernel.Runtime.Abstractions;

/// <summary>
/// Represents the normalized compiled binding of one simulation signal to one runtime node.
/// </summary>
public sealed class RuntimeNodeBinding
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RuntimeNodeBinding" /> type.
    /// </summary>
    /// <param name="sourceSignalId">The source simulation signal identifier.</param>
    /// <param name="targetNodeId">The target runtime node identifier.</param>
    /// <param name="displayName">The human-readable node display name.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="sourceSignalId" /> or <paramref name="targetNodeId" /> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="displayName" /> is null, empty, or whitespace.
    /// </exception>
    public RuntimeNodeBinding(
        SimulationSignalId sourceSignalId,
        RuntimeNodeId targetNodeId,
        string displayName)
    {
        ArgumentNullException.ThrowIfNull(sourceSignalId);
        ArgumentNullException.ThrowIfNull(targetNodeId);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);

        SourceSignalId = sourceSignalId;
        TargetNodeId = targetNodeId;
        DisplayName = displayName.Trim();
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

    internal static RuntimeNodeBinding FromProjection(SimulationSignalProjection projection)
    {
        ArgumentNullException.ThrowIfNull(projection);
        return new RuntimeNodeBinding(
            projection.SourceSignalId,
            projection.TargetNodeId,
            projection.DisplayName);
    }
}
