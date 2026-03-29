using StateKernel.Simulation.Clock;
using StateKernel.Simulation.Modes;

namespace StateKernel.Simulation.Activation;

/// <summary>
/// Represents the minimal deterministic context required by the baseline activation slice.
/// </summary>
/// <remarks>
/// This context intentionally exposes only the current deterministic tick and the current
/// deterministic simulation mode. It may widen later only when a concrete deterministic activation
/// policy requires additional state.
/// </remarks>
public sealed class BehaviorActivationContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BehaviorActivationContext" /> type.
    /// </summary>
    /// <param name="currentTick">The deterministic simulation tick being evaluated.</param>
    /// <param name="currentMode">
    /// The current deterministic simulation mode at the moment already-due work is evaluated.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="currentMode" /> is null.
    /// </exception>
    public BehaviorActivationContext(SimulationTick currentTick, SimulationMode currentMode)
    {
        ArgumentNullException.ThrowIfNull(currentMode);

        CurrentTick = currentTick;
        CurrentMode = currentMode;
    }

    /// <summary>
    /// Gets the deterministic simulation tick being evaluated.
    /// </summary>
    public SimulationTick CurrentTick { get; }

    /// <summary>
    /// Gets the current deterministic simulation mode being evaluated.
    /// </summary>
    public SimulationMode CurrentMode { get; }
}
