namespace StateKernel.Simulation.Behaviors;

/// <summary>
/// Defines a sink for behavior execution records.
/// </summary>
public interface IBehaviorOutputSink
{
    /// <summary>
    /// Records a behavior execution result.
    /// </summary>
    /// <param name="record">The behavior execution record to capture.</param>
    void Record(BehaviorExecutionRecord record);
}
