using StateKernel.Simulation.Signals;

namespace StateKernel.Simulation.Signals.Dependencies;

/// <summary>
/// Represents one deterministic published signal discovered during dependency planning.
/// </summary>
public sealed record SimulationPublishedSignal
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SimulationPublishedSignal" /> type.
    /// </summary>
    /// <param name="signalId">The published signal identifier.</param>
    /// <param name="producerWorkKey">The stable key of the producing scheduled work.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="signalId" /> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="producerWorkKey" /> is null, empty, or whitespace.
    /// </exception>
    public SimulationPublishedSignal(
        SimulationSignalId signalId,
        string producerWorkKey)
    {
        ArgumentNullException.ThrowIfNull(signalId);
        ArgumentException.ThrowIfNullOrWhiteSpace(producerWorkKey);

        SignalId = signalId;
        ProducerWorkKey = producerWorkKey;
    }

    /// <summary>
    /// Gets the published signal identifier.
    /// </summary>
    public SimulationSignalId SignalId { get; }

    /// <summary>
    /// Gets the stable key of the producing scheduled work.
    /// </summary>
    public string ProducerWorkKey { get; }
}
