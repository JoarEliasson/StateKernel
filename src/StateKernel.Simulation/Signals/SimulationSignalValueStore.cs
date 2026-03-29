namespace StateKernel.Simulation.Signals;

/// <summary>
/// Maintains the latest committed deterministic signal snapshot for one simulation run.
/// </summary>
/// <remarks>
/// This store is intentionally narrow, run-scoped, and single-threaded. It is not a history store,
/// a graph engine, a formula engine, a runtime-facing projection layer, or a general shared cache.
/// It stages values produced during the current producing tick and exposes only the latest
/// committed snapshot from earlier producing ticks.
/// </remarks>
public sealed class SimulationSignalValueStore
{
    private readonly Dictionary<SimulationSignalId, SimulationSignalValue> committedValues = [];
    private readonly Dictionary<SimulationSignalId, SimulationSignalValue> stagedValues = [];
    private SimulationSignalSnapshot currentSnapshot = SimulationSignalSnapshot.Empty;
    private long? lastPreparedTickSequenceNumber;
    private long? stagedTickSequenceNumber;

    /// <summary>
    /// Gets the latest committed signal snapshot for the supplied deterministic tick.
    /// </summary>
    /// <param name="tick">The deterministic tick being evaluated.</param>
    /// <returns>The latest committed prior-step signal snapshot.</returns>
    /// <remarks>
    /// This is a tick-boundary advancement operation conceptually. The first call for tick
    /// <c>N</c> commits any values staged during earlier producing ticks and returns the latest
    /// committed snapshot for reads on tick <c>N</c>. Repeated calls for the same tick are
    /// idempotent and return the same committed snapshot. Values staged during tick <c>N</c> never
    /// become visible during tick <c>N</c>; they become visible only when a later tick calls this
    /// method.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the store is asked to move backward to an earlier tick.
    /// </exception>
    public SimulationSignalSnapshot GetCommittedSnapshotForTick(Clock.SimulationTick tick)
    {
        if (lastPreparedTickSequenceNumber.HasValue)
        {
            if (tick.SequenceNumber < lastPreparedTickSequenceNumber.Value)
            {
                throw new InvalidOperationException(
                    "Signal value stores are run-scoped and require monotonically increasing tick access.");
            }

            if (tick.SequenceNumber == lastPreparedTickSequenceNumber.Value)
            {
                return currentSnapshot;
            }
        }

        CommitStagedValuesBefore(tick.SequenceNumber);
        lastPreparedTickSequenceNumber = tick.SequenceNumber;
        return currentSnapshot;
    }

    /// <summary>
    /// Records a produced signal value for later committed visibility.
    /// </summary>
    /// <param name="value">The produced signal value to stage.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value" /> is null.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the store has not been advanced to <paramref name="value" />'s tick or when
    /// staged values would cross tick boundaries.
    /// </exception>
    public void RecordProducedValue(SimulationSignalValue value)
    {
        ArgumentNullException.ThrowIfNull(value);

        if (!lastPreparedTickSequenceNumber.HasValue ||
            lastPreparedTickSequenceNumber.Value != value.Tick.SequenceNumber)
        {
            throw new InvalidOperationException(
                "Produced signal values can be staged only after the store has been advanced to the same tick.");
        }

        if (stagedTickSequenceNumber.HasValue &&
            stagedTickSequenceNumber.Value != value.Tick.SequenceNumber)
        {
            throw new InvalidOperationException(
                "Produced signal values cannot be staged across multiple ticks before a later tick commits them.");
        }

        stagedTickSequenceNumber = value.Tick.SequenceNumber;
        stagedValues[value.SignalId] = value;
    }

    private void CommitStagedValuesBefore(long nextTickSequenceNumber)
    {
        if (!stagedTickSequenceNumber.HasValue ||
            stagedTickSequenceNumber.Value >= nextTickSequenceNumber ||
            stagedValues.Count == 0)
        {
            return;
        }

        foreach (var stagedValue in stagedValues)
        {
            committedValues[stagedValue.Key] = stagedValue.Value;
        }

        stagedValues.Clear();
        stagedTickSequenceNumber = null;
        currentSnapshot = SimulationSignalSnapshot.CreateCommitted(committedValues);
    }
}
