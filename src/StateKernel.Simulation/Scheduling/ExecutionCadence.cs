using StateKernel.Simulation.Clock;

namespace StateKernel.Simulation.Scheduling;

/// <summary>
/// Represents a fixed tick cadence used by the deterministic scheduler.
/// </summary>
/// <remarks>
/// A cadence is due only for positive sequence numbers that are evenly divisible by the interval.
/// The origin tick is never due, so execution begins on the first positive tick that matches the
/// configured interval after a reset.
/// </remarks>
public sealed record ExecutionCadence
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExecutionCadence" /> type.
    /// </summary>
    /// <param name="intervalInTicks">The number of simulation ticks between executions.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="intervalInTicks" /> is not greater than zero.
    /// </exception>
    public ExecutionCadence(int intervalInTicks)
    {
        if (intervalInTicks <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(intervalInTicks),
                "Execution cadence intervals must be greater than zero.");
        }

        IntervalInTicks = intervalInTicks;
    }

    /// <summary>
    /// Gets a cadence that executes on every deterministic tick.
    /// </summary>
    public static ExecutionCadence EveryTick { get; } = new(1);

    /// <summary>
    /// Gets the number of simulation ticks between executions.
    /// </summary>
    public int IntervalInTicks { get; }

    /// <summary>
    /// Determines whether the cadence is due on the supplied tick.
    /// </summary>
    /// <param name="tick">The deterministic simulation tick to evaluate.</param>
    /// <returns>
    /// <see langword="true" /> when the cadence is due on the supplied tick;
    /// otherwise, <see langword="false" />.
    /// </returns>
    public bool IsDue(SimulationTick tick)
    {
        return tick.SequenceNumber > 0 && tick.SequenceNumber % IntervalInTicks == 0;
    }
}
