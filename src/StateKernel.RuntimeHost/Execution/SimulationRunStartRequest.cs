using StateKernel.Runtime.Abstractions;

namespace StateKernel.RuntimeHost.Execution;

/// <summary>
/// Captures the validated inputs required to start a simulation run and its attached runtime.
/// </summary>
public sealed class SimulationRunStartRequest
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SimulationRunStartRequest" /> type.
    /// </summary>
    /// <param name="executableDefinition">The executable run definition that should be started.</param>
    /// <param name="runtimeStartRequest">The validated runtime start request to attach.</param>
    /// <param name="executionSettings">The execution settings for the run.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when any supplied value is null.
    /// </exception>
    public SimulationRunStartRequest(
        ISimulationExecutableRunDefinition executableDefinition,
        RuntimeStartRequest runtimeStartRequest,
        SimulationExecutionSettings executionSettings)
    {
        ArgumentNullException.ThrowIfNull(executableDefinition);
        ArgumentNullException.ThrowIfNull(runtimeStartRequest);
        ArgumentNullException.ThrowIfNull(executionSettings);

        ExecutableDefinition = executableDefinition;
        RuntimeStartRequest = runtimeStartRequest;
        ExecutionSettings = executionSettings;
    }

    /// <summary>
    /// Gets the executable run definition that should be started.
    /// </summary>
    public ISimulationExecutableRunDefinition ExecutableDefinition { get; }

    /// <summary>
    /// Gets the validated runtime start request to attach.
    /// </summary>
    public RuntimeStartRequest RuntimeStartRequest { get; }

    /// <summary>
    /// Gets the execution settings for the run.
    /// </summary>
    public SimulationExecutionSettings ExecutionSettings { get; }
}
