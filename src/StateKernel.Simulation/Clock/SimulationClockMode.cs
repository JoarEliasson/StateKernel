namespace StateKernel.Simulation.Clock;

/// <summary>
/// Identifies the strategy used to advance simulation time.
/// </summary>
public enum SimulationClockMode
{
    /// <summary>
    /// Advances logical time through explicit deterministic steps.
    /// </summary>
    Deterministic = 0,
}
