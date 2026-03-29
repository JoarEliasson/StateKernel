using StateKernel.Simulation.Clock;
using StateKernel.Simulation.Signals;

namespace StateKernel.Simulation.Behaviors;

/// <summary>
/// Represents the minimal deterministic context required by the current behavior slice.
/// </summary>
/// <remarks>
/// This context intentionally exposes only the current deterministic tick and a read-only
/// committed signal snapshot. <see cref="AvailableSignals" /> never reflects same-tick staged
/// writes and is the only signal-read surface available to derived behaviors in this slice.
/// </remarks>
public sealed class BehaviorExecutionContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BehaviorExecutionContext" /> type.
    /// </summary>
    /// <param name="currentTick">The deterministic simulation tick being evaluated.</param>
    /// <param name="availableSignals">
    /// The read-only committed signal snapshot available to the behavior for this tick.
    /// </param>
    public BehaviorExecutionContext(
        SimulationTick currentTick,
        SimulationSignalSnapshot? availableSignals = null)
    {
        CurrentTick = currentTick;
        AvailableSignals = availableSignals ?? SimulationSignalSnapshot.Empty;
    }

    /// <summary>
    /// Gets the deterministic simulation tick being evaluated.
    /// </summary>
    public SimulationTick CurrentTick { get; }

    /// <summary>
    /// Gets the read-only committed signal snapshot available to the behavior for this tick.
    /// </summary>
    public SimulationSignalSnapshot AvailableSignals { get; }
}
