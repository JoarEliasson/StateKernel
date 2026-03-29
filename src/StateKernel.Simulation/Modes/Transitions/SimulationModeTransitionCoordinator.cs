using StateKernel.Simulation.Clock;

namespace StateKernel.Simulation.Modes.Transitions;

/// <summary>
/// Evaluates one deterministic mode-transition rule after completed scheduler steps.
/// </summary>
/// <remarks>
/// This coordinator is intentionally narrow, run-scoped, and single-threaded. It evaluates a
/// single transition rule against the completed tick and the pre-transition current mode, then
/// applies any actual change through <see cref="SimulationModeController" />. It is not a
/// scheduler, a transition graph engine, a multi-rule arbiter, a history store, or a reset or
/// lifecycle manager.
/// </remarks>
public sealed class SimulationModeTransitionCoordinator
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SimulationModeTransitionCoordinator" /> type.
    /// </summary>
    /// <param name="modeController">
    /// The controller that exposes and updates the current deterministic simulation mode.
    /// </param>
    /// <param name="rule">The single deterministic transition rule evaluated after completed steps.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="modeController" /> or <paramref name="rule" /> is null.
    /// </exception>
    public SimulationModeTransitionCoordinator(
        SimulationModeController modeController,
        ISimulationModeTransitionRule rule)
    {
        ArgumentNullException.ThrowIfNull(modeController);
        ArgumentNullException.ThrowIfNull(rule);

        ModeController = modeController;
        Rule = rule;
    }

    /// <summary>
    /// Gets the controller that exposes and updates the current deterministic simulation mode.
    /// </summary>
    public SimulationModeController ModeController { get; }

    /// <summary>
    /// Gets the single deterministic transition rule evaluated after completed steps.
    /// </summary>
    public ISimulationModeTransitionRule Rule { get; }

    /// <summary>
    /// Evaluates the completed step and applies any resulting mode transition.
    /// </summary>
    /// <param name="completedTick">The deterministic scheduler tick that just completed.</param>
    /// <returns>
    /// The applied transition when the selected target mode differs from the pre-transition current
    /// mode; otherwise, <see langword="null" />.
    /// </returns>
    /// <remarks>
    /// Same-mode target selections are treated as no transition. No transition record is produced
    /// unless an actual mode change occurs.
    /// </remarks>
    public SimulationModeTransition? EvaluateAndApply(SimulationTick completedTick)
    {
        var currentMode = ModeController.CurrentMode;
        ArgumentNullException.ThrowIfNull(currentMode);

        var context = new SimulationModeTransitionContext(completedTick, currentMode);

        if (!Rule.TrySelectTargetMode(context, out var targetMode))
        {
            return null;
        }

        ArgumentNullException.ThrowIfNull(targetMode);

        if (targetMode == currentMode)
        {
            return null;
        }

        ModeController.SetMode(targetMode);
        return new SimulationModeTransition(completedTick, currentMode, targetMode);
    }
}
