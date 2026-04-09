namespace StateKernel.RuntimeHost.Hosting;

/// <summary>
/// Represents a bounded runtime-host internal failure that should surface as an initiating
/// lifecycle failure while leaving the host inactive with retained fault state.
/// </summary>
public sealed class RuntimeHostFaultException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RuntimeHostFaultException" /> type.
    /// </summary>
    /// <param name="faultInfo">The bounded retained fault information.</param>
    /// <param name="innerException">The original internal exception, if any.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="faultInfo" /> is null.
    /// </exception>
    public RuntimeHostFaultException(
        RuntimeHostFaultInfo faultInfo,
        Exception? innerException = null)
        : base(faultInfo?.Message, innerException)
    {
        ArgumentNullException.ThrowIfNull(faultInfo);
        FaultInfo = faultInfo;
    }

    /// <summary>
    /// Gets the bounded retained fault information.
    /// </summary>
    public RuntimeHostFaultInfo FaultInfo { get; }
}
