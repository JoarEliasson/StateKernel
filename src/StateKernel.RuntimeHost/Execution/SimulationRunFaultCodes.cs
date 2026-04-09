namespace StateKernel.RuntimeHost.Execution;

/// <summary>
/// Defines the closed set of bounded simulation run fault codes.
/// </summary>
public static class SimulationRunFaultCodes
{
    /// <summary>
    /// The run could not attach to the runtime during startup.
    /// </summary>
    public const string RuntimeAttachFailed = "runtime-attach-failed";

    /// <summary>
    /// The run could not complete its mandatory initial deterministic step during startup.
    /// </summary>
    public const string InitialStepFailed = "initial-step-failed";

    /// <summary>
    /// The active run failed while advancing and publishing its deterministic execution loop.
    /// </summary>
    public const string ContinuousStepFailed = "continuous-step-failed";

    /// <summary>
    /// The active run failed while executing an explicit deterministic step.
    /// </summary>
    public const string StepFailed = "step-failed";

    /// <summary>
    /// The active run failed while stopping.
    /// </summary>
    public const string RunStopFailed = "run-stop-failed";
}
