namespace StateKernel.Simulation.Modes;

/// <summary>
/// Provides explicit deterministic control over the current simulation mode.
/// </summary>
/// <remarks>
/// This controller is intentionally narrow and run-scoped. It is designed for the current
/// deterministic single-threaded simulation seam and is not a concurrent shared-state abstraction.
/// It is not a transition engine, a scheduler participant, a mode-history store, or a transition
/// validation component.
/// </remarks>
public sealed class SimulationModeController : ISimulationModeSource
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SimulationModeController" /> type.
    /// </summary>
    /// <param name="initialMode">The initial deterministic simulation mode.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="initialMode" /> is null.
    /// </exception>
    public SimulationModeController(SimulationMode initialMode)
    {
        ArgumentNullException.ThrowIfNull(initialMode);
        CurrentMode = initialMode;
    }

    /// <inheritdoc />
    public SimulationMode CurrentMode { get; private set; }

    /// <summary>
    /// Updates the current deterministic simulation mode.
    /// </summary>
    /// <param name="mode">The mode that should become current.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="mode" /> is null.
    /// </exception>
    public void SetMode(SimulationMode mode)
    {
        ArgumentNullException.ThrowIfNull(mode);
        CurrentMode = mode;
    }
}
