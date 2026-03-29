namespace StateKernel.Runtime.Abstractions;

/// <summary>
/// Captures the validated inputs required to start a concrete runtime adapter.
/// </summary>
public sealed class RuntimeStartRequest
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RuntimeStartRequest" /> type.
    /// </summary>
    /// <param name="adapterKey">The stable adapter key to start.</param>
    /// <param name="compiledPlan">The compiled runtime plan describing what to expose.</param>
    /// <param name="endpoint">The loopback endpoint settings used for hosting.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="adapterKey" /> is null, empty, or whitespace.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="compiledPlan" /> or <paramref name="endpoint" /> is null.
    /// </exception>
    public RuntimeStartRequest(
        string adapterKey,
        CompiledRuntimePlan compiledPlan,
        RuntimeEndpointSettings endpoint)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(adapterKey);
        ArgumentNullException.ThrowIfNull(compiledPlan);
        ArgumentNullException.ThrowIfNull(endpoint);

        AdapterKey = adapterKey.Trim();
        CompiledPlan = compiledPlan;
        Endpoint = endpoint;
    }

    /// <summary>
    /// Gets the stable adapter key to start.
    /// </summary>
    public string AdapterKey { get; }

    /// <summary>
    /// Gets the compiled runtime plan describing what to expose.
    /// </summary>
    public CompiledRuntimePlan CompiledPlan { get; }

    /// <summary>
    /// Gets the loopback endpoint settings used for hosting.
    /// </summary>
    public RuntimeEndpointSettings Endpoint { get; }
}
