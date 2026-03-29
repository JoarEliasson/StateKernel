namespace StateKernel.Simulation.Clock;

/// <summary>
/// Defines the deterministic logical clock contract used by the simulation core.
/// </summary>
public interface ISimulationClock
{
    /// <summary>
    /// Gets the advancement strategy used by the clock.
    /// </summary>
    SimulationClockMode Mode { get; }

    /// <summary>
    /// Gets the fixed-step settings used by the clock.
    /// </summary>
    SimulationClockSettings Settings { get; }

    /// <summary>
    /// Gets the current deterministic simulation tick.
    /// </summary>
    SimulationTick CurrentTick { get; }

    /// <summary>
    /// Advances the clock by a single deterministic tick.
    /// </summary>
    /// <returns>The new current simulation tick.</returns>
    SimulationTick Advance();

    /// <summary>
    /// Advances the clock by a bounded number of deterministic ticks.
    /// </summary>
    /// <param name="tickCount">The number of ticks to advance.</param>
    /// <returns>The new current simulation tick.</returns>
    SimulationTick AdvanceBy(int tickCount);

    /// <summary>
    /// Resets the clock back to its configured origin tick.
    /// </summary>
    void Reset();
}
