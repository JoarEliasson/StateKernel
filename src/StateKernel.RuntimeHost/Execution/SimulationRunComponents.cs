using StateKernel.Simulation.Scheduling;
using StateKernel.Simulation.Signals;

namespace StateKernel.RuntimeHost.Execution;

/// <summary>
/// Captures the fresh run-scoped simulation collaborators created by an executable run definition.
/// </summary>
public sealed class SimulationRunComponents
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SimulationRunComponents" /> type.
    /// </summary>
    /// <param name="scheduler">The scheduler that owns deterministic stepping for the run.</param>
    /// <param name="signalStore">The run-scoped signal store used for committed snapshot visibility.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="scheduler" /> or <paramref name="signalStore" /> is null.
    /// </exception>
    public SimulationRunComponents(
        ISimulationScheduler scheduler,
        SimulationSignalValueStore signalStore)
    {
        ArgumentNullException.ThrowIfNull(scheduler);
        ArgumentNullException.ThrowIfNull(signalStore);

        Scheduler = scheduler;
        SignalStore = signalStore;
    }

    /// <summary>
    /// Gets the scheduler that owns deterministic stepping for the run.
    /// </summary>
    public ISimulationScheduler Scheduler { get; }

    /// <summary>
    /// Gets the run-scoped signal store used for committed snapshot visibility.
    /// </summary>
    public SimulationSignalValueStore SignalStore { get; }
}
