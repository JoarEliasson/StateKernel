using StateKernel.Runtime.Abstractions;

namespace StateKernel.RuntimeHost.Hosting;

/// <summary>
/// Owns the lifecycle of a single active runtime adapter instance.
/// </summary>
public sealed class RuntimeHostService
{
    private readonly Dictionary<string, IRuntimeAdapterFactory> factoriesByKey;
    private IRuntimeAdapter? activeAdapter;
    private CompiledRuntimePlan? activeCompiledPlan;
    private RuntimeStartResult? activeStartResult;

    /// <summary>
    /// Initializes a new instance of the <see cref="RuntimeHostService" /> type.
    /// </summary>
    /// <param name="adapterFactories">The runtime adapter factories available to the host.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="adapterFactories" /> is null.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the supplied factories contain null entries or duplicate adapter keys.
    /// </exception>
    public RuntimeHostService(IEnumerable<IRuntimeAdapterFactory> adapterFactories)
    {
        ArgumentNullException.ThrowIfNull(adapterFactories);

        var materializedFactories = adapterFactories.ToArray();

        if (Array.Exists(materializedFactories, static factory => factory is null))
        {
            throw new InvalidOperationException(
                "Runtime host services cannot be configured with null adapter factories.");
        }

        var duplicateFactoryKey = materializedFactories
            .GroupBy(static factory => factory.Descriptor.Key, StringComparer.Ordinal)
            .FirstOrDefault(static group => group.Count() > 1)?
            .Key;

        if (duplicateFactoryKey is not null)
        {
            throw new InvalidOperationException(
                $"Runtime adapter factory keys must be unique. Duplicate adapter key: '{duplicateFactoryKey}'.");
        }

        factoriesByKey = materializedFactories.ToDictionary(
            static factory => factory.Descriptor.Key,
            StringComparer.Ordinal);
    }

    /// <summary>
    /// Gets a value indicating whether a runtime adapter is currently active.
    /// </summary>
    public bool IsRunning => activeAdapter is not null;

    /// <summary>
    /// Gets the active adapter key when a runtime adapter is running.
    /// </summary>
    public string? ActiveAdapterKey => activeAdapter?.Descriptor.Key;

    /// <summary>
    /// Gets the active externally readable endpoint URL when a runtime adapter is running.
    /// </summary>
    public string? EndpointUrl => activeStartResult?.EndpointUrl;

    /// <summary>
    /// Starts one runtime adapter instance from the supplied start request.
    /// </summary>
    /// <param name="request">The validated runtime start request.</param>
    /// <param name="cancellationToken">The token that cancels the start operation.</param>
    /// <returns>The runtime start result.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="request" /> is null.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the host is already running or when the requested adapter key is unknown.
    /// </exception>
    public async ValueTask<RuntimeStartResult> StartAsync(
        RuntimeStartRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (activeAdapter is not null)
        {
            throw new InvalidOperationException(
                "The runtime host is already running an adapter instance.");
        }

        if (!factoriesByKey.TryGetValue(request.AdapterKey, out var factory))
        {
            throw new InvalidOperationException(
                $"No runtime adapter factory is registered for adapter key '{request.AdapterKey}'.");
        }

        var adapter = factory.CreateAdapter();
        var startResult = await adapter.StartAsync(request, cancellationToken);

        activeAdapter = adapter;
        activeCompiledPlan = request.CompiledPlan;
        activeStartResult = startResult;

        return startResult;
    }

    /// <summary>
    /// Applies an ordered batch of runtime value updates to the active runtime adapter.
    /// </summary>
    /// <param name="updates">The ordered runtime value updates to forward.</param>
    /// <param name="cancellationToken">The token that cancels the update operation.</param>
    /// <returns>A task representing the asynchronous update operation.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="updates" /> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="updates" /> contains null updates.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the host is not running or when the batch references unprojected signals.
    /// </exception>
    public ValueTask ApplyUpdatesAsync(
        IReadOnlyList<RuntimeValueUpdate> updates,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(updates);

        if (activeAdapter is null || activeCompiledPlan is null)
        {
            throw new InvalidOperationException(
                "The runtime host must be started before updates can be applied.");
        }

        if (updates.Any(static update => update is null))
        {
            throw new ArgumentException(
                "Runtime update batches cannot contain null updates.",
                nameof(updates));
        }

        if (updates.Count == 0)
        {
            return ValueTask.CompletedTask;
        }

        var unknownUpdate = updates.FirstOrDefault(
            update => !activeCompiledPlan.TryGetBinding(update.SourceSignalId, out _));

        if (unknownUpdate is not null)
        {
            throw new InvalidOperationException(
                $"Runtime update batches cannot reference unprojected signals. Unknown signal id: '{unknownUpdate.SourceSignalId}'.");
        }

        return activeAdapter.ApplyUpdatesAsync(updates, cancellationToken);
    }

    /// <summary>
    /// Stops the active runtime adapter instance.
    /// </summary>
    /// <param name="cancellationToken">The token that cancels the stop operation.</param>
    /// <returns>The runtime stop result.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the host is not currently running.
    /// </exception>
    public async ValueTask<RuntimeStopResult> StopAsync(CancellationToken cancellationToken)
    {
        if (activeAdapter is null)
        {
            throw new InvalidOperationException(
                "The runtime host is not currently running.");
        }

        var adapter = activeAdapter;
        var stopResult = await adapter.StopAsync(cancellationToken);

        activeAdapter = null;
        activeCompiledPlan = null;
        activeStartResult = null;

        return stopResult;
    }
}
