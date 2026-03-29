using StateKernel.Simulation.Clock;

namespace StateKernel.Simulation.StateMachines;

/// <summary>
/// Represents an applied formal simulation state transition.
/// </summary>
public sealed record SimulationStateTransition
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SimulationStateTransition" /> type.
    /// </summary>
    /// <param name="completedTick">The completed deterministic tick that triggered the state change.</param>
    /// <param name="previousStateId">The formal state that was active during the completed step.</param>
    /// <param name="nextStateId">The formal state that became current after the transition was applied.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="previousStateId" /> or <paramref name="nextStateId" /> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="previousStateId" /> and <paramref name="nextStateId" /> are
    /// equal.
    /// </exception>
    public SimulationStateTransition(
        SimulationTick completedTick,
        SimulationStateId previousStateId,
        SimulationStateId nextStateId)
    {
        ArgumentNullException.ThrowIfNull(previousStateId);
        ArgumentNullException.ThrowIfNull(nextStateId);

        if (previousStateId == nextStateId)
        {
            throw new ArgumentException(
                "Applied formal state transitions must change the current state.",
                nameof(nextStateId));
        }

        CompletedTick = completedTick;
        PreviousStateId = previousStateId;
        NextStateId = nextStateId;
    }

    /// <summary>
    /// Gets the completed deterministic tick that triggered the state change.
    /// </summary>
    public SimulationTick CompletedTick { get; }

    /// <summary>
    /// Gets the formal state that was active during the completed step.
    /// </summary>
    public SimulationStateId PreviousStateId { get; }

    /// <summary>
    /// Gets the formal state that became current after the transition was applied.
    /// </summary>
    public SimulationStateId NextStateId { get; }
}
