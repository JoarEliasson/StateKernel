using StateKernel.Simulation.Signals;

namespace StateKernel.Runtime.Abstractions;

/// <summary>
/// Represents the immutable validated runtime exposure plan for signal-to-node projection.
/// </summary>
/// <remarks>
/// This plan intentionally models only runtime exposure projection. It is not a general OPC UA
/// information model, a schema authoring surface, or a runtime orchestration plan.
/// </remarks>
public sealed class RuntimeProjectionPlan
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RuntimeProjectionPlan" /> type.
    /// </summary>
    /// <param name="projections">The signal-to-node projections to expose.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="projections" /> is null.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the plan contains null projections, duplicate source signals, or duplicate
    /// target runtime node identifiers.
    /// </exception>
    public RuntimeProjectionPlan(IEnumerable<SimulationSignalProjection> projections)
    {
        ArgumentNullException.ThrowIfNull(projections);

        var materializedProjections = projections.ToArray();

        if (Array.Exists(materializedProjections, static projection => projection is null))
        {
            throw new InvalidOperationException(
                "Runtime projection plans cannot contain null projections.");
        }

        var duplicateSignalId = materializedProjections
            .GroupBy(static projection => projection.SourceSignalId)
            .FirstOrDefault(static group => group.Count() > 1)?
            .Key;

        if (duplicateSignalId is not null)
        {
            throw new InvalidOperationException(
                $"Runtime projection plans must use unique source signal identifiers. Duplicate signal id: '{duplicateSignalId}'.");
        }

        var duplicateNodeId = materializedProjections
            .GroupBy(static projection => projection.TargetNodeId)
            .FirstOrDefault(static group => group.Count() > 1)?
            .Key;

        if (duplicateNodeId is not null)
        {
            throw new InvalidOperationException(
                $"Runtime projection plans must use unique target runtime node identifiers. Duplicate node id: '{duplicateNodeId}'.");
        }

        Projections = Array.AsReadOnly(
            materializedProjections
                .OrderBy(static projection => projection.SourceSignalId.Value, StringComparer.Ordinal)
                .ToArray());
    }

    /// <summary>
    /// Gets the deterministically ordered signal-to-node projections.
    /// </summary>
    public IReadOnlyList<SimulationSignalProjection> Projections { get; }
}
