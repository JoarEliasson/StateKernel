using StateKernel.Simulation.Exceptions;
using StateKernel.Simulation.Signals;

namespace StateKernel.Simulation.Scheduling;

/// <summary>
/// Represents the immutable cadence-bucket plan used by the deterministic scheduler.
/// </summary>
/// <remarks>
/// The plan is structurally immutable after construction. Bucket membership and bucket ordering
/// cannot be mutated externally. Scheduled work instances are referenced rather than deep-copied,
/// so any internal mutable state inside a work implementation remains the responsibility of that
/// implementation and its owning lifecycle.
/// </remarks>
public sealed class SimulationSchedulerPlan
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SimulationSchedulerPlan" /> type.
    /// </summary>
    /// <param name="workItems">The scheduled work items that should be grouped into cadence buckets.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="workItems" /> is null.
    /// </exception>
    /// <exception cref="SimulationConfigurationException">
    /// Thrown when the plan contains null work items, duplicate keys, invalid keys, or
    /// invalid explicit ordering values. Signal-producing work items may also fail validation when
    /// they publish duplicate signal identifiers.
    /// </exception>
    public SimulationSchedulerPlan(IEnumerable<IScheduledWork> workItems)
    {
        ArgumentNullException.ThrowIfNull(workItems);

        var materializedItems = workItems.ToArray();

        if (Array.Exists(materializedItems, static item => item is null))
        {
            throw new SimulationConfigurationException(
                "Scheduler plans cannot contain null work items.");
        }

        var invalidKey = materializedItems
            .FirstOrDefault(item => string.IsNullOrWhiteSpace(item.Key));

        if (invalidKey is not null)
        {
            throw new SimulationConfigurationException(
                "Scheduler work item keys must be non-empty and stable.");
        }

        var invalidCadence = materializedItems.FirstOrDefault(item => item.Cadence is null);

        if (invalidCadence is not null)
        {
            throw new SimulationConfigurationException(
                $"Scheduler work item '{invalidCadence.Key}' must define a cadence.");
        }

        var invalidOrder = materializedItems.FirstOrDefault(item => item.Order < 0);

        if (invalidOrder is not null)
        {
            throw new SimulationConfigurationException(
                $"Scheduler work item '{invalidOrder.Key}' uses a negative explicit order.");
        }

        var duplicateKey = materializedItems
            .GroupBy(item => item.Key, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1)?
            .Key;

        if (duplicateKey is not null)
        {
            throw new SimulationConfigurationException(
                $"Scheduler work item keys must be unique. Duplicate key: '{duplicateKey}'.");
        }

        var duplicateProducedSignalId = materializedItems
            .OfType<ISignalProducingWork>()
            .Where(item => item.ProducedSignalId is not null)
            .GroupBy(item => item.ProducedSignalId!)
            .FirstOrDefault(group => group.Count() > 1)?
            .Key;

        if (duplicateProducedSignalId is not null)
        {
            throw new SimulationConfigurationException(
                $"Published signal identifiers must be unique. Duplicate signal id: '{duplicateProducedSignalId}'.");
        }

        Buckets = Array.AsReadOnly(
            materializedItems
                .GroupBy(item => item.Cadence)
                .OrderBy(group => group.Key.IntervalInTicks)
                .Select(group => new OrderedWorkBucket(group.Key, group))
                .ToArray());
    }

    /// <summary>
    /// Gets an empty scheduler plan.
    /// </summary>
    public static SimulationSchedulerPlan Empty { get; } = new(Array.Empty<IScheduledWork>());

    /// <summary>
    /// Gets the deterministically ordered cadence buckets in the plan.
    /// </summary>
    public IReadOnlyList<OrderedWorkBucket> Buckets { get; }
}
