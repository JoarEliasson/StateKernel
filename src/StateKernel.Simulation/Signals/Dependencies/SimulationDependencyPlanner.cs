using StateKernel.Simulation.Behaviors;
using StateKernel.Simulation.Exceptions;
using StateKernel.Simulation.Scheduling;

namespace StateKernel.Simulation.Signals.Dependencies;

/// <summary>
/// Creates immutable declared signal dependency plans from validated scheduler plans.
/// </summary>
/// <remarks>
/// This planner is intentionally narrow. It discovers published signals from
/// <see cref="ISignalProducingWork" /> and declared behavior dependencies from
/// <see cref="BehaviorScheduledWork" /> instances whose adapted behavior implements
/// <see cref="ISignalDependentBehavior" />. It does not perform graph analysis, cadence
/// validation, first-tick availability validation, or same-tick propagation planning.
/// </remarks>
public static class SimulationDependencyPlanner
{
    /// <summary>
    /// Creates a dependency plan from the supplied validated scheduler plan.
    /// </summary>
    /// <param name="schedulerPlan">The validated scheduler plan to inspect.</param>
    /// <returns>The immutable dependency plan for the supplied scheduler plan.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="schedulerPlan" /> is null.
    /// </exception>
    /// <exception cref="SimulationConfigurationException">
    /// Thrown when declared dependency metadata is invalid or references an unknown published
    /// signal identifier.
    /// </exception>
    public static SimulationDependencyPlan CreatePlan(SimulationSchedulerPlan schedulerPlan)
    {
        ArgumentNullException.ThrowIfNull(schedulerPlan);

        var orderedWorkItems = schedulerPlan.Buckets
            .SelectMany(static bucket => bucket.WorkItems)
            .ToArray();
        var publishedSignals = new List<SimulationPublishedSignal>();
        var publishedSignalsById = new Dictionary<SimulationSignalId, SimulationPublishedSignal>();

        foreach (var workItem in orderedWorkItems.OfType<ISignalProducingWork>())
        {
            if (workItem.ProducedSignalId is null)
            {
                continue;
            }

            var publishedSignal = new SimulationPublishedSignal(
                workItem.ProducedSignalId,
                workItem.Key);

            publishedSignals.Add(publishedSignal);
            publishedSignalsById.Add(publishedSignal.SignalId, publishedSignal);
        }

        var dependencyBindings = new List<SimulationSignalDependencyBinding>();

        foreach (var workItem in orderedWorkItems)
        {
            if (workItem is not BehaviorScheduledWork behaviorWork ||
                behaviorWork.Behavior is not ISignalDependentBehavior dependentBehavior)
            {
                continue;
            }

            var requiredSignalIds = MaterializeRequiredSignalIds(
                workItem.Key,
                dependentBehavior);

            foreach (var requiredSignalId in requiredSignalIds)
            {
                if (!publishedSignalsById.TryGetValue(requiredSignalId, out var publishedSignal))
                {
                    throw new SimulationConfigurationException(
                        $"Behavior work '{workItem.Key}' declares required signal '{requiredSignalId}', but no scheduled work publishes that signal.");
                }

                dependencyBindings.Add(
                    new SimulationSignalDependencyBinding(
                        workItem.Key,
                        requiredSignalId,
                        publishedSignal.ProducerWorkKey));
            }
        }

        return new SimulationDependencyPlan(publishedSignals, dependencyBindings);
    }

    private static SimulationSignalId[] MaterializeRequiredSignalIds(
        string workKey,
        ISignalDependentBehavior dependentBehavior)
    {
        var requiredSignalIds = dependentBehavior.RequiredSignalIds;

        if (requiredSignalIds is null)
        {
            throw new SimulationConfigurationException(
                $"Behavior work '{workKey}' returned null declared signal dependencies.");
        }

        var materializedRequiredSignalIds = requiredSignalIds.ToArray();

        if (Array.Exists(materializedRequiredSignalIds, static signalId => signalId is null))
        {
            throw new SimulationConfigurationException(
                $"Behavior work '{workKey}' declares a null required signal identifier.");
        }

        var duplicateRequiredSignalId = materializedRequiredSignalIds
            .GroupBy(static signalId => signalId)
            .FirstOrDefault(static group => group.Count() > 1)?
            .Key;

        if (duplicateRequiredSignalId is not null)
        {
            throw new SimulationConfigurationException(
                $"Behavior work '{workKey}' declares duplicate required signal '{duplicateRequiredSignalId}'.");
        }

        return materializedRequiredSignalIds
            .OrderBy(static signalId => signalId.Value, StringComparer.Ordinal)
            .ToArray();
    }
}
