namespace StateKernel.Simulation.Activation;

/// <summary>
/// Represents an activation policy that permits already-due behavior work only within an inclusive tick range.
/// </summary>
/// <remarks>
/// Tick zero may be considered active when the configured range includes zero. Activation still
/// does not trigger execution on its own, because the scheduler must first determine that work is due.
/// Current simulation mode does not affect this policy.
/// </remarks>
public sealed class TickRangeActivationPolicy : IBehaviorActivationPolicy
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TickRangeActivationPolicy" /> type.
    /// </summary>
    /// <param name="startTickInclusive">The first active tick sequence number.</param>
    /// <param name="endTickInclusive">The last active tick sequence number.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when either tick bound is negative.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="startTickInclusive" /> is greater than
    /// <paramref name="endTickInclusive" />.
    /// </exception>
    public TickRangeActivationPolicy(long startTickInclusive, long endTickInclusive)
    {
        if (startTickInclusive < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(startTickInclusive),
                "Activation range start ticks cannot be negative.");
        }

        if (endTickInclusive < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(endTickInclusive),
                "Activation range end ticks cannot be negative.");
        }

        if (startTickInclusive > endTickInclusive)
        {
            throw new ArgumentException(
                "Activation range start ticks cannot be greater than the end tick.",
                nameof(startTickInclusive));
        }

        StartTickInclusive = startTickInclusive;
        EndTickInclusive = endTickInclusive;
    }

    /// <summary>
    /// Gets the first active tick sequence number.
    /// </summary>
    public long StartTickInclusive { get; }

    /// <summary>
    /// Gets the last active tick sequence number.
    /// </summary>
    public long EndTickInclusive { get; }

    /// <inheritdoc />
    public bool IsActive(BehaviorActivationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var sequenceNumber = context.CurrentTick.SequenceNumber;
        return sequenceNumber >= StartTickInclusive && sequenceNumber <= EndTickInclusive;
    }
}
