using StateKernel.Simulation.Context;

namespace StateKernel.Simulation.Scheduling;

/// <summary>
/// Defines the deterministic scheduler contract used by the simulation core.
/// </summary>
public interface ISimulationScheduler
{
    /// <summary>
    /// Gets the deterministic simulation context used by the scheduler.
    /// </summary>
    SimulationContext Context { get; }

    /// <summary>
    /// Gets the immutable scheduler plan used by the scheduler.
    /// </summary>
    SimulationSchedulerPlan Plan { get; }

    /// <summary>
    /// Advances the simulation by one tick and executes any due cadence buckets.
    /// </summary>
    /// <returns>The execution frame for the advanced tick.</returns>
    SchedulerExecutionFrame RunNextTick();

    /// <summary>
    /// Advances the simulation by the requested number of ticks and executes any due cadence buckets.
    /// </summary>
    /// <param name="tickCount">The number of deterministic ticks to execute.</param>
    /// <returns>The ordered execution frames for the requested ticks.</returns>
    IReadOnlyList<SchedulerExecutionFrame> RunTicks(int tickCount);

    /// <summary>
    /// Resets the scheduler back to the origin tick of its clock.
    /// </summary>
    /// <remarks>
    /// Resetting the scheduler resets only scheduler position through the clock. It does not reset
    /// state that may be held inside scheduled work implementations or other external collaborators.
    /// Stateful work is expected to participate in a separate run lifecycle or be recreated for a
    /// fresh simulation run.
    /// </remarks>
    void Reset();
}
