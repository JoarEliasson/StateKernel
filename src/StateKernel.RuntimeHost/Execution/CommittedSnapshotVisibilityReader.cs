using StateKernel.Simulation.Clock;
using StateKernel.Simulation.Scheduling;
using StateKernel.Simulation.Signals;

namespace StateKernel.RuntimeHost.Execution;

/// <summary>
/// Resolves the committed signal snapshot visible after a completed deterministic step.
/// </summary>
/// <remarks>
/// This helper makes the accepted prior-step committed-snapshot visibility rule explicit for the
/// execution seam. It does not introduce a new simulation semantic; it translates a completed
/// execution frame into the next tick boundary at which the produced values become committed.
/// </remarks>
public static class CommittedSnapshotVisibilityReader
{
    /// <summary>
    /// Resolves the committed signal snapshot visible after the supplied completed execution frame.
    /// </summary>
    /// <param name="completedFrame">The completed execution frame to evaluate.</param>
    /// <param name="clockSettings">The deterministic clock settings used by the scheduler.</param>
    /// <param name="signalStore">The run-scoped signal store used for committed visibility.</param>
    /// <returns>The committed snapshot visible at the next tick boundary.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="clockSettings" /> or <paramref name="signalStore" /> is null.
    /// </exception>
    public static SimulationSignalSnapshot ReadVisibleSnapshotAfterCompletedFrame(
        SchedulerExecutionFrame completedFrame,
        SimulationClockSettings clockSettings,
        SimulationSignalValueStore signalStore)
    {
        ArgumentNullException.ThrowIfNull(clockSettings);
        ArgumentNullException.ThrowIfNull(signalStore);

        return signalStore.GetCommittedSnapshotForTick(
            completedFrame.Tick.Advance(clockSettings.TickInterval));
    }
}
