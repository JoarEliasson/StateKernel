namespace StateKernel.RuntimeHost.Execution;

/// <summary>
/// Represents the bounded fault information retained by the run orchestrator while inactive after
/// an internal failure.
/// </summary>
public sealed class SimulationRunFaultInfo
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SimulationRunFaultInfo" /> type.
    /// </summary>
    /// <param name="faultCode">The stable bounded fault code.</param>
    /// <param name="message">The bounded product-facing fault message.</param>
    /// <param name="occurredAtUtc">The UTC timestamp at which the fault occurred.</param>
    /// <param name="lastCompletedTick">The last completed deterministic tick, if one existed.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="faultCode" /> or <paramref name="message" /> is null, empty, or whitespace.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="occurredAtUtc" /> is default or <paramref name="lastCompletedTick" /> is negative.
    /// </exception>
    public SimulationRunFaultInfo(
        string faultCode,
        string message,
        DateTimeOffset occurredAtUtc,
        long? lastCompletedTick)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(faultCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        if (occurredAtUtc == default)
        {
            throw new ArgumentOutOfRangeException(
                nameof(occurredAtUtc),
                "Simulation run fault timestamps must be non-default UTC values.");
        }

        if (lastCompletedTick is < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(lastCompletedTick),
                "Completed tick values cannot be negative.");
        }

        FaultCode = faultCode.Trim();
        Message = message.Trim();
        OccurredAtUtc = occurredAtUtc;
        LastCompletedTick = lastCompletedTick;
    }

    /// <summary>
    /// Gets the stable bounded fault code.
    /// </summary>
    public string FaultCode { get; }

    /// <summary>
    /// Gets the bounded product-facing fault message.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Gets the UTC timestamp at which the fault occurred.
    /// </summary>
    public DateTimeOffset OccurredAtUtc { get; }

    /// <summary>
    /// Gets the last completed deterministic tick, if one existed before the fault.
    /// </summary>
    public long? LastCompletedTick { get; }
}
