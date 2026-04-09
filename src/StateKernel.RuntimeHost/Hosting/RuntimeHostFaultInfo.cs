namespace StateKernel.RuntimeHost.Hosting;

/// <summary>
/// Represents the bounded fault information retained by the runtime host while inactive after an
/// internal failure.
/// </summary>
public sealed class RuntimeHostFaultInfo
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RuntimeHostFaultInfo" /> type.
    /// </summary>
    /// <param name="faultCode">The stable bounded fault code.</param>
    /// <param name="message">The bounded product-facing fault message.</param>
    /// <param name="occurredAtUtc">The UTC timestamp at which the fault occurred.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="faultCode" /> or <paramref name="message" /> is null, empty, or whitespace.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="occurredAtUtc" /> is the default value.
    /// </exception>
    public RuntimeHostFaultInfo(
        string faultCode,
        string message,
        DateTimeOffset occurredAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(faultCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        if (occurredAtUtc == default)
        {
            throw new ArgumentOutOfRangeException(
                nameof(occurredAtUtc),
                "Runtime host fault timestamps must be non-default UTC values.");
        }

        FaultCode = faultCode.Trim();
        Message = message.Trim();
        OccurredAtUtc = occurredAtUtc;
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
}
