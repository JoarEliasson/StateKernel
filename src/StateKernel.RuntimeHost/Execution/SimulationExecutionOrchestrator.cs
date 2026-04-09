using StateKernel.Runtime.Abstractions;
using StateKernel.RuntimeHost.Hosting;

namespace StateKernel.RuntimeHost.Execution;

/// <summary>
/// Owns one active deterministic simulation run and its downstream runtime publication flow.
/// </summary>
public sealed class SimulationExecutionOrchestrator : IDisposable
{
    private readonly RuntimeHostService runtimeHostService;
    // Serializes start/stop/fault transitions so the active run and attached runtime change state atomically.
    private readonly SemaphoreSlim lifecycleGate = new(1, 1);
    // Serializes all deterministic stepping so timer-driven iterations and explicit manual steps cannot overlap.
    private readonly SemaphoreSlim stepGate = new(1, 1);
    private ActiveSimulationRun? activeRun;
    private ActiveSimulationRun? stoppingRun;
    private SimulationRunStatus status = SimulationRunStatus.Inactive;
    private bool disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="SimulationExecutionOrchestrator" /> type.
    /// </summary>
    /// <param name="runtimeHostService">The runtime host service used for runtime lifecycle ownership.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="runtimeHostService" /> is null.
    /// </exception>
    public SimulationExecutionOrchestrator(RuntimeHostService runtimeHostService)
    {
        ArgumentNullException.ThrowIfNull(runtimeHostService);
        this.runtimeHostService = runtimeHostService;
    }

    /// <summary>
    /// Gets the canonical run status snapshot.
    /// </summary>
    /// <returns>The canonical run status snapshot.</returns>
    public SimulationRunStatus GetStatus()
    {
        return status;
    }

    /// <summary>
    /// Disposes the orchestrator and its owned synchronization primitives.
    /// </summary>
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        activeRun?.LifetimeCancellationSource.Cancel();
        activeRun?.LifetimeCancellationSource.Dispose();

        if (!ReferenceEquals(stoppingRun, activeRun))
        {
            stoppingRun?.LifetimeCancellationSource.Cancel();
            stoppingRun?.LifetimeCancellationSource.Dispose();
        }

