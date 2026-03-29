using StateKernel.Simulation.Clock;

namespace StateKernel.Simulation.Modes.Transitions;

/// <summary>
/// Represents the minimal deterministic context required by the baseline mode-transition slice.
/// </summary>
/// <remarks>
/// This context intentionally exposes only the completed tick and the pre-transition current mode
/// that was active during that completed step. It may widen later only when a concrete
/// deterministic transition rule requires additional state.
/// </remarks>
public sealed class SimulationModeTransitionContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SimulationModeTransitionContext" /> type.
    /// </summary>
    /// <param name="completedTick">The deterministic simulation tick that just completed.</param>
    /// <param name="currentMode">
    /// The deterministic simulation mode that was active during the completed step, before any
    /// transition is applied.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="currentMode" /> is null.
    /// </exception>
    public SimulationModeTransitionContext(SimulationTick completedTick, SimulationMode currentMode)
    {
        ArgumentNullException.ThrowIfNull(currentMode);

        CompletedTick = completedTick;
        CurrentMode = currentMode;
    }

    /// <summary>
    /// Gets the deterministic simulation tick that just completed.
    /// </summary>
    public SimulationTick CompletedTick { get; }

    /// <summary>
    /// Gets the deterministic simulation mode that was active during the completed step.
    /// </summary>
    public SimulationMode CurrentMode { get; }
}
