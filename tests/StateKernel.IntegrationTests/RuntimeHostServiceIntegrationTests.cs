using StateKernel.Runtime.Abstractions;
using StateKernel.RuntimeHost.Hosting;
using StateKernel.Simulation.Behaviors;
using StateKernel.Simulation.Clock;
using StateKernel.Simulation.Signals;

namespace StateKernel.IntegrationTests;

public sealed class RuntimeHostServiceIntegrationTests
{
    private static readonly SimulationSignalId SourceSignal = SimulationSignalId.From("Source");
    private static readonly SimulationSignalId SecondarySignal = SimulationSignalId.From("Secondary");

    [Fact]
    public async Task RuntimeHostService_StartApplyStopFlow_ForwardsLifecycleAndUpdates()
    {
        var factory = new FakeRuntimeAdapterFactory("fake");
        var host = new RuntimeHostService([factory]);
        var request = new RuntimeStartRequest(
            "fake",
            CreateCompiledPlan(SourceSignal),
            RuntimeEndpointSettings.Loopback());

        var startResult = await host.StartAsync(request, CancellationToken.None);
        await host.ApplyUpdatesAsync(
            [
                new RuntimeValueUpdate(SourceSignal, 12.5, 3),
            ],
            CancellationToken.None);
        var stopResult = await host.StopAsync(CancellationToken.None);

        Assert.Equal(1, factory.Adapter.StartCallCount);
        Assert.Single(factory.Adapter.AppliedBatches);
        Assert.Equal("fake", startResult.EndpointUrl);
        Assert.Equal("fake", stopResult.AdapterKey);
        Assert.False(host.IsRunning);
    }

    [Fact]
    public async Task RuntimeHostService_RejectsInvalidLifecycleUsageAndUnprojectedSignals()
    {
        var factory = new FakeRuntimeAdapterFactory("fake");
        var host = new RuntimeHostService([factory]);
        var request = new RuntimeStartRequest(
            "fake",
            CreateCompiledPlan(SourceSignal),
            RuntimeEndpointSettings.Loopback());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            host.ApplyUpdatesAsync(
                [
                    new RuntimeValueUpdate(SourceSignal, 1.0, 1),
                ],
                CancellationToken.None).AsTask());
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            host.StopAsync(CancellationToken.None).AsTask());

        _ = await host.StartAsync(request, CancellationToken.None);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            host.StartAsync(request, CancellationToken.None).AsTask());
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            host.ApplyUpdatesAsync(
                [
                    new RuntimeValueUpdate(SecondarySignal, 1.0, 1),
                ],
                CancellationToken.None).AsTask());
    }

    [Fact]
    public void RuntimeValueUpdateProjector_ProjectsCommittedSignalsInCompiledBindingOrder()
    {
        var signalStore = new SimulationSignalValueStore();
        var firstTick = CreateTick(1);
        var secondTick = CreateTick(2);

        signalStore.GetCommittedSnapshotForTick(firstTick);
        signalStore.RecordProducedValue(
            new SimulationSignalValue(SecondarySignal, firstTick, new BehaviorSample(9.0)));
        signalStore.RecordProducedValue(
            new SimulationSignalValue(SourceSignal, firstTick, new BehaviorSample(5.0)));

        var committedSnapshot = signalStore.GetCommittedSnapshotForTick(secondTick);
        var updates = RuntimeValueUpdateProjector.CreateUpdates(
            new CompiledRuntimePlan(
                new RuntimeProjectionPlan(
                [
                    new SimulationSignalProjection(SecondarySignal, RuntimeNodeId.ForSignal(SecondarySignal)),
                    new SimulationSignalProjection(SourceSignal, RuntimeNodeId.ForSignal(SourceSignal)),
                ])),
            committedSnapshot);

        Assert.Collection(
            updates,
            update =>
            {
                Assert.Equal(SecondarySignal, update.SourceSignalId);
                Assert.Equal(9.0, update.Value);
                Assert.Equal(1, update.SourceTickSequenceNumber);
            },
            update =>
            {
                Assert.Equal(SourceSignal, update.SourceSignalId);
                Assert.Equal(5.0, update.Value);
                Assert.Equal(1, update.SourceTickSequenceNumber);
            });
    }

    private static CompiledRuntimePlan CreateCompiledPlan(SimulationSignalId signalId)
    {
        return new CompiledRuntimePlan(
            new RuntimeProjectionPlan(
            [
                new SimulationSignalProjection(signalId, RuntimeNodeId.ForSignal(signalId)),
            ]));
    }

    private static SimulationTick CreateTick(long sequenceNumber)
    {
        return new SimulationTick(sequenceNumber, TimeSpan.FromMilliseconds(sequenceNumber * 10));
    }

    private sealed class FakeRuntimeAdapterFactory : IRuntimeAdapterFactory
    {
        public FakeRuntimeAdapterFactory(string key)
        {
            Adapter = new FakeRuntimeAdapter(key);
        }

        public FakeRuntimeAdapter Adapter { get; }

        public RuntimeAdapterDescriptor Descriptor => Adapter.Descriptor;

        public IRuntimeAdapter CreateAdapter()
        {
            return Adapter;
        }
    }

    private sealed class FakeRuntimeAdapter : IRuntimeAdapter
    {
        public FakeRuntimeAdapter(string key)
        {
            Descriptor = new RuntimeAdapterDescriptor(
                key,
                "Fake Adapter",
                new RuntimeCapabilitySet([RuntimeCapability.ReadOnlyValueExposure]));
        }

        public List<IReadOnlyList<RuntimeValueUpdate>> AppliedBatches { get; } = [];

        public RuntimeAdapterDescriptor Descriptor { get; }

        public bool Started { get; private set; }

        public int StartCallCount { get; private set; }

        public ValueTask<RuntimeStartResult> StartAsync(
            RuntimeStartRequest request,
            CancellationToken cancellationToken)
        {
            Started = true;
            StartCallCount++;
            return ValueTask.FromResult(new RuntimeStartResult(Descriptor.Key, request.CompiledPlan.Bindings.Count));
        }

        public ValueTask ApplyUpdatesAsync(
            IReadOnlyList<RuntimeValueUpdate> updates,
            CancellationToken cancellationToken)
        {
            AppliedBatches.Add(updates.ToArray());
            return ValueTask.CompletedTask;
        }

        public ValueTask<RuntimeStopResult> StopAsync(CancellationToken cancellationToken)
        {
            Started = false;
            return ValueTask.FromResult(new RuntimeStopResult(Descriptor.Key));
        }
    }
}
