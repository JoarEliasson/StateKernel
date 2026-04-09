namespace StateKernel.RuntimeHost.Execution;

/// <summary>
/// Represents the result of starting a simulation run.
/// </summary>
public sealed class SimulationRunStartResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SimulationRunStartResult" /> type.
    /// </summary>
    /// <param name="runId">The active run identifier.</param>
    /// <param name="status">The canonical run status after startup.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="runId" /> or <paramref name="status" /> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="status" /> is inactive.
    /// </exception>
    public SimulationRunStartResult(
        SimulationRunId runId,
        SimulationRunStatus status)
    {
        ArgumentNullException.ThrowIfNull(runId);
        ArgumentNullException.ThrowIfNull(status);

        if (!status.IsActive)
        {
            throw new ArgumentException(
                "Simulation run start results must include an active run status.",
                nameof(status));
        }

        RunId = runId;
        Status = status;
    }

    /// <summary>
    /// Gets the active run identifier.
    /// </summary>
    public SimulationRunId RunId { get; }

    /// <summary>
    /// Gets the canonical run status after startup.
    /// </summary>
    public SimulationRunStatus Status { get; }
}
