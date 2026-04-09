namespace StateKernel.RuntimeHost.Execution;

/// <summary>
/// Represents a bounded run-orchestration internal failure that should surface as an initiating
/// lifecycle failure while leaving the run inactive with retained fault state.
/// </summary>
public sealed class SimulationRunFaultException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SimulationRunFaultException" /> type.
    /// </summary>
    /// <param name="faultInfo">The bounded retained fault information.</param>
    /// <param name="innerException">The original internal exception, if any.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="faultInfo" /> is null.
    /// </exception>
    public SimulationRunFaultException(
        SimulationRunFaultInfo faultInfo,
        Exception? innerException = null)
        : base(faultInfo?.Message, innerException)
    {
        ArgumentNullException.ThrowIfNull(faultInfo);
        FaultInfo = faultInfo;
    }

    /// <summary>
    /// Gets the bounded retained fault information.
    /// </summary>
    public SimulationRunFaultInfo FaultInfo { get; }
}
