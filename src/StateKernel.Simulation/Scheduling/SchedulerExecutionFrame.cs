using StateKernel.Simulation.Clock;

namespace StateKernel.Simulation.Scheduling;

/// <summary>
/// Captures the deterministic work keys executed for a single simulation tick.
/// </summary>
/// <remarks>
/// The execution frame intentionally captures only the executed tick and ordered work keys. It
/// does not yet include durations, outcomes, state deltas, or metrics snapshots.
/// </remarks>
public sealed record SchedulerExecutionFrame
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SchedulerExecutionFrame" /> type.
    /// </summary>
    /// <param name="tick">The deterministic simulation tick that was executed.</param>
    /// <param name="executedWorkKeys">The ordered work keys that executed on the tick.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="executedWorkKeys" /> is null.
    /// </exception>
    public SchedulerExecutionFrame(SimulationTick tick, IEnumerable<string> executedWorkKeys)
    {
        ArgumentNullException.ThrowIfNull(executedWorkKeys);

        Tick = tick;
        ExecutedWorkKeys = Array.AsReadOnly(executedWorkKeys.ToArray());
    }

    /// <summary>
    /// Gets the deterministic simulation tick that was executed.
    /// </summary>
    public SimulationTick Tick { get; }

    /// <summary>
    /// Gets the ordered work keys that executed on the tick.
    /// </summary>
    public IReadOnlyList<string> ExecutedWorkKeys { get; }
}
