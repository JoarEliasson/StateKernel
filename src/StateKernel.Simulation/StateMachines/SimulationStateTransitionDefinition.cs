namespace StateKernel.Simulation.StateMachines;

/// <summary>
/// Represents a deterministic formal state transition definition.
/// </summary>
public sealed class SimulationStateTransitionDefinition
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SimulationStateTransitionDefinition" /> type.
    /// </summary>
    /// <param name="sourceStateId">The source state that must be active during the completed step.</param>
    /// <param name="targetStateId">The target state that becomes current after the transition.</param>
    /// <param name="condition">The deterministic condition that controls transition eligibility.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="sourceStateId" />, <paramref name="targetStateId" />, or
    /// <paramref name="condition" /> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="sourceStateId" /> and <paramref name="targetStateId" /> are
    /// equal.
    /// </exception>
    public SimulationStateTransitionDefinition(
        SimulationStateId sourceStateId,
        SimulationStateId targetStateId,
        ISimulationStateTransitionCondition condition)
    {
        ArgumentNullException.ThrowIfNull(sourceStateId);
        ArgumentNullException.ThrowIfNull(targetStateId);
        ArgumentNullException.ThrowIfNull(condition);

        if (sourceStateId == targetStateId)
        {
            throw new ArgumentException(
                "Formal state transitions must change the current state.",
                nameof(targetStateId));
        }

        SourceStateId = sourceStateId;
        TargetStateId = targetStateId;
        Condition = condition;
    }

    /// <summary>
    /// Gets the source state that must have been active during the completed step.
    /// </summary>
    public SimulationStateId SourceStateId { get; }

    /// <summary>
    /// Gets the target state that becomes current after the transition.
    /// </summary>
    public SimulationStateId TargetStateId { get; }

    /// <summary>
    /// Gets the deterministic condition that controls transition eligibility.
    /// </summary>
    public ISimulationStateTransitionCondition Condition { get; }
}
