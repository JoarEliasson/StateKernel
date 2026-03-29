namespace StateKernel.Simulation.StateMachines;

/// <summary>
/// Represents a transition condition that is eligible only on one exact completed tick.
/// </summary>
public sealed class CompletedTickMatchCondition : ISimulationStateTransitionCondition
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CompletedTickMatchCondition" /> type.
    /// </summary>
    /// <param name="completedTick">The exact completed tick sequence number required for eligibility.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="completedTick" /> is not positive.
    /// </exception>
    public CompletedTickMatchCondition(long completedTick)
    {
        if (completedTick <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(completedTick),
                "Completed tick match conditions require a positive tick.");
        }

        CompletedTick = completedTick;
    }

    /// <summary>
    /// Gets the exact completed tick sequence number required for eligibility.
    /// </summary>
    public long CompletedTick { get; }

    /// <inheritdoc />
    public bool IsEligible(SimulationStateTransitionConditionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.CompletedTick.SequenceNumber == CompletedTick;
    }
}
