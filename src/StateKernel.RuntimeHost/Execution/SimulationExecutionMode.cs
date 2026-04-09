namespace StateKernel.RuntimeHost.Execution;

/// <summary>
/// Defines the execution mode used by a simulation run.
/// </summary>
public enum SimulationExecutionMode
{
    /// <summary>
    /// The run advances only through explicit step requests.
    /// </summary>
    Manual = 0,

    /// <summary>
    /// The run advances through the orchestrator's internal loop.
    /// </summary>
    Continuous = 1,
}
