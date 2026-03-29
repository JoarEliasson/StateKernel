namespace StateKernel.Simulation.Modes.Transitions;

/// <summary>
/// Defines a deterministic rule that may request a simulation mode change after a completed step.
/// </summary>
/// <remarks>
/// Transition rules are evaluated only after a scheduler step has completed. They select a target
/// mode from the completed tick and the pre-transition current mode, but they do not apply changes
/// themselves.
/// </remarks>
public interface ISimulationModeTransitionRule
{
    /// <summary>
    /// Determines whether a target mode should be selected for the supplied completed-step context.
    /// </summary>
    /// <param name="context">The deterministic transition context.</param>
    /// <param name="targetMode">
    /// When this method returns <see langword="true" />, contains the requested target mode.
    /// Otherwise, the value must be ignored.
    /// </param>
    /// <returns>
    /// <see langword="true" /> when a target mode is requested; otherwise,
    /// <see langword="false" />.
    /// </returns>
    bool TrySelectTargetMode(SimulationModeTransitionContext context, out SimulationMode targetMode);
}
