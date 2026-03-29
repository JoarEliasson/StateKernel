using StateKernel.Simulation.Exceptions;
using StateKernel.Simulation.Scheduling;

namespace StateKernel.Simulation.Signals.Dependencies.Diagnostics;

/// <summary>
/// Produces deterministic advisory timing diagnostics for declared signal dependencies.
/// </summary>
/// <remarks>
/// This analyzer is intentionally narrow and external to runtime execution. It consumes a validated
/// scheduler plan and dependency plan, then reasons only about the first consumer due tick under
/// the current scheduler cadence semantics: tick <c>0</c> is never due, and a cadence is due only
/// when <c>tick.SequenceNumber &gt; 0</c> and evenly divisible by the cadence interval. Under that
/// baseline model, a work item's first due tick equals <see cref="ExecutionCadence.IntervalInTicks" />.
/// If scheduler due semantics change later, this analyzer's first-due reasoning must be revisited.
/// </remarks>
public static class SimulationDependencyDiagnosticsAnalyzer
{
    /// <summary>
    /// Analyzes the supplied scheduler and dependency plans for first-need timing diagnostics.
    /// </summary>
    /// <param name="schedulerPlan">The validated scheduler plan to inspect.</param>
    /// <param name="dependencyPlan">The validated dependency plan to inspect.</param>
    /// <returns>The immutable advisory diagnostics report.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="schedulerPlan" /> or <paramref name="dependencyPlan" /> is null.
    /// </exception>
    /// <exception cref="SimulationConfigurationException">
    /// Thrown when the supplied plans are inconsistent by work key or producer signal identity.
    /// </exception>
    public static SimulationDependencyDiagnosticsReport Analyze(
        SimulationSchedulerPlan schedulerPlan,
        SimulationDependencyPlan dependencyPlan)
    {
        ArgumentNullException.ThrowIfNull(schedulerPlan);
        ArgumentNullException.ThrowIfNull(dependencyPlan);

        var workItemsByKey = schedulerPlan.Buckets
            .SelectMany(static bucket => bucket.WorkItems)
            .ToDictionary(static workItem => workItem.Key, StringComparer.Ordinal);
        var diagnostics = new List<SimulationDependencyDiagnostic>();

        foreach (var binding in dependencyPlan.DependencyBindings)
        {
            if (!workItemsByKey.TryGetValue(binding.ConsumerWorkKey, out var consumerWork))
            {
                throw new SimulationConfigurationException(
                    $"Dependency diagnostics require consumer work '{binding.ConsumerWorkKey}' to exist in the analyzed scheduler plan.");
            }

            if (!workItemsByKey.TryGetValue(binding.ProducerWorkKey, out var producerWork))
            {
                throw new SimulationConfigurationException(
                    $"Dependency diagnostics require producer work '{binding.ProducerWorkKey}' to exist in the analyzed scheduler plan.");
            }

            if (producerWork is not ISignalProducingWork signalProducingWork ||
                signalProducingWork.ProducedSignalId is null ||
                signalProducingWork.ProducedSignalId != binding.RequiredSignalId)
            {
                throw new SimulationConfigurationException(
                    $"Dependency diagnostics require producer work '{binding.ProducerWorkKey}' to publish signal '{binding.RequiredSignalId}'.");
            }

            var consumerFirstDueTick = GetFirstDueTick(consumerWork);
            var producerFirstDueTick = GetFirstDueTick(producerWork);

            if (producerFirstDueTick > consumerFirstDueTick)
            {
                diagnostics.Add(
                    CreateNoPriorProducerBeforeFirstConsumerTickDiagnostic(
                        binding,
                        consumerFirstDueTick,
                        producerFirstDueTick));
                continue;
            }

            if (producerFirstDueTick == consumerFirstDueTick)
            {
                diagnostics.Add(
                    CreateSameTickDependencyUnavailableDiagnostic(
                        binding,
                        consumerFirstDueTick,
                        producerFirstDueTick));
            }
        }

        return new SimulationDependencyDiagnosticsReport(diagnostics);
    }

    private static long GetFirstDueTick(IScheduledWork work)
    {
        return work.Cadence.IntervalInTicks;
    }

    private static SimulationDependencyDiagnostic CreateNoPriorProducerBeforeFirstConsumerTickDiagnostic(
        SimulationSignalDependencyBinding binding,
        long consumerFirstDueTick,
        long producerFirstDueTick)
    {
        return new SimulationDependencyDiagnostic(
            SimulationDependencyDiagnosticCode.NoPriorProducerBeforeFirstConsumerTick,
            binding.ConsumerWorkKey,
            binding.RequiredSignalId,
            binding.ProducerWorkKey,
            consumerFirstDueTick,
            producerFirstDueTick,
            $"Behavior work '{binding.ConsumerWorkKey}' first needs signal '{binding.RequiredSignalId}' on tick {consumerFirstDueTick}, but producer '{binding.ProducerWorkKey}' first publishes it on tick {producerFirstDueTick}. Under the prior-step committed-snapshot model, no prior producer value exists before that first need. This finding applies to the first need only; later consumer ticks may still succeed.");
    }

    private static SimulationDependencyDiagnostic CreateSameTickDependencyUnavailableDiagnostic(
        SimulationSignalDependencyBinding binding,
        long consumerFirstDueTick,
        long producerFirstDueTick)
    {
        return new SimulationDependencyDiagnostic(
            SimulationDependencyDiagnosticCode.SameTickDependencyUnavailable,
            binding.ConsumerWorkKey,
            binding.RequiredSignalId,
            binding.ProducerWorkKey,
            consumerFirstDueTick,
            producerFirstDueTick,
            $"Behavior work '{binding.ConsumerWorkKey}' first needs signal '{binding.RequiredSignalId}' on tick {consumerFirstDueTick}, and producer '{binding.ProducerWorkKey}' first publishes it on the same tick. Under the prior-step committed-snapshot model, same-tick reads are unavailable, so that first need cannot be satisfied. This finding applies to the first need only; later consumer ticks may still succeed.");
    }
}
