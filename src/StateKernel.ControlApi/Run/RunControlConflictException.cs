namespace StateKernel.ControlApi.Run;

/// <summary>
/// Represents a bounded control API lifecycle conflict for run start/stop operations.
/// </summary>
internal sealed class RunControlConflictException : InvalidOperationException
{
    public RunControlConflictException(string message)
        : base(message)
    {
    }
}
