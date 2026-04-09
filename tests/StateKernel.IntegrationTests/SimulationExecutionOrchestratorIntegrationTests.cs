using StateKernel.Runtime.Abstractions;
using StateKernel.Runtime.Abstractions.Composition;
using StateKernel.Runtime.Abstractions.Selection;
using StateKernel.RuntimeHost.Execution;
using StateKernel.RuntimeHost.Hosting;
using StateKernel.Simulation.Clock;
using StateKernel.Simulation.Scheduling;
using StateKernel.Simulation.Signals;

namespace StateKernel.IntegrationTests;

public sealed class SimulationExecutionOrchestratorIntegrationTests
{
    private static readonly SimulationSignalId SourceSignal = SimulationSignalId.From("Source");

    [Fact]
    public void SimulationRunIdentifiers_ValidateCanonicalValues()
    {
        Assert.ThrowsAny<ArgumentException>(() => SimulationRunId.From(" "));
        Assert.ThrowsAny<ArgumentException>(() => SimulationRunDefinitionId.From(" "));

        var runId = SimulationRunId.From(" run-1 ");
        var definitionId = SimulationRunDefinitionId.From(" baseline-constant-source ");

        Assert.Equal("run-1", runId.Value);
        Assert.Equal("baseline-constant-source", definitionId.Value);
        Assert.NotEqual(SimulationRunId.CreateNew(), SimulationRunId.CreateNew());
    }

    [Fact]
    public void SimulationExecutionSettings_ValidateManualAndContinuousModes()
    {
        Assert.Equal(SimulationExecutionMode.Manual, SimulationExecutionSettings.Manual.Mode);
        Assert.Null(SimulationExecutionSettings.Manual.LoopDelay);
        Assert.Equal(
            TimeSpan.FromMilliseconds(25),
            SimulationExecutionSettings.CreateContinuous(TimeSpan.FromMilliseconds(25)).LoopDelay);

        Assert.Throws<ArgumentException>(() =>
            new SimulationExecutionSettings(
                SimulationExecutionMode.Manual,
                TimeSpan.FromMilliseconds(10)));
        Assert.Throws<ArgumentException>(() =>
            SimulationExecutionSettings.CreateContinuous(TimeSpan.Zero));
    }

    [Fact]
    public void SimulationRunStatus_EnforcesActiveInactiveAndFaultContracts()
    {
        var faultInfo = new SimulationRunFaultInfo(
            SimulationRunFaultCodes.ContinuousStepFailed,
            "Faulted run.",
            DateTimeOffset.UtcNow,
            3);

        Assert.False(SimulationRunStatus.Inactive.IsActive);
        Assert.False(SimulationRunStatus.Inactive.RuntimeAttached);
        Assert.Null(SimulationRunStatus.Inactive.LastFault);

        var faultedStatus = SimulationRunStatus.Faulted(faultInfo);

        Assert.False(faultedStatus.IsActive);
        Assert.False(faultedStatus.RuntimeAttached);
        Assert.Equal(faultInfo, faultedStatus.LastFault);

        var activeManualStatus = SimulationRunStatus.Active(
            SimulationRunId.CreateNew(),
            SimulationRunDefinitionId.From("baseline"),
            "fake",
            RuntimeEndpointProfiles.LocalDevelopment.Id,
            "opc.tcp://127.0.0.1:40123/fake",
            1,
            null);

        Assert.True(activeManualStatus.IsActive);
        Assert.Null(activeManualStatus.LastCompletedTick);
        Assert.Null(activeManualStatus.LastFault);

        Assert.Null(activeManualStatus.LastFault);
    }

    [Fact]
    public void SimulationRunDefinitionCatalog_ResolvesDefinitionsDeterministicallyWithoutFallback()
    {
        var catalog = SimulationRunDefinitionCatalog.CreateDefault();

        Assert.Collection(
            catalog.All,
            definition => Assert.Equal("baseline-constant-source", definition.Id.Value));
        Assert.Throws<InvalidOperationException>(() =>
            catalog.GetRequired(SimulationRunDefinitionId.From("unknown-definition")));
    }

