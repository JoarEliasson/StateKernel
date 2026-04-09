namespace StateKernel.ControlApi.Runtime;

/// <summary>
/// Represents a bounded control API lifecycle conflict for runtime start/stop operations.
/// </summary>
internal sealed class RuntimeControlConflictException : InvalidOperationException
{
    public RuntimeControlConflictException(string message)
        : base(message)
    {
    }
}
