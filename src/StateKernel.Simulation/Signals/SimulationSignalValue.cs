using StateKernel.Simulation.Behaviors;
using StateKernel.Simulation.Clock;

namespace StateKernel.Simulation.Signals;

/// <summary>
/// Represents a produced deterministic signal value.
/// </summary>
public sealed record SimulationSignalValue
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SimulationSignalValue" /> type.
    /// </summary>
    /// <param name="signalId">The canonical produced signal identifier.</param>
    /// <param name="tick">The deterministic tick that produced the value.</param>
    /// <param name="sample">The produced sampled value.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="signalId" /> or <paramref name="sample" /> is null.
    /// </exception>
    public SimulationSignalValue(
        SimulationSignalId signalId,
        SimulationTick tick,
        BehaviorSample sample)
    {
        ArgumentNullException.ThrowIfNull(signalId);
        ArgumentNullException.ThrowIfNull(sample);

        SignalId = signalId;
        Tick = tick;
        Sample = sample;
    }

    /// <summary>
    /// Gets the canonical produced signal identifier.
    /// </summary>
    public SimulationSignalId SignalId { get; }

    /// <summary>
    /// Gets the deterministic tick that produced the value.
    /// </summary>
    public SimulationTick Tick { get; }

    /// <summary>
    /// Gets the produced sampled value.
    /// </summary>
    public BehaviorSample Sample { get; }
}
