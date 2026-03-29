namespace StateKernel.Runtime.Abstractions;

/// <summary>
/// Represents the result of stopping a runtime adapter.
/// </summary>
public sealed class RuntimeStopResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RuntimeStopResult" /> type.
    /// </summary>
    /// <param name="adapterKey">The stable adapter key that was stopped.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="adapterKey" /> is null, empty, or whitespace.
    /// </exception>
    public RuntimeStopResult(string adapterKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(adapterKey);
        AdapterKey = adapterKey.Trim();
    }

    /// <summary>
    /// Gets the stable adapter key that was stopped.
    /// </summary>
    public string AdapterKey { get; }
}
