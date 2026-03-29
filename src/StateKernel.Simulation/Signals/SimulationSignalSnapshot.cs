namespace StateKernel.Simulation.Signals;

/// <summary>
/// Represents a read-only committed snapshot of deterministic signal values.
/// </summary>
public sealed class SimulationSignalSnapshot
{
    private readonly IReadOnlyDictionary<SimulationSignalId, SimulationSignalValue> values;

    private SimulationSignalSnapshot(IReadOnlyDictionary<SimulationSignalId, SimulationSignalValue> values)
    {
        this.values = values;
    }

    /// <summary>
    /// Gets an empty committed signal snapshot.
    /// </summary>
    public static SimulationSignalSnapshot Empty { get; } = new(
        new Dictionary<SimulationSignalId, SimulationSignalValue>());

    /// <summary>
    /// Gets the number of committed signal values in the snapshot.
    /// </summary>
    public int Count => values.Count;

    /// <summary>
    /// Attempts to get a committed signal value from the snapshot.
    /// </summary>
    /// <param name="signalId">The signal identifier to look up.</param>
    /// <param name="value">The committed signal value when one exists.</param>
    /// <returns>
    /// <see langword="true" /> when a committed value exists for <paramref name="signalId" />;
    /// otherwise, <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="signalId" /> is null.
    /// </exception>
    public bool TryGetValue(SimulationSignalId signalId, out SimulationSignalValue value)
    {
        ArgumentNullException.ThrowIfNull(signalId);

        if (values.TryGetValue(signalId, out var committedValue))
        {
            value = committedValue;
            return true;
        }

        value = null!;
        return false;
    }

    /// <summary>
    /// Gets a required committed signal value from the snapshot.
    /// </summary>
    /// <param name="signalId">The signal identifier to look up.</param>
    /// <returns>The committed signal value for <paramref name="signalId" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="signalId" /> is null.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no committed value exists for <paramref name="signalId" />.
    /// </exception>
    public SimulationSignalValue GetRequiredValue(SimulationSignalId signalId)
    {
        ArgumentNullException.ThrowIfNull(signalId);

        if (TryGetValue(signalId, out var value))
        {
            return value;
        }

        throw new InvalidOperationException(
            $"A committed value for signal '{signalId}' is required but was not available.");
    }

    internal static SimulationSignalSnapshot CreateCommitted(
        IReadOnlyDictionary<SimulationSignalId, SimulationSignalValue> values)
    {
        return values.Count == 0
            ? Empty
            : new SimulationSignalSnapshot(
                new Dictionary<SimulationSignalId, SimulationSignalValue>(values));
    }
}
