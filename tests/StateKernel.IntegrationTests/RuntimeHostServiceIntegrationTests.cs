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
    public void RuntimeHostStatus_EnforcesActiveInactiveAndFaultContracts()
    {
        var faultInfo = new RuntimeHostFaultInfo(
            RuntimeHostFaultCodes.RuntimeStartFailed,
            "Faulted host.",
            DateTimeOffset.UtcNow);

        Assert.False(RuntimeHostStatus.Inactive.IsRunning);
        Assert.Null(RuntimeHostStatus.Inactive.LastFault);

        var faultedStatus = RuntimeHostStatus.Faulted(faultInfo);

        Assert.False(faultedStatus.IsRunning);
        Assert.Equal(faultInfo, faultedStatus.LastFault);

        var activeStatus = RuntimeHostStatus.Active(
            "fake",
            "opc.tcp://127.0.0.1:40123/fake",
            RuntimeEndpointProfiles.LocalDevelopment.Id,
            1);

        Assert.True(activeStatus.IsRunning);
        Assert.Null(activeStatus.LastFault);

        Assert.Null(activeStatus.LastFault);
    }

    [Fact]
    public async Task RuntimeHostService_StartApplyStopFlow_ForwardsLifecycleAndUpdates()
    {
        var factory = new ConfigurableFakeRuntimeAdapterFactory();
        var host = new RuntimeHostService([factory]);
        var request = CreateStartRequest("fake", RuntimeEndpointProfiles.LocalDevelopment);

        var startResult = await host.StartAsync(request, CancellationToken.None);
        await host.ApplyUpdatesAsync(
            [
                new RuntimeValueUpdate(SourceSignal, 12.5, 3),
            ],
            CancellationToken.None);
        var stopResult = await host.StopAsync(CancellationToken.None);

        Assert.Equal(1, factory.Adapter.StartCallCount);
        Assert.Single(factory.Adapter.AppliedBatches);
        Assert.Equal("opc.tcp://127.0.0.1:40124/fake", startResult.EndpointUrl);
        Assert.Equal("fake", stopResult.AdapterKey);
        Assert.False(host.IsRunning);
        Assert.Null(host.GetStatus().LastFault);
    }

    [Fact]
    public async Task RuntimeHostService_InvalidLifecycleMisuseDoesNotRetainFaults()
    {
        var factory = new ConfigurableFakeRuntimeAdapterFactory();
        var host = new RuntimeHostService([factory]);
        var request = CreateStartRequest("fake", RuntimeEndpointProfiles.LocalDevelopment);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            host.ApplyUpdatesAsync(
                [
                    new RuntimeValueUpdate(SourceSignal, 1.0, 1),
                ],
                CancellationToken.None).AsTask());
        Assert.Null(host.GetStatus().LastFault);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            host.StopAsync(CancellationToken.None).AsTask());
        Assert.Null(host.GetStatus().LastFault);

        _ = await host.StartAsync(request, CancellationToken.None);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            host.StartAsync(request, CancellationToken.None).AsTask());
        Assert.Null(host.GetStatus().LastFault);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            host.ApplyUpdatesAsync(
                [
                    new RuntimeValueUpdate(SecondarySignal, 1.0, 1),
                ],
                CancellationToken.None).AsTask());
        Assert.Null(host.GetStatus().LastFault);
    }

    [Fact]
    public async Task RuntimeHostService_GetStatusUsesTheCanonicalRuntimeHostReadModel()
    {
        var factory = new ConfigurableFakeRuntimeAdapterFactory();
        var host = new RuntimeHostService([factory]);
        var inactiveStatus = host.GetStatus();
        var request = CreateStartRequest("fake", RuntimeEndpointProfiles.LocalDevelopment);

        Assert.False(inactiveStatus.IsRunning);
        Assert.Null(inactiveStatus.ActiveAdapterKey);
        Assert.Null(inactiveStatus.EndpointUrl);
        Assert.Null(inactiveStatus.ActiveProfileId);
        Assert.Null(inactiveStatus.ExposedNodeCount);
        Assert.Null(inactiveStatus.LastFault);

        var startResult = await host.StartAsync(request, CancellationToken.None);
        var activeStatus = host.GetStatus();

        Assert.True(activeStatus.IsRunning);
        Assert.Equal("fake", activeStatus.ActiveAdapterKey);
        Assert.Equal(startResult.EndpointUrl, activeStatus.EndpointUrl);
        Assert.Equal(RuntimeEndpointProfiles.LocalDevelopment.Id, activeStatus.ActiveProfileId);
        Assert.Equal(1, activeStatus.ExposedNodeCount);
        Assert.Null(activeStatus.LastFault);

        _ = await host.StopAsync(CancellationToken.None);

        var stoppedStatus = host.GetStatus();

        Assert.False(stoppedStatus.IsRunning);
        Assert.Null(stoppedStatus.ActiveAdapterKey);
        Assert.Null(stoppedStatus.EndpointUrl);
        Assert.Null(stoppedStatus.ActiveProfileId);
        Assert.Null(stoppedStatus.ExposedNodeCount);
        Assert.Null(stoppedStatus.LastFault);
    }

    [Fact]
    public async Task RuntimeHostService_RejectsUnsupportedEndpointProfilesBeforeAdapterStartupWithoutRetainingFaults()
    {
        var factory = new ConfigurableFakeRuntimeAdapterFactory(
            supportedEndpointProfiles:
            [
                RuntimeEndpointProfiles.LocalDevelopment.Id,
            ]);
        var host = new RuntimeHostService([factory]);
        var request = CreateStartRequest("fake", RuntimeEndpointProfiles.BaselineSecure);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            host.StartAsync(request, CancellationToken.None).AsTask());

        Assert.Contains(RuntimeEndpointProfiles.BaselineSecure.Id.Value, exception.Message);
        Assert.Equal(0, factory.Adapter.StartCallCount);
        Assert.False(host.GetStatus().IsRunning);
        Assert.Null(host.GetStatus().LastFault);
    }

    [Fact]
    public async Task RuntimeHostService_StartFailure_LeavesInactiveFaultedState()
    {
        var factory = new ConfigurableFakeRuntimeAdapterFactory();
        factory.Adapter.ThrowOnStartCallNumber = 1;
        var host = new RuntimeHostService([factory]);

        var exception = await Assert.ThrowsAsync<RuntimeHostFaultException>(() =>
            host.StartAsync(
                CreateStartRequest("fake", RuntimeEndpointProfiles.LocalDevelopment),
                CancellationToken.None).AsTask());

        var status = host.GetStatus();

        Assert.Equal(RuntimeHostFaultCodes.RuntimeStartFailed, exception.FaultInfo.FaultCode);
        Assert.False(status.IsRunning);
        Assert.Null(status.ActiveAdapterKey);
        Assert.Null(status.EndpointUrl);
        Assert.Null(status.ActiveProfileId);
        Assert.Null(status.ExposedNodeCount);
        Assert.NotNull(status.LastFault);
        Assert.Equal(RuntimeHostFaultCodes.RuntimeStartFailed, status.LastFault!.FaultCode);
    }

    [Fact]
    public async Task RuntimeHostService_SecureStartFailure_LeavesInactiveFaultedStateAndSuccessfulRestartClearsFault()
    {
        var factory = new ConfigurableFakeRuntimeAdapterFactory();
        factory.Adapter.ThrowOnStartCallNumber = 1;
        var host = new RuntimeHostService([factory]);

        _ = await Assert.ThrowsAsync<RuntimeHostFaultException>(() =>
            host.StartAsync(
                CreateStartRequest("fake", RuntimeEndpointProfiles.BaselineSecure),
                CancellationToken.None).AsTask());

        var faultedStatus = host.GetStatus();

        Assert.False(faultedStatus.IsRunning);
        Assert.NotNull(faultedStatus.LastFault);
        Assert.Equal(RuntimeHostFaultCodes.SecureStartupFailed, faultedStatus.LastFault!.FaultCode);

        var restartResult = await host.StartAsync(
            CreateStartRequest("fake", RuntimeEndpointProfiles.LocalDevelopment),
            CancellationToken.None);
        var restartedStatus = host.GetStatus();

        Assert.True(restartedStatus.IsRunning);
        Assert.Equal(restartResult.EndpointUrl, restartedStatus.EndpointUrl);
        Assert.Null(restartedStatus.LastFault);
    }

    [Fact]
    public async Task RuntimeHostService_ApplyFailure_LeavesInactiveFaultedState()
    {
        var factory = new ConfigurableFakeRuntimeAdapterFactory();
        factory.Adapter.ThrowOnApplyCallNumber = 1;
        var host = new RuntimeHostService([factory]);

        _ = await host.StartAsync(
            CreateStartRequest("fake", RuntimeEndpointProfiles.LocalDevelopment),
            CancellationToken.None);

        var exception = await Assert.ThrowsAsync<RuntimeHostFaultException>(() =>
            host.ApplyUpdatesAsync(
                [
                    new RuntimeValueUpdate(SourceSignal, 9.0, 1),
                ],
                CancellationToken.None).AsTask());

        var status = host.GetStatus();

        Assert.Equal(RuntimeHostFaultCodes.RuntimeApplyFailed, exception.FaultInfo.FaultCode);
        Assert.False(status.IsRunning);
        Assert.NotNull(status.LastFault);
        Assert.Equal(RuntimeHostFaultCodes.RuntimeApplyFailed, status.LastFault!.FaultCode);
        Assert.False(factory.Adapter.Started);
    }

    [Fact]
    public async Task RuntimeHostService_StopFailure_LeavesInactiveFaultedState()
    {
        var factory = new ConfigurableFakeRuntimeAdapterFactory();
        factory.Adapter.ThrowOnStopCallNumber = 1;
        var host = new RuntimeHostService([factory]);

        _ = await host.StartAsync(
            CreateStartRequest("fake", RuntimeEndpointProfiles.LocalDevelopment),
            CancellationToken.None);

        var exception = await Assert.ThrowsAsync<RuntimeHostFaultException>(() =>
            host.StopAsync(CancellationToken.None).AsTask());

        var status = host.GetStatus();

        Assert.Equal(RuntimeHostFaultCodes.RuntimeStopFailed, exception.FaultInfo.FaultCode);
        Assert.False(status.IsRunning);
        Assert.NotNull(status.LastFault);
        Assert.Equal(RuntimeHostFaultCodes.RuntimeStopFailed, status.LastFault!.FaultCode);
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

    private static RuntimeStartRequest CreateStartRequest(
        string adapterKey,
        RuntimeEndpointProfile endpointProfile)
    {
        return new RuntimeStartRequest(
            adapterKey,
            CreateCompiledPlan(SourceSignal),
            RuntimeEndpointSettings.Loopback(0),
            endpointProfile);
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
}
