namespace StateKernel.Simulation.Clock;

/// <summary>
/// Defines the deterministic clock settings used by the simulation core.
/// </summary>
public sealed record SimulationClockSettings
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SimulationClockSettings" /> type.
    /// </summary>
    /// <param name="tickInterval">The logical duration of one simulation tick.</param>
    /// <param name="maxCatchUpTicks">The maximum number of ticks allowed in one bounded advance burst.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="tickInterval" /> is not positive.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="maxCatchUpTicks" /> is not greater than zero.
    /// </exception>
    public SimulationClockSettings(TimeSpan tickInterval, int maxCatchUpTicks)
    {
        if (tickInterval <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(tickInterval), "Tick interval must be positive.");
        }

        if (maxCatchUpTicks <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxCatchUpTicks),
                "Maximum catch-up ticks must be greater than zero.");
        }

        TickInterval = tickInterval;
        MaxCatchUpTicks = maxCatchUpTicks;
    }

    /// <summary>
    /// Gets a conservative default clock profile for early simulation work.
    /// </summary>
    public static SimulationClockSettings Default { get; } = new(TimeSpan.FromMilliseconds(100), 8);

    /// <summary>
    /// Gets the logical duration of one simulation tick.
    /// </summary>
    public TimeSpan TickInterval { get; }

    /// <summary>
    /// Gets the maximum number of ticks allowed in one bounded advance burst.
    /// </summary>
    public int MaxCatchUpTicks { get; }
}
