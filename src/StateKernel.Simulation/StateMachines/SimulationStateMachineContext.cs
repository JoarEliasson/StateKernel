using StateKernel.Simulation.Clock;

namespace StateKernel.Simulation.StateMachines;

/// <summary>
/// Represents the minimal deterministic context required by the baseline formal state-machine slice.
/// </summary>
/// <remarks>
/// This context intentionally exposes only the completed tick and the pre-transition formal state
/// that was active during that completed step. It may widen later only when a concrete
/// deterministic state-machine feature requires additional state.
/// </remarks>
public sealed class SimulationStateMachineContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SimulationStateMachineContext" /> type.
    /// </summary>
    /// <param name="completedTick">The deterministic scheduler tick that just completed.</param>
    /// <param name="currentStateId">
    /// The formal state that was active during the completed step, before any new state is applied.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="currentStateId" /> is null.
    /// </exception>
    public SimulationStateMachineContext(
        SimulationTick completedTick,
        SimulationStateId currentStateId)
    {
        ArgumentNullException.ThrowIfNull(currentStateId);

        CompletedTick = completedTick;
        CurrentStateId = currentStateId;
    }

    /// <summary>
    /// Gets the deterministic scheduler tick that just completed.
    /// </summary>
    public SimulationTick CompletedTick { get; }

    /// <summary>
    /// Gets the formal state that was active during the completed step before any new state is applied.
    /// </summary>
    public SimulationStateId CurrentStateId { get; }
}
