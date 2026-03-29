namespace StateKernel.Simulation.Modes.Transitions;

/// <summary>
/// Represents a transition rule that never requests a simulation mode change.
/// </summary>
public sealed class NeverTransitionRule : ISimulationModeTransitionRule
{
    /// <inheritdoc />
    public bool TrySelectTargetMode(SimulationModeTransitionContext context, out SimulationMode targetMode)
    {
        ArgumentNullException.ThrowIfNull(context);

        targetMode = null!;
        return false;
    }
}
