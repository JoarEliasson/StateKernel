using StateKernel.Simulation.Clock;
using StateKernel.Simulation.Context;
using StateKernel.Simulation.Exceptions;

namespace StateKernel.Simulation.Scheduling;

/// <summary>
/// Represents an ordered deterministic work bucket for a single execution cadence.
/// </summary>
public sealed class OrderedWorkBucket
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OrderedWorkBucket" /> type.
    /// </summary>
    /// <param name="cadence">The cadence represented by the bucket.</param>
    /// <param name="workItems">The work items contained by the bucket.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="workItems" /> is null.
    /// </exception>
    /// <exception cref="SimulationConfigurationException">
    /// Thrown when the bucket is empty, contains null items, or contains work items
    /// with a cadence that does not match <paramref name="cadence" />.
    /// </exception>
    public OrderedWorkBucket(ExecutionCadence cadence, IEnumerable<IScheduledWork> workItems)
    {
        ArgumentNullException.ThrowIfNull(cadence);
        ArgumentNullException.ThrowIfNull(workItems);

        var materializedItems = workItems.ToArray();

        if (materializedItems.Length == 0)
        {
            throw new SimulationConfigurationException(
                "Ordered work buckets must contain at least one work item.");
        }

        if (Array.Exists(materializedItems, static item => item is null))
        {
            throw new SimulationConfigurationException(
                "Ordered work buckets cannot contain null work items.");
        }

        if (Array.Exists(materializedItems, item => item.Cadence is null || item.Cadence != cadence))
        {
            throw new SimulationConfigurationException(
                "All work items in a bucket must share the same execution cadence.");
        }

        Cadence = cadence;
        WorkItems = Array.AsReadOnly(
            materializedItems
                .OrderBy(item => item.Order)
                .ThenBy(item => item.Key, StringComparer.Ordinal)
                .ToArray());
    }

    /// <summary>
    /// Gets the cadence represented by the bucket.
    /// </summary>
    public ExecutionCadence Cadence { get; }

    /// <summary>
    /// Gets the deterministically ordered work items in the bucket.
    /// </summary>
    public IReadOnlyList<IScheduledWork> WorkItems { get; }

    /// <summary>
    /// Determines whether the bucket is due on the supplied simulation tick.
    /// </summary>
    /// <param name="tick">The deterministic simulation tick to evaluate.</param>
    /// <returns>
    /// <see langword="true" /> when the bucket is due on the supplied tick;
    /// otherwise, <see langword="false" />.
    /// </returns>
    public bool IsDue(SimulationTick tick)
    {
        return Cadence.IsDue(tick);
    }

    /// <summary>
    /// Executes the bucket on the supplied simulation tick.
    /// </summary>
    /// <param name="context">The deterministic simulation context.</param>
    /// <param name="tick">The deterministic simulation tick being executed.</param>
    /// <returns>The ordered keys of the work items that executed.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="context" /> is null.
    /// </exception>
    public IReadOnlyList<string> Execute(SimulationContext context, SimulationTick tick)
    {
        ArgumentNullException.ThrowIfNull(context);

        var executedKeys = new string[WorkItems.Count];

        for (var index = 0; index < WorkItems.Count; index++)
        {
            var workItem = WorkItems[index];
            workItem.Execute(context, tick);
            executedKeys[index] = workItem.Key;
        }

        return Array.AsReadOnly(executedKeys);
    }
}