    [Fact]
    public void CommittedSnapshotVisibilityReader_UsesTheNextTickBoundary()
    {
        var signalStore = new SimulationSignalValueStore();
        var firstTick = new SimulationTick(1, TimeSpan.FromMilliseconds(10));
        var frame = new SchedulerExecutionFrame(firstTick, ["source"]);

        _ = signalStore.GetCommittedSnapshotForTick(firstTick);
        signalStore.RecordProducedValue(
            new SimulationSignalValue(SourceSignal, firstTick, new StateKernel.Simulation.Behaviors.BehaviorSample(5.0)));

        var visibleSnapshot = CommittedSnapshotVisibilityReader.ReadVisibleSnapshotAfterCompletedFrame(
            frame,
            new SimulationClockSettings(TimeSpan.FromMilliseconds(10), 8),
            signalStore);

        Assert.Equal(SourceSignal, visibleSnapshot.GetRequiredValue(SourceSignal).SignalId);
    }

    [Fact]
    public async Task SimulationExecutionOrchestrator_ManualRunsRequireExplicitStepsAndAdvanceDeterministically()
    {
        var factory = new ConfigurableFakeRuntimeAdapterFactory();
        var host = new RuntimeHostService([factory]);
        using var orchestrator = new SimulationExecutionOrchestrator(host);
        var definition = GetBaselineDefinition();
        var startRequest = CreateRunStartRequest(
            definition,
            "fake",
            SimulationExecutionSettings.Manual);

        var startResult = await orchestrator.StartAsync(startRequest, CancellationToken.None);
        await Task.Delay(75);

        Assert.True(startResult.Status.IsActive);
        Assert.Null(startResult.Status.LastCompletedTick);
        Assert.Null(startResult.Status.LastFault);
        Assert.Empty(factory.Adapter.AppliedBatches);

        var firstStep = await orchestrator.StepOnceAsync(CancellationToken.None);
        var secondStep = await orchestrator.StepOnceAsync(CancellationToken.None);

        Assert.Equal(1, firstStep.Frame.Tick.SequenceNumber);
        Assert.Equal(1, firstStep.PublishedUpdateCount);
        Assert.Equal(1, firstStep.Status.LastCompletedTick);
        Assert.Equal(2, secondStep.Frame.Tick.SequenceNumber);
        Assert.Equal(2, secondStep.Status.LastCompletedTick);
        Assert.Equal(2, factory.Adapter.AppliedBatches.Length);
        Assert.Equal(1, factory.Adapter.AppliedBatches[0][0].SourceTickSequenceNumber);
        Assert.Equal(2, factory.Adapter.AppliedBatches[1][0].SourceTickSequenceNumber);

        var stopStatus = await orchestrator.StopAsync(CancellationToken.None);

        Assert.False(stopStatus.IsActive);
        Assert.Null(stopStatus.LastFault);
        Assert.False(host.GetStatus().IsRunning);
    }

    [Fact]
    public async Task SimulationExecutionOrchestrator_RejectsInvalidLifecycleAndExplicitStepUsageWithoutRetainingFaults()
    {
        var factory = new ConfigurableFakeRuntimeAdapterFactory();
        var host = new RuntimeHostService([factory]);
        using var orchestrator = new SimulationExecutionOrchestrator(host);
        var definition = GetBaselineDefinition();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            orchestrator.StopAsync(CancellationToken.None).AsTask());
        Assert.Null(orchestrator.GetStatus().LastFault);

