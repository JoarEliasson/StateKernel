using System.Collections.ObjectModel;

namespace StateKernel.Simulation.Behaviors;

/// <summary>
/// Captures behavior execution records in deterministic insertion order.
/// </summary>
public sealed class BehaviorExecutionRecorder : IBehaviorOutputSink
{
    private readonly List<BehaviorExecutionRecord> records = [];
    private readonly ReadOnlyCollection<BehaviorExecutionRecord> readOnlyRecords;

    /// <summary>
    /// Initializes a new instance of the <see cref="BehaviorExecutionRecorder" /> type.
    /// </summary>
    public BehaviorExecutionRecorder()
    {
        readOnlyRecords = records.AsReadOnly();
    }

    /// <summary>
    /// Gets the recorded behavior executions in insertion order.
    /// </summary>
    public IReadOnlyList<BehaviorExecutionRecord> Records => readOnlyRecords;

    /// <inheritdoc />
    public void Record(BehaviorExecutionRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        records.Add(record);
    }
}
