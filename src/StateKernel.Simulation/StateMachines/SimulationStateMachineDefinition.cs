using StateKernel.Simulation.Exceptions;

namespace StateKernel.Simulation.StateMachines;

/// <summary>
/// Represents the immutable baseline formal state-machine definition.
/// </summary>
/// <remarks>
/// The definition is structurally immutable after construction. State membership and transition
/// definitions cannot be mutated externally.
/// </remarks>
public sealed class SimulationStateMachineDefinition
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SimulationStateMachineDefinition" /> type.
    /// </summary>
    /// <param name="initialStateId">The canonical identifier of the initial formal state.</param>
    /// <param name="states">The formal states defined by the machine.</param>
    /// <param name="transitions">The deterministic transition definitions.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="initialStateId" />, <paramref name="states" />, or
    /// <paramref name="transitions" /> is null.
    /// </exception>
    /// <exception cref="SimulationConfigurationException">
    /// Thrown when the definition contains duplicate states, an undefined initial state, an
    /// undefined transition endpoint, or multiple transition definitions for the same source state
    /// in the baseline condition-driven model.
    /// </exception>
    public SimulationStateMachineDefinition(
        SimulationStateId initialStateId,
        IEnumerable<SimulationStateDefinition> states,
        IEnumerable<SimulationStateTransitionDefinition> transitions)
    {
        ArgumentNullException.ThrowIfNull(initialStateId);
        ArgumentNullException.ThrowIfNull(states);
        ArgumentNullException.ThrowIfNull(transitions);

        var materializedStates = states.ToArray();
        var materializedTransitions = transitions.ToArray();

        if (Array.Exists(materializedStates, static state => state is null))
        {
            throw new SimulationConfigurationException(
                "State-machine definitions cannot contain null states.");
        }

        if (Array.Exists(materializedTransitions, static transition => transition is null))
        {
            throw new SimulationConfigurationException(
                "State-machine definitions cannot contain null transitions.");
        }

        var duplicateStateId = materializedStates
            .GroupBy(state => state.Id)
            .FirstOrDefault(group => group.Count() > 1)?
            .Key;

        if (duplicateStateId is not null)
        {
            throw new SimulationConfigurationException(
                $"Formal state identifiers must be unique. Duplicate state: '{duplicateStateId}'.");
        }

        var definedStateIds = materializedStates
            .Select(state => state.Id)
            .ToHashSet();

        if (!definedStateIds.Contains(initialStateId))
        {
            throw new SimulationConfigurationException(
                $"The initial state '{initialStateId}' must be defined by the state machine.");
        }

        var undefinedSourceTransition = materializedTransitions
            .FirstOrDefault(transition => !definedStateIds.Contains(transition.SourceStateId));

        if (undefinedSourceTransition is not null)
        {
            throw new SimulationConfigurationException(
                $"Transition source state '{undefinedSourceTransition.SourceStateId}' is not defined by the state machine.");
        }

        var undefinedTargetTransition = materializedTransitions
            .FirstOrDefault(transition => !definedStateIds.Contains(transition.TargetStateId));

        if (undefinedTargetTransition is not null)
        {
            throw new SimulationConfigurationException(
                $"Transition target state '{undefinedTargetTransition.TargetStateId}' is not defined by the state machine.");
        }

        var ambiguousTransitionGroup = materializedTransitions
            .GroupBy(transition => transition.SourceStateId)
            .FirstOrDefault(group => group.Count() > 1);

        if (ambiguousTransitionGroup is not null)
        {
            throw new SimulationConfigurationException(
                $"The baseline condition-driven state-machine model supports at most one transition definition per source state. Duplicate source state: '{ambiguousTransitionGroup.Key}'.");
        }

        InitialStateId = initialStateId;
        States = Array.AsReadOnly(
            materializedStates
                .OrderBy(state => state.Id.Value, StringComparer.Ordinal)
                .ToArray());
        Transitions = Array.AsReadOnly(
            materializedTransitions
                .OrderBy(transition => transition.SourceStateId.Value, StringComparer.Ordinal)
                .ToArray());
    }

    /// <summary>
    /// Gets the canonical identifier of the initial formal state.
    /// </summary>
    public SimulationStateId InitialStateId { get; }

    /// <summary>
    /// Gets the defined formal states.
    /// </summary>
    public IReadOnlyList<SimulationStateDefinition> States { get; }

    /// <summary>
    /// Gets the deterministic transition definitions.
    /// </summary>
    public IReadOnlyList<SimulationStateTransitionDefinition> Transitions { get; }
}
