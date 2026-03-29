using StateKernel.Simulation.Signals;

namespace StateKernel.Simulation.Signals.Dependencies;

/// <summary>
/// Represents one resolved declared signal dependency discovered during dependency planning.
/// </summary>
public sealed record SimulationSignalDependencyBinding
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SimulationSignalDependencyBinding" /> type.
    /// </summary>
    /// <param name="consumerWorkKey">The stable key of the consuming scheduled work.</param>
    /// <param name="requiredSignalId">The required upstream signal identifier.</param>
    /// <param name="producerWorkKey">The stable key of the producing scheduled work.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="requiredSignalId" /> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="consumerWorkKey" /> or <paramref name="producerWorkKey" />
    /// is null, empty, or whitespace.
    /// </exception>
    public SimulationSignalDependencyBinding(
        string consumerWorkKey,
        SimulationSignalId requiredSignalId,
        string producerWorkKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(consumerWorkKey);
        ArgumentNullException.ThrowIfNull(requiredSignalId);
        ArgumentException.ThrowIfNullOrWhiteSpace(producerWorkKey);

        ConsumerWorkKey = consumerWorkKey;
        RequiredSignalId = requiredSignalId;
        ProducerWorkKey = producerWorkKey;
    }

    /// <summary>
    /// Gets the stable key of the consuming scheduled work.
    /// </summary>
    public string ConsumerWorkKey { get; }

    /// <summary>
    /// Gets the required upstream signal identifier.
    /// </summary>
    public SimulationSignalId RequiredSignalId { get; }

    /// <summary>
    /// Gets the stable key of the producing scheduled work.
    /// </summary>
    public string ProducerWorkKey { get; }
}
