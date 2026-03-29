namespace StateKernel.Simulation.StateMachines;

/// <summary>
/// Represents a transition condition that is eligible on one completed tick and every later tick.
/// </summary>
public sealed class CompletedTickAtOrAfterCondition : ISimulationStateTransitionCondition
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CompletedTickAtOrAfterCondition" /> type.
    /// </summary>
    /// <param name="completedTick">
    /// The earliest completed tick sequence number that allows eligibility.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="completedTick" /> is not positive.
    /// </exception>
    public CompletedTickAtOrAfterCondition(long completedTick)
    {
        if (completedTick <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(completedTick),
                "Completed tick at-or-after conditions require a positive tick.");
        }

        CompletedTick = completedTick;
    }

    /// <summary>
    /// Gets the earliest completed tick sequence number that allows eligibility.
    /// </summary>
    public long CompletedTick { get; }

    /// <inheritdoc />
    public bool IsEligible(SimulationStateTransitionConditionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.CompletedTick.SequenceNumber >= CompletedTick;
    }
}
