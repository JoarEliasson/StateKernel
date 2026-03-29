namespace StateKernel.Simulation.Clock;

/// <summary>
/// Represents a deterministic logical tick within a simulation run.
/// </summary>
public readonly record struct SimulationTick
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SimulationTick" /> type.
    /// </summary>
    /// <param name="sequenceNumber">The zero-based tick sequence number.</param>
    /// <param name="logicalTime">The elapsed logical time since the simulation origin.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="sequenceNumber" /> is negative.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="logicalTime" /> is negative.
    /// </exception>
    public SimulationTick(long sequenceNumber, TimeSpan logicalTime)
    {
        if (sequenceNumber < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(sequenceNumber),
                "Tick sequence numbers cannot be negative.");
        }

        if (logicalTime < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(logicalTime),
                "Logical time cannot be negative.");
        }

        SequenceNumber = sequenceNumber;
        LogicalTime = logicalTime;
    }

    /// <summary>
    /// Gets the origin tick for a new simulation.
    /// </summary>
    public static SimulationTick Origin { get; } = new(0, TimeSpan.Zero);

    /// <summary>
    /// Gets the zero-based tick sequence number.
    /// </summary>
    public long SequenceNumber { get; }

    /// <summary>
    /// Gets the elapsed logical time since the simulation origin.
    /// </summary>
    public TimeSpan LogicalTime { get; }

    /// <summary>
    /// Determines whether the tick aligns with the supplied fixed-step interval.
    /// </summary>
    /// <param name="tickInterval">The fixed-step interval to validate.</param>
    /// <returns>
    /// <see langword="true" /> when the tick sequence number and logical time are
    /// consistent with the supplied interval; otherwise, <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="tickInterval" /> is not positive.
    /// </exception>
    public bool IsConsistentWith(TimeSpan tickInterval)
    {
        if (tickInterval <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(tickInterval),
                "Tick intervals must be positive.");
        }

        try
        {
            return LogicalTime == TimeSpan.FromTicks(checked(SequenceNumber * tickInterval.Ticks));
        }
        catch (OverflowException)
        {
            return false;
        }
    }

    /// <summary>
    /// Advances the tick by exactly one fixed-step interval.
    /// </summary>
    /// <param name="tickInterval">The interval represented by a single simulation step.</param>
    /// <returns>The next deterministic simulation tick.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="tickInterval" /> is not positive.
    /// </exception>
    public SimulationTick Advance(TimeSpan tickInterval)
    {
        if (tickInterval <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(tickInterval),
                "Tick intervals must be positive.");
        }

        var nextSequenceNumber = checked(SequenceNumber + 1);
        var nextLogicalTimeTicks = checked(LogicalTime.Ticks + tickInterval.Ticks);
        return new SimulationTick(nextSequenceNumber, TimeSpan.FromTicks(nextLogicalTimeTicks));
    }
}
