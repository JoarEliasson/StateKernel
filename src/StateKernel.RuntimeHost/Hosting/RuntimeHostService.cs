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
    private RuntimeHostStatus status = RuntimeHostStatus.Inactive;

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
    public bool IsRunning => status.IsRunning;

    /// <summary>
    /// Gets the active adapter key when a runtime adapter is running.
    /// </summary>
    public string? ActiveAdapterKey => status.ActiveAdapterKey;

    /// <summary>
    /// Gets the active externally readable endpoint URL when a runtime adapter is running.
    /// </summary>
    public string? EndpointUrl => status.EndpointUrl;

    /// <summary>
    /// Gets the canonical runtime host status snapshot.
    /// </summary>
    /// <returns>The canonical runtime host status snapshot.</returns>
    public RuntimeHostStatus GetStatus()
    {
        return status;
    }

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
    /// <exception cref="RuntimeHostFaultException">
    /// Thrown when adapter creation or startup fails internally.
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

        if (!factory.Descriptor.SupportedEndpointProfiles.Contains(request.EndpointProfile.Id))
        {
            throw new InvalidOperationException(
                $"Runtime adapter '{request.AdapterKey}' does not support endpoint/profile id '{request.EndpointProfile.Id}'.");
        }

        IRuntimeAdapter adapter;

        try
        {
            adapter = factory.CreateAdapter();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            var faultInfo = CreateStartFaultInfo(request.EndpointProfile);
            status = RuntimeHostStatus.Faulted(faultInfo);
            throw new RuntimeHostFaultException(faultInfo, exception);
        }

        try
        {
            var startResult = await adapter.StartAsync(request, cancellationToken);

            activeAdapter = adapter;
            activeCompiledPlan = request.CompiledPlan;
            status = RuntimeHostStatus.Active(
                adapter.Descriptor.Key,
                startResult.EndpointUrl,
                request.EndpointProfile.Id,
                startResult.ExposedNodeCount);

            return startResult;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            await BestEffortStopAdapterAsync(adapter, CancellationToken.None);

            activeAdapter = null;
            activeCompiledPlan = null;

            var faultInfo = CreateStartFaultInfo(request.EndpointProfile);
            status = RuntimeHostStatus.Faulted(faultInfo);
            throw new RuntimeHostFaultException(faultInfo, exception);
        }
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
    /// <exception cref="RuntimeHostFaultException">
    /// Thrown when the active adapter fails while applying the published update batch.
    /// </exception>
    public async ValueTask ApplyUpdatesAsync(
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
            return;
        }

        var unknownUpdate = updates.FirstOrDefault(
            update => !activeCompiledPlan.TryGetBinding(update.SourceSignalId, out _));

        if (unknownUpdate is not null)
        {
            throw new InvalidOperationException(
                $"Runtime update batches cannot reference unprojected signals. Unknown signal id: '{unknownUpdate.SourceSignalId}'.");
        }

        var adapter = activeAdapter;

        try
        {
            await adapter.ApplyUpdatesAsync(updates, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            await BestEffortStopAdapterAsync(adapter, CancellationToken.None);
            ClearActiveState();

            var faultInfo = CreateFaultInfo(
                RuntimeHostFaultCodes.RuntimeApplyFailed,
                "The runtime adapter failed while applying published value updates.");
            status = RuntimeHostStatus.Faulted(faultInfo);
            throw new RuntimeHostFaultException(faultInfo, exception);
        }
    }

    /// <summary>
    /// Stops the active runtime adapter instance.
    /// </summary>
    /// <param name="cancellationToken">The token that cancels the stop operation.</param>
    /// <returns>The runtime stop result.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the host is not currently running.
    /// </exception>
    /// <exception cref="RuntimeHostFaultException">
    /// Thrown when the active adapter fails while stopping.
    /// </exception>
    public async ValueTask<RuntimeStopResult> StopAsync(CancellationToken cancellationToken)
    {
        if (activeAdapter is null)
        {
            throw new InvalidOperationException(
                "The runtime host is not currently running.");
        }

        var adapter = activeAdapter;

        try
        {
            var stopResult = await adapter.StopAsync(cancellationToken);
            ClearActiveState();
            status = RuntimeHostStatus.Inactive;
            return stopResult;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            ClearActiveState();

            var faultInfo = CreateFaultInfo(
                RuntimeHostFaultCodes.RuntimeStopFailed,
                "The runtime adapter failed while stopping.");
            status = RuntimeHostStatus.Faulted(faultInfo);
            throw new RuntimeHostFaultException(faultInfo, exception);
        }
    }

    private static async Task BestEffortStopAdapterAsync(
        IRuntimeAdapter adapter,
        CancellationToken cancellationToken)
    {
        try
        {
            _ = await adapter.StopAsync(cancellationToken);
        }
        catch
        {
        }
    }

    private static RuntimeHostFaultInfo CreateFaultInfo(
        string faultCode,
        string message)
    {
        return new RuntimeHostFaultInfo(
            faultCode,
            message,
            DateTimeOffset.UtcNow);
    }

    private static RuntimeHostFaultInfo CreateStartFaultInfo(RuntimeEndpointProfile endpointProfile)
    {
        ArgumentNullException.ThrowIfNull(endpointProfile);

        if (endpointProfile.Id == RuntimeEndpointProfiles.BaselineSecure.Id)
        {
            return CreateFaultInfo(
                RuntimeHostFaultCodes.SecureStartupFailed,
                "The runtime adapter could not realize the requested secure endpoint/profile.");
        }

        return CreateFaultInfo(
            RuntimeHostFaultCodes.RuntimeStartFailed,
            "The runtime adapter could not start.");
    }

    private void ClearActiveState()
    {
        activeAdapter = null;
        activeCompiledPlan = null;
    }
}