        lifecycleGate.Dispose();
        stepGate.Dispose();
    }

    /// <summary>
    /// Starts a new simulation run and attaches the runtime host through the supplied runtime start request.
    /// </summary>
    /// <remarks>
    /// Manual runs start without a background loop and advance only through <see cref="StepOnceAsync" />.
    /// Continuous runs perform one immediate deterministic step before start returns, then continue on the
    /// orchestrator-owned timer loop. All stepping paths share the same serialized step gate.
    /// </remarks>
    /// <param name="request">The validated run start request.</param>
    /// <param name="cancellationToken">The token that cancels the start operation.</param>
    /// <returns>The run start result.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="request" /> is null.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when a run is already active or when the runtime host is already active.
    /// </exception>
    /// <exception cref="SimulationRunFaultException">
    /// Thrown when runtime attachment or the initial deterministic step fails internally.
    /// </exception>
    public async ValueTask<SimulationRunStartResult> StartAsync(
        SimulationRunStartRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        await lifecycleGate.WaitAsync(cancellationToken);

        try
        {
            if (activeRun is not null || stoppingRun is not null)
            {
                throw new InvalidOperationException(
                    "A simulation run is already active.");
            }

            if (runtimeHostService.GetStatus().IsRunning)
            {
                throw new InvalidOperationException(
                    "The runtime host is already active and cannot be attached by a new simulation run.");
            }

            var run = new ActiveSimulationRun(
                SimulationRunId.CreateNew(),
                request.ExecutableDefinition,
                request.RuntimeStartRequest,
                request.ExecutionSettings,
                request.ExecutableDefinition.CreateExecutionComponents(),
                new CancellationTokenSource());

            try
            {
                _ = await runtimeHostService.StartAsync(
                    request.RuntimeStartRequest,
                    cancellationToken);
            }
            catch (OperationCanceledException)
            {
                run.LifetimeCancellationSource.Dispose();
                throw;
            }
            catch (RuntimeHostFaultException exception)
            {
                var faultInfo = CreateRunFaultInfo(
                    SimulationRunFaultCodes.RuntimeAttachFailed,
                    "The simulation run could not attach to the runtime.",
                    null);
                status = SimulationRunStatus.Faulted(faultInfo);
                run.LifetimeCancellationSource.Dispose();
                throw new SimulationRunFaultException(faultInfo, exception);
            }

            activeRun = run;
            status = CreateActiveStatus(run, null);

            if (request.ExecutionSettings.Mode == SimulationExecutionMode.Continuous)
            {
                try
                {
                    _ = await StepCoreAsync(run, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    await CleanupFailedStartAsync(run, null, cancellationToken);
                    throw;
                }
                catch (Exception exception)
                {
                    var faultInfo = CreateRunFaultInfo(
                        SimulationRunFaultCodes.InitialStepFailed,
                        "The simulation run could not complete its initial deterministic step.",
                        null);
                    await CleanupFailedStartAsync(run, faultInfo, cancellationToken);
                    throw new SimulationRunFaultException(faultInfo, exception);
                }

                run.LoopTask = RunContinuousLoopAsync(run);
            }

            return new SimulationRunStartResult(run.RunId, status);
        }
        finally
        {
            lifecycleGate.Release();
        }
    }

    /// <summary>
    /// Advances the active manual run by one deterministic step and publishes its committed runtime updates.
    /// </summary>
    /// <param name="cancellationToken">The token that cancels waiting for the step gate.</param>
    /// <returns>The step result.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no run is active or when the active run is not manual.
    /// </exception>
    /// <exception cref="SimulationRunFaultException">
    /// Thrown when an explicit deterministic step fails internally and leaves the run inactive/faulted.
    /// </exception>
    public async ValueTask<SimulationRunStepResult> StepOnceAsync(CancellationToken cancellationToken)
    {
        var run = activeRun;

        if (run is null)
        {
            throw new InvalidOperationException(
                "The simulation run is not currently active.");
        }

        if (run.ExecutionSettings.Mode != SimulationExecutionMode.Manual)
        {
            throw new InvalidOperationException(
                "Explicit deterministic stepping is supported only for manual simulation runs.");
        }

        try
        {
            return await StepCoreAsync(run, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception) when (ShouldFaultExplicitStep(run))
        {
            var faultInfo = CreateRunFaultInfo(
                SimulationRunFaultCodes.StepFailed,
                "The simulation run failed while advancing and publishing an explicit deterministic step.",
                status.LastCompletedTick);
            await FailActiveRunAsync(run, faultInfo, CancellationToken.None);
            throw new SimulationRunFaultException(faultInfo, exception);
        }
    }

    /// <summary>
    /// Stops the active simulation run and its attached runtime.
    /// </summary>
    /// <param name="cancellationToken">The token that cancels the stop operation.</param>
    /// <returns>The canonical inactive run status after stop.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no run is currently active.
    /// </exception>
    /// <exception cref="SimulationRunFaultException">
    /// Thrown when stop fails internally after the run has transitioned inactive/faulted.
    /// </exception>
    public async ValueTask<SimulationRunStatus> StopAsync(CancellationToken cancellationToken)
    {
        ActiveSimulationRun run;
        Task? loopTask;

        await lifecycleGate.WaitAsync(cancellationToken);

        try
        {
            run = activeRun ?? throw new InvalidOperationException(
                "The simulation run is not currently active.");
            stoppingRun = run;
            run.LifetimeCancellationSource.Cancel();
            loopTask = run.LoopTask;
        }
        finally
        {
            lifecycleGate.Release();
        }

        if (loopTask is not null)
        {
            await loopTask;
        }

        await stepGate.WaitAsync(cancellationToken);

        try
        {
            await lifecycleGate.WaitAsync(cancellationToken);

            try
            {
                if (!ReferenceEquals(activeRun, run))
                {
                    if (ReferenceEquals(stoppingRun, run))
                    {
                        stoppingRun = null;
                    }

                    return status;
                }

                try
                {
                    if (runtimeHostService.GetStatus().IsRunning)
                    {
                        _ = await runtimeHostService.StopAsync(cancellationToken);
                    }

                    activeRun = null;
                    status = SimulationRunStatus.Inactive;
                }
                catch (RuntimeHostFaultException exception)
                {
                    var faultInfo = CreateRunFaultInfo(
                        SimulationRunFaultCodes.RunStopFailed,
                        "The simulation run failed while stopping.",
                        status.LastCompletedTick);
                    activeRun = null;
                    status = SimulationRunStatus.Faulted(faultInfo);
                    throw new SimulationRunFaultException(faultInfo, exception);
                }
                finally
                {
                    if (ReferenceEquals(stoppingRun, run))
                    {
                        stoppingRun = null;
                    }
                }
            }
            finally
            {
                lifecycleGate.Release();
            }
        }
        finally
        {
            stepGate.Release();
            run.LifetimeCancellationSource.Dispose();
        }

        return status;
    }

    private async Task RunContinuousLoopAsync(ActiveSimulationRun run)
    {
        try
        {
            // The timer is scoped to this loop so cancellation or fault exit deterministically tears it down.
            using var timer = new PeriodicTimer(run.ExecutionSettings.LoopDelay!.Value);

            while (await timer.WaitForNextTickAsync(run.LifetimeCancellationSource.Token))
            {
                try
                {
                    _ = await StepCoreAsync(run, CancellationToken.None);
                }
                catch (OperationCanceledException) when (run.LifetimeCancellationSource.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception) when (ShouldIgnoreLoopStepFailure(run))
                {
                    return;
                }
                catch (Exception)
                {
                    var faultInfo = CreateRunFaultInfo(
                        SimulationRunFaultCodes.ContinuousStepFailed,
                        "The active simulation run failed while advancing and publishing updates.",
                        status.LastCompletedTick);
                    await FailActiveRunAsync(run, faultInfo, CancellationToken.None);
                    return;
                }
            }
        }
        catch (OperationCanceledException) when (run.LifetimeCancellationSource.IsCancellationRequested)
        {
        }
    }

    private async ValueTask<SimulationRunStepResult> StepCoreAsync(
        ActiveSimulationRun run,
        CancellationToken cancellationToken)
    {
        await stepGate.WaitAsync(cancellationToken);

        try
        {
            if (!ReferenceEquals(activeRun, run))
            {
                throw new InvalidOperationException(
                    "The simulation run is not currently active.");
            }

            if (ReferenceEquals(stoppingRun, run) || run.LifetimeCancellationSource.IsCancellationRequested)
            {
                throw new InvalidOperationException(
                    "The simulation run is stopping and cannot advance.");
            }

            var runtimeStatus = runtimeHostService.GetStatus();

            if (!runtimeStatus.IsRunning)
            {
                throw new InvalidOperationException(
                    "The attached runtime host is not currently active.");
            }

            var frame = run.Components.Scheduler.RunNextTick();
            var committedSnapshot = CommittedSnapshotVisibilityReader.ReadVisibleSnapshotAfterCompletedFrame(
                frame,
                run.Components.Scheduler.Context.Clock.Settings,
                run.Components.SignalStore);
            var updates = RuntimeValueUpdateProjector.CreateUpdates(
                run.RuntimeStartRequest.CompiledPlan,
                committedSnapshot);

            await runtimeHostService.ApplyUpdatesAsync(updates, CancellationToken.None);

            status = CreateActiveStatus(run, frame.Tick.SequenceNumber);

            return new SimulationRunStepResult(
                frame,
                status,
                updates.Count);
        }
        finally
        {
            stepGate.Release();
        }
    }

    private async Task CleanupFailedStartAsync(
        ActiveSimulationRun run,
        SimulationRunFaultInfo? faultInfo,
        CancellationToken cancellationToken)
    {
        run.LifetimeCancellationSource.Cancel();

        if (runtimeHostService.GetStatus().IsRunning)
        {
            try
            {
                _ = await runtimeHostService.StopAsync(cancellationToken);
            }
            catch (RuntimeHostFaultException)
            {
            }
        }

        activeRun = null;
        stoppingRun = null;
        status = faultInfo is null
            ? SimulationRunStatus.Inactive
            : SimulationRunStatus.Faulted(faultInfo);
        run.LifetimeCancellationSource.Dispose();
    }

    private async Task FailActiveRunAsync(
        ActiveSimulationRun run,
        SimulationRunFaultInfo faultInfo,
        CancellationToken cancellationToken)
    {
        await lifecycleGate.WaitAsync(cancellationToken);

        try
        {
            if (!ReferenceEquals(activeRun, run))
            {
                return;
            }

            stoppingRun = run;
            run.LifetimeCancellationSource.Cancel();

            if (runtimeHostService.GetStatus().IsRunning)
            {
                try
                {
                    _ = await runtimeHostService.StopAsync(cancellationToken);
                }
                catch (RuntimeHostFaultException)
                {
                }
            }

            activeRun = null;
            stoppingRun = null;
            status = SimulationRunStatus.Faulted(faultInfo);
        }
        finally
        {
            lifecycleGate.Release();
            run.LifetimeCancellationSource.Dispose();
        }
    }

    private SimulationRunStatus CreateActiveStatus(
        ActiveSimulationRun run,
        long? lastCompletedTick)
    {
        var runtimeStatus = runtimeHostService.GetStatus();

        if (!runtimeStatus.IsRunning)
        {
            throw new InvalidOperationException(
                "Active simulation runs require an attached active runtime host.");
        }

        return SimulationRunStatus.Active(
            run.RunId,
            run.ExecutableDefinition.Id,
            run.RuntimeStartRequest.AdapterKey,
            run.RuntimeStartRequest.EndpointProfile.Id,
            runtimeStatus.EndpointUrl!,
            runtimeStatus.ExposedNodeCount!.Value,
            lastCompletedTick);
    }

    private static SimulationRunFaultInfo CreateRunFaultInfo(
        string faultCode,
        string message,
        long? lastCompletedTick)
    {
        return new SimulationRunFaultInfo(
            faultCode,
            message,
            DateTimeOffset.UtcNow,
            lastCompletedTick);
    }

    private bool ShouldFaultExplicitStep(ActiveSimulationRun run)
    {
        return ReferenceEquals(activeRun, run) &&
            !ReferenceEquals(stoppingRun, run) &&
            !run.LifetimeCancellationSource.IsCancellationRequested;
    }

    private bool ShouldIgnoreLoopStepFailure(ActiveSimulationRun run)
    {
        return !ReferenceEquals(activeRun, run) ||
            ReferenceEquals(stoppingRun, run) ||
            run.LifetimeCancellationSource.IsCancellationRequested;
    }

    private sealed class ActiveSimulationRun
    {
        public ActiveSimulationRun(
            SimulationRunId runId,
            ISimulationExecutableRunDefinition executableDefinition,
            RuntimeStartRequest runtimeStartRequest,
            SimulationExecutionSettings executionSettings,
            SimulationRunComponents components,
            CancellationTokenSource lifetimeCancellationSource)
        {
            RunId = runId;
            ExecutableDefinition = executableDefinition;
            RuntimeStartRequest = runtimeStartRequest;
            ExecutionSettings = executionSettings;
            Components = components;
            LifetimeCancellationSource = lifetimeCancellationSource;
        }

        public SimulationRunId RunId { get; }

        public ISimulationExecutableRunDefinition ExecutableDefinition { get; }

        public RuntimeStartRequest RuntimeStartRequest { get; }

        public SimulationExecutionSettings ExecutionSettings { get; }

        public SimulationRunComponents Components { get; }

        public CancellationTokenSource LifetimeCancellationSource { get; }

        public Task? LoopTask { get; set; }
    }
}
