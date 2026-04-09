using StateKernel.Simulation.Scheduling;

namespace StateKernel.RuntimeHost.Execution;

/// <summary>
/// Represents the result of completing one deterministic run step.
/// </summary>
public sealed class SimulationRunStepResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SimulationRunStepResult" /> type.
    /// </summary>
    /// <param name="frame">The execution frame for the completed step.</param>
    /// <param name="status">The canonical run status after the step.</param>
    /// <param name="publishedUpdateCount">The number of runtime updates published for the step.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="frame" /> or <paramref name="status" /> is null.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="publishedUpdateCount" /> is negative.
    /// </exception>
    public SimulationRunStepResult(
        SchedulerExecutionFrame frame,
        SimulationRunStatus status,
        int publishedUpdateCount)
    {
        ArgumentNullException.ThrowIfNull(frame);
        ArgumentNullException.ThrowIfNull(status);

        if (publishedUpdateCount < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(publishedUpdateCount),
                "Published update counts cannot be negative.");
        }

        Frame = frame;
        Status = status;
        PublishedUpdateCount = publishedUpdateCount;
    }

    /// <summary>
    /// Gets the execution frame for the completed step.
    /// </summary>
    public SchedulerExecutionFrame Frame { get; }

    /// <summary>
    /// Gets the canonical run status after the step.
    /// </summary>
    public SimulationRunStatus Status { get; }

    /// <summary>
    /// Gets the number of runtime updates published for the step.
    /// </summary>
    public int PublishedUpdateCount { get; }
}