        _ = await orchestrator.StartAsync(
            CreateRunStartRequest(definition, "fake", SimulationExecutionSettings.Manual),
            CancellationToken.None);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            orchestrator.StartAsync(
                CreateRunStartRequest(definition, "fake", SimulationExecutionSettings.Manual),
                CancellationToken.None).AsTask());
        Assert.Null(orchestrator.GetStatus().LastFault);

        _ = await orchestrator.StopAsync(CancellationToken.None);

        _ = await host.StartAsync(
            CreateRuntimeStartRequest("fake", definition.ExposableSignals),
            CancellationToken.None);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            orchestrator.StartAsync(
                CreateRunStartRequest(definition, "fake", SimulationExecutionSettings.Manual),
                CancellationToken.None).AsTask());
        Assert.Null(orchestrator.GetStatus().LastFault);

        _ = await host.StopAsync(CancellationToken.None);

        var continuousStart = await orchestrator.StartAsync(
            CreateRunStartRequest(
                definition,
                "fake",
                SimulationExecutionSettings.CreateContinuous(TimeSpan.FromMilliseconds(25))),
            CancellationToken.None);

        Assert.True(continuousStart.Status.LastCompletedTick >= 1);
        Assert.Null(continuousStart.Status.LastFault);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            orchestrator.StepOnceAsync(CancellationToken.None).AsTask());
        Assert.True(orchestrator.GetStatus().IsActive);
        Assert.Null(orchestrator.GetStatus().LastFault);

        _ = await orchestrator.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task SimulationExecutionOrchestrator_SerializesConcurrentManualSteps()
    {
        var factory = new ConfigurableFakeRuntimeAdapterFactory();
        factory.Adapter.ApplyDelay = TimeSpan.FromMilliseconds(75);
        var host = new RuntimeHostService([factory]);
        using var orchestrator = new SimulationExecutionOrchestrator(host);
        var definition = GetBaselineDefinition();

        _ = await orchestrator.StartAsync(
            CreateRunStartRequest(definition, "fake", SimulationExecutionSettings.Manual),
            CancellationToken.None);

        var firstStepTask = orchestrator.StepOnceAsync(CancellationToken.None).AsTask();
        var secondStepTask = orchestrator.StepOnceAsync(CancellationToken.None).AsTask();

        await Task.WhenAll(firstStepTask, secondStepTask);

        Assert.Equal(1, factory.Adapter.MaxConcurrentApplyCalls);
        Assert.Equal(2, orchestrator.GetStatus().LastCompletedTick);

        _ = await orchestrator.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task SimulationExecutionOrchestrator_StopDuringAnInFlightStepEndsInACleanInactiveState()
    {
        var factory = new ConfigurableFakeRuntimeAdapterFactory();
        factory.Adapter.ApplyDelay = TimeSpan.FromMilliseconds(125);
        var host = new RuntimeHostService([factory]);
        using var orchestrator = new SimulationExecutionOrchestrator(host);

        _ = await orchestrator.StartAsync(
            CreateRunStartRequest(GetBaselineDefinition(), "fake", SimulationExecutionSettings.Manual),
            CancellationToken.None);

        var stepTask = orchestrator.StepOnceAsync(CancellationToken.None).AsTask();
        await Task.Delay(25);
        var stopStatus = await orchestrator.StopAsync(CancellationToken.None);
        var stepResult = await stepTask;

        Assert.Equal(1, stepResult.Status.LastCompletedTick);
        Assert.False(stopStatus.IsActive);
        Assert.False(stopStatus.RuntimeAttached);
        Assert.Null(stopStatus.LastFault);
        Assert.False(host.GetStatus().IsRunning);
        Assert.Null(host.GetStatus().LastFault);
    }

    [Fact]
    public async Task SimulationExecutionOrchestrator_ContinuousRunsStepImmediatelyBeforeStartReturns()
    {
        var factory = new ConfigurableFakeRuntimeAdapterFactory();
        var host = new RuntimeHostService([factory]);
        using var orchestrator = new SimulationExecutionOrchestrator(host);
        var definition = GetBaselineDefinition();

        var startResult = await orchestrator.StartAsync(
            CreateRunStartRequest(
                definition,
                "fake",
                SimulationExecutionSettings.CreateContinuous(TimeSpan.FromMilliseconds(50))),
            CancellationToken.None);

        Assert.True(startResult.Status.IsActive);
        Assert.True(startResult.Status.RuntimeAttached);
        Assert.True(startResult.Status.LastCompletedTick >= 1);
        Assert.Null(startResult.Status.LastFault);
        Assert.True(factory.Adapter.AppliedBatches.Length >= 1);

        _ = await orchestrator.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task SimulationExecutionOrchestrator_RuntimeAttachFailure_LeavesInactiveFaultedStates()
    {
        var factory = new ConfigurableFakeRuntimeAdapterFactory();
        factory.Adapter.ThrowOnStartCallNumber = 1;
        var host = new RuntimeHostService([factory]);
        using var orchestrator = new SimulationExecutionOrchestrator(host);

        var exception = await Assert.ThrowsAsync<SimulationRunFaultException>(() =>
            orchestrator.StartAsync(
                CreateRunStartRequest(
                    GetBaselineDefinition(),
                    "fake",
                    SimulationExecutionSettings.Manual),
                CancellationToken.None).AsTask());

        var runStatus = orchestrator.GetStatus();
        var runtimeStatus = host.GetStatus();

        Assert.Equal(SimulationRunFaultCodes.RuntimeAttachFailed, exception.FaultInfo.FaultCode);
        Assert.False(runStatus.IsActive);
        Assert.NotNull(runStatus.LastFault);
        Assert.Equal(SimulationRunFaultCodes.RuntimeAttachFailed, runStatus.LastFault!.FaultCode);
        Assert.False(runtimeStatus.IsRunning);
        Assert.NotNull(runtimeStatus.LastFault);
        Assert.Equal(RuntimeHostFaultCodes.RuntimeStartFailed, runtimeStatus.LastFault!.FaultCode);
    }

    [Fact]
    public async Task SimulationExecutionOrchestrator_InitialContinuousStepFailure_LeavesInactiveFaultedStates()
    {
        var factory = new ConfigurableFakeRuntimeAdapterFactory();
        factory.Adapter.ThrowOnApplyCallNumber = 1;
        var host = new RuntimeHostService([factory]);
        using var orchestrator = new SimulationExecutionOrchestrator(host);

        var exception = await Assert.ThrowsAsync<SimulationRunFaultException>(() =>
            orchestrator.StartAsync(
                CreateRunStartRequest(
                    GetBaselineDefinition(),
                    "fake",
                    SimulationExecutionSettings.CreateContinuous(TimeSpan.FromMilliseconds(25))),
                CancellationToken.None).AsTask());

        var runStatus = orchestrator.GetStatus();
        var runtimeStatus = host.GetStatus();

        Assert.Equal(SimulationRunFaultCodes.InitialStepFailed, exception.FaultInfo.FaultCode);
        Assert.False(runStatus.IsActive);
        Assert.NotNull(runStatus.LastFault);
        Assert.Equal(SimulationRunFaultCodes.InitialStepFailed, runStatus.LastFault!.FaultCode);
        Assert.False(runtimeStatus.IsRunning);
        Assert.NotNull(runtimeStatus.LastFault);
        Assert.Equal(RuntimeHostFaultCodes.RuntimeApplyFailed, runtimeStatus.LastFault!.FaultCode);
    }

    [Fact]
    public async Task SimulationExecutionOrchestrator_ExplicitManualStepFailure_LeavesInactiveFaultedStates()
    {
        var factory = new ConfigurableFakeRuntimeAdapterFactory();
        factory.Adapter.ThrowOnApplyCallNumber = 1;
        var host = new RuntimeHostService([factory]);
        using var orchestrator = new SimulationExecutionOrchestrator(host);

        _ = await orchestrator.StartAsync(
            CreateRunStartRequest(
                GetBaselineDefinition(),
                "fake",
                SimulationExecutionSettings.Manual),
            CancellationToken.None);

        var exception = await Assert.ThrowsAsync<SimulationRunFaultException>(() =>
            orchestrator.StepOnceAsync(CancellationToken.None).AsTask());

        var runStatus = orchestrator.GetStatus();
        var runtimeStatus = host.GetStatus();

        Assert.Equal(SimulationRunFaultCodes.StepFailed, exception.FaultInfo.FaultCode);
        Assert.False(runStatus.IsActive);
        Assert.NotNull(runStatus.LastFault);
        Assert.Equal(SimulationRunFaultCodes.StepFailed, runStatus.LastFault!.FaultCode);
        Assert.False(runtimeStatus.IsRunning);
        Assert.NotNull(runtimeStatus.LastFault);
        Assert.Equal(RuntimeHostFaultCodes.RuntimeApplyFailed, runtimeStatus.LastFault!.FaultCode);
    }

    [Fact]
    public async Task SimulationExecutionOrchestrator_ContinuousLoopFaultsStopTheRunAndRuntime()
    {
        var factory = new ConfigurableFakeRuntimeAdapterFactory();
        factory.Adapter.ThrowOnApplyCallNumber = 2;
        var host = new RuntimeHostService([factory]);
        using var orchestrator = new SimulationExecutionOrchestrator(host);

        var startResult = await orchestrator.StartAsync(
            CreateRunStartRequest(
                GetBaselineDefinition(),
                "fake",
                SimulationExecutionSettings.CreateContinuous(TimeSpan.FromMilliseconds(20))),
            CancellationToken.None);

        Assert.True(startResult.Status.IsActive);

        await EventuallyAsync(
            () => !orchestrator.GetStatus().IsActive && !host.GetStatus().IsRunning,
            TimeSpan.FromSeconds(3));

        var runStatus = orchestrator.GetStatus();
        var runtimeStatus = host.GetStatus();

        Assert.False(runStatus.IsActive);
        Assert.NotNull(runStatus.LastFault);
        Assert.Equal(SimulationRunFaultCodes.ContinuousStepFailed, runStatus.LastFault!.FaultCode);
        Assert.False(runStatus.RuntimeAttached);
        Assert.False(runtimeStatus.IsRunning);
        Assert.NotNull(runtimeStatus.LastFault);
        Assert.Equal(RuntimeHostFaultCodes.RuntimeApplyFailed, runtimeStatus.LastFault!.FaultCode);
    }

    [Fact]
    public async Task SimulationExecutionOrchestrator_StopFailure_LeavesInactiveFaultedStates()
    {
        var factory = new ConfigurableFakeRuntimeAdapterFactory();
        factory.Adapter.ThrowOnStopCallNumber = 1;
        var host = new RuntimeHostService([factory]);
        using var orchestrator = new SimulationExecutionOrchestrator(host);

        _ = await orchestrator.StartAsync(
            CreateRunStartRequest(
                GetBaselineDefinition(),
                "fake",
                SimulationExecutionSettings.Manual),
            CancellationToken.None);

        var exception = await Assert.ThrowsAsync<SimulationRunFaultException>(() =>
            orchestrator.StopAsync(CancellationToken.None).AsTask());

        var runStatus = orchestrator.GetStatus();
        var runtimeStatus = host.GetStatus();

        Assert.Equal(SimulationRunFaultCodes.RunStopFailed, exception.FaultInfo.FaultCode);
        Assert.False(runStatus.IsActive);
        Assert.NotNull(runStatus.LastFault);
        Assert.Equal(SimulationRunFaultCodes.RunStopFailed, runStatus.LastFault!.FaultCode);
        Assert.False(runtimeStatus.IsRunning);
        Assert.NotNull(runtimeStatus.LastFault);
        Assert.Equal(RuntimeHostFaultCodes.RuntimeStopFailed, runtimeStatus.LastFault!.FaultCode);
    }

    [Fact]
    public async Task SimulationExecutionOrchestrator_SuccessfulRestartClearsRetainedRunFaultState()
    {
        var factory = new ConfigurableFakeRuntimeAdapterFactory();
        factory.Adapter.ThrowOnApplyCallNumber = 1;
        var host = new RuntimeHostService([factory]);
        using var orchestrator = new SimulationExecutionOrchestrator(host);
        var definition = GetBaselineDefinition();

        _ = await orchestrator.StartAsync(
            CreateRunStartRequest(definition, "fake", SimulationExecutionSettings.Manual),
            CancellationToken.None);
        _ = await Assert.ThrowsAsync<SimulationRunFaultException>(() =>
            orchestrator.StepOnceAsync(CancellationToken.None).AsTask());

        factory.Adapter.ThrowOnApplyCallNumber = null;

        var restartResult = await orchestrator.StartAsync(
            CreateRunStartRequest(definition, "fake", SimulationExecutionSettings.Manual),
            CancellationToken.None);

        Assert.True(restartResult.Status.IsActive);
        Assert.Null(restartResult.Status.LastFault);
        Assert.Null(orchestrator.GetStatus().LastFault);

        _ = await orchestrator.StopAsync(CancellationToken.None);
    }

    private static ISimulationExecutableRunDefinition GetBaselineDefinition()
    {
        return SimulationRunDefinitionCatalog.CreateDefault()
            .GetRequired(SimulationRunDefinitionId.From("baseline-constant-source"));
    }

    private static SimulationRunStartRequest CreateRunStartRequest(
        ISimulationExecutableRunDefinition executableDefinition,
        string adapterKey,
        SimulationExecutionSettings executionSettings)
    {
        return new SimulationRunStartRequest(
            executableDefinition,
            CreateRuntimeStartRequest(adapterKey, executableDefinition.ExposableSignals),
            executionSettings);
    }

    private static RuntimeStartRequest CreateRuntimeStartRequest(
        string adapterKey,
        IReadOnlyList<SimulationSignalId> signalIds)
    {
        var exposureChoices = signalIds
            .Select(static signalId => new SimulationSignalExposureChoice(signalId))
            .ToArray();
        var selectionResult = RuntimeSignalSelectionService.CreateSelections(
            new RuntimeSignalSelectionRequest(exposureChoices));
        var compositionResult = RuntimeCompositionService.Compose(
            new RuntimeCompositionRequest(
                adapterKey,
                selectionResult.SignalSelections,
                RuntimeCompositionDefaults.Baseline));

        return new RuntimeStartRequest(
            adapterKey,
            compositionResult.CompiledRuntimePlan,
            RuntimeEndpointSettings.Loopback(0),
            RuntimeEndpointProfiles.LocalDevelopment);
    }

    private static async Task EventuallyAsync(
        Func<bool> condition,
        TimeSpan timeout)
    {
        var deadline = DateTimeOffset.UtcNow + timeout;

        while (DateTimeOffset.UtcNow < deadline)
        {
            if (condition())
            {
                return;
            }

            await Task.Delay(50);
        }

        Assert.True(condition());
    }
}
