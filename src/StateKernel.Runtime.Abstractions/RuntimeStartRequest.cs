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
    /// <param name="endpoint">The endpoint settings used for hosting.</param>
    /// <param name="endpointProfile">The bounded endpoint/security profile to apply at startup.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="adapterKey" /> is null, empty, or whitespace.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="compiledPlan" />, <paramref name="endpoint" />, or
    /// <paramref name="endpointProfile" /> is null.
    /// </exception>
    public RuntimeStartRequest(
        string adapterKey,
        CompiledRuntimePlan compiledPlan,
        RuntimeEndpointSettings endpoint,
        RuntimeEndpointProfile endpointProfile)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(adapterKey);
        ArgumentNullException.ThrowIfNull(compiledPlan);
        ArgumentNullException.ThrowIfNull(endpoint);
        ArgumentNullException.ThrowIfNull(endpointProfile);

        AdapterKey = adapterKey.Trim();
        CompiledPlan = compiledPlan;
        Endpoint = endpoint;
        EndpointProfile = endpointProfile;
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
    /// Gets the endpoint settings used for hosting.
    /// </summary>
    public RuntimeEndpointSettings Endpoint { get; }

    /// <summary>
    /// Gets the bounded endpoint/security profile to apply at startup.
    /// </summary>
    public RuntimeEndpointProfile EndpointProfile { get; }
}
