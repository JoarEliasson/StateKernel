using StateKernel.Simulation.Clock;
using StateKernel.Simulation.Exceptions;
using StateKernel.Simulation.Modes;

namespace StateKernel.Simulation.StateMachines;

/// <summary>
/// Evaluates one formal state machine after completed scheduler steps and drives the mode seam.
/// </summary>
/// <remarks>
/// This coordinator is intentionally narrow, run-scoped, and single-threaded. It owns the current
/// formal state, evaluates deterministic transitions against the completed tick and the
/// pre-transition current state, and updates the activation-facing mode controller only when the
/// target state's mapped mode actually differs. It is not a scheduler, a history store, a
/// multi-machine orchestrator, a graph editor model, or a general transition DSL engine.
/// </remarks>
public sealed class SimulationStateMachineCoordinator
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SimulationStateMachineCoordinator" /> type.
    /// </summary>
    /// <param name="definition">The baseline formal state-machine definition.</param>
    /// <param name="stateModeMap">The explicit total mapping from formal states to operating modes.</param>
    /// <param name="modeController">The activation-facing mode controller driven by the coordinator.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="definition" />, <paramref name="stateModeMap" />, or
    /// <paramref name="modeController" /> is null.
    /// </exception>
    /// <exception cref="SimulationConfigurationException">
    /// Thrown when the initial state's mapped mode does not match the current controller mode.
    /// </exception>
    public SimulationStateMachineCoordinator(
        SimulationStateMachineDefinition definition,
        SimulationStateModeMap stateModeMap,
        SimulationModeController modeController)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(stateModeMap);
        ArgumentNullException.ThrowIfNull(modeController);

        var initialMode = stateModeMap.GetMode(definition.InitialStateId);

        if (modeController.CurrentMode != initialMode)
        {
            throw new SimulationConfigurationException(
                $"The current simulation mode '{modeController.CurrentMode}' must match the mapped mode '{initialMode}' for initial formal state '{definition.InitialStateId}'.");
        }

        Definition = definition;
        StateModeMap = stateModeMap;
        ModeController = modeController;
        CurrentStateId = definition.InitialStateId;
    }

    /// <summary>
    /// Gets the baseline formal state-machine definition.
    /// </summary>
    public SimulationStateMachineDefinition Definition { get; }

    /// <summary>
    /// Gets the explicit total mapping from formal states to operating modes.
    /// </summary>
    public SimulationStateModeMap StateModeMap { get; }

    /// <summary>
    /// Gets the activation-facing mode controller driven by the coordinator.
    /// </summary>
    public SimulationModeController ModeController { get; }

    /// <summary>
    /// Gets the current formal state.
    /// </summary>
    public SimulationStateId CurrentStateId { get; private set; }

    /// <summary>
    /// Evaluates the completed step and applies any resulting formal state transition.
    /// </summary>
    /// <param name="completedTick">The deterministic scheduler tick that just completed.</param>
    /// <returns>
    /// The applied formal state transition when the completed step selects a new state; otherwise,
    /// <see langword="null" />.
    /// </returns>
    /// <remarks>
    /// Transition evaluation happens against the completed tick and the pre-transition current
    /// state that was active during that completed step. Transition conditions are evaluated only
    /// inside the formal state-machine layer. A formal state change may occur without a mode change
    /// when the target state's mapped mode matches the current controller mode.
    /// </remarks>
    public SimulationStateTransition? EvaluateAndApply(SimulationTick completedTick)
    {
        var currentStateId = CurrentStateId;
        var context = new SimulationStateMachineContext(completedTick, currentStateId);
        var conditionContext = new SimulationStateTransitionConditionContext(
            context.CompletedTick,
            context.CurrentStateId);
        var matchingTransition = Definition.Transitions.FirstOrDefault(
            transition =>
                transition.SourceStateId == context.CurrentStateId &&
                transition.Condition.IsEligible(conditionContext));

        if (matchingTransition is null)
        {
            return null;
        }

        var targetStateId = matchingTransition.TargetStateId;
        CurrentStateId = targetStateId;

        var targetMode = StateModeMap.GetMode(targetStateId);

        if (ModeController.CurrentMode != targetMode)
        {
            ModeController.SetMode(targetMode);
        }

        return new SimulationStateTransition(
            completedTick,
            currentStateId,
            targetStateId);
    }
}
