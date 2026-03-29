namespace StateKernel.Simulation.Modes;

/// <summary>
/// Defines a source that exposes the current deterministic simulation mode.
/// </summary>
public interface ISimulationModeSource
{
    /// <summary>
    /// Gets the current deterministic simulation mode.
    /// </summary>
    /// <remarks>
    /// This contract is intentionally non-null. There is no implicit default mode and no
    /// uninitialized mode state in the baseline control seam.
    /// </remarks>
    SimulationMode CurrentMode { get; }
}
