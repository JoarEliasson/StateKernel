using StateKernel.Runtime.Abstractions;

namespace StateKernel.IntegrationTests;

internal sealed class ConfigurableFakeRuntimeAdapterFactory : IRuntimeAdapterFactory
{
    public ConfigurableFakeRuntimeAdapterFactory(
        string key = "fake",
        IReadOnlyList<RuntimeEndpointProfileId>? supportedEndpointProfiles = null)
    {
        Adapter = new ConfigurableFakeRuntimeAdapter(
            key,
            supportedEndpointProfiles ??
            [
                RuntimeEndpointProfiles.LocalDevelopment.Id,
                RuntimeEndpointProfiles.BaselineSecure.Id,
            ]);
    }

    public ConfigurableFakeRuntimeAdapter Adapter { get; }

    public RuntimeAdapterDescriptor Descriptor => Adapter.Descriptor;

    public IRuntimeAdapter CreateAdapter()
    {
        return Adapter;
    }
}

internal sealed class ConfigurableFakeRuntimeAdapter : IRuntimeAdapter
{
    private readonly List<IReadOnlyList<RuntimeValueUpdate>> appliedBatches = [];
    private readonly object sync = new();
    private int applyCallCount;
    private int concurrentApplyCalls;
    private int maxConcurrentApplyCalls;
    private int startCallCount;
    private int stopCallCount;

    public ConfigurableFakeRuntimeAdapter(
        string key,
        IReadOnlyList<RuntimeEndpointProfileId> supportedEndpointProfiles)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(supportedEndpointProfiles);

        Descriptor = new RuntimeAdapterDescriptor(
            key,
            "Configurable Fake Adapter",
            new RuntimeCapabilitySet([RuntimeCapability.ReadOnlyValueExposure]),
            supportedEndpointProfiles);
    }

    public RuntimeAdapterDescriptor Descriptor { get; }

    public TimeSpan ApplyDelay { get; set; }

    public int? ThrowOnStartCallNumber { get; set; }

    public int? ThrowOnApplyCallNumber { get; set; }

    public int? ThrowOnStopCallNumber { get; set; }

    public bool Started { get; private set; }

    public int StartCallCount => startCallCount;

    public int StopCallCount => stopCallCount;

    public int MaxConcurrentApplyCalls => maxConcurrentApplyCalls;

    public IReadOnlyList<RuntimeValueUpdate>[] AppliedBatches
    {
        get
        {
            lock (sync)
            {
                return appliedBatches.ToArray();
            }
        }
    }

    public ValueTask<RuntimeStartResult> StartAsync(
        RuntimeStartRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var callNumber = Interlocked.Increment(ref startCallCount);

        if (ThrowOnStartCallNumber == callNumber)
        {
            throw new InvalidOperationException("Configured adapter start failure.");
        }

        Started = true;

        var resolvedPort = request.Endpoint.Port == 0
            ? 40123 + callNumber
            : request.Endpoint.Port;

        return ValueTask.FromResult(
            new RuntimeStartResult(
                $"opc.tcp://{request.Endpoint.Host}:{resolvedPort}/{Descriptor.Key}",
                request.CompiledPlan.Bindings.Count));
    }

    public async ValueTask ApplyUpdatesAsync(
        IReadOnlyList<RuntimeValueUpdate> updates,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var callNumber = Interlocked.Increment(ref applyCallCount);
        var concurrentCalls = Interlocked.Increment(ref concurrentApplyCalls);

        UpdateMaxConcurrentApplyCalls(concurrentCalls);

        try
        {
            if (ApplyDelay > TimeSpan.Zero)
            {
                await Task.Delay(ApplyDelay, cancellationToken);
            }

            if (ThrowOnApplyCallNumber == callNumber)
            {
                throw new InvalidOperationException("Configured adapter apply failure.");
            }

            lock (sync)
            {
                appliedBatches.Add(updates.ToArray());
            }
        }
        finally
        {
            _ = Interlocked.Decrement(ref concurrentApplyCalls);
        }
    }

    public ValueTask<RuntimeStopResult> StopAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var callNumber = Interlocked.Increment(ref stopCallCount);
        Started = false;

        if (ThrowOnStopCallNumber == callNumber)
        {
            throw new InvalidOperationException("Configured adapter stop failure.");
        }

        return ValueTask.FromResult(new RuntimeStopResult(Descriptor.Key));
    }

    private void UpdateMaxConcurrentApplyCalls(int candidateValue)
    {
        while (true)
        {
            var currentMax = maxConcurrentApplyCalls;

            if (candidateValue <= currentMax)
            {
                return;
            }

            if (Interlocked.CompareExchange(
                    ref maxConcurrentApplyCalls,
                    candidateValue,
                    currentMax) == currentMax)
            {
                return;
            }
        }
    }
}
