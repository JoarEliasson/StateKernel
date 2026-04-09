using StateKernel.ControlApi.Contracts.Run;
using StateKernel.RuntimeHost.Execution;
using StateKernel.RuntimeHost.Hosting;

namespace StateKernel.ControlApi.Run;

/// <summary>
/// Orchestrates the bounded run control API flow through the existing selection, composition,
/// runtime start, and execution seams.
/// </summary>
public sealed class RunControlService
{
    private readonly ISimulationRunDefinitionCatalog runDefinitionCatalog;
    private readonly SimulationExecutionOrchestrator executionOrchestrator;
    private readonly RuntimeHostService runtimeHostService;

    /// <summary>
    /// Initializes a new instance of the <see cref="RunControlService" /> type.
    /// </summary>
    /// <param name="runDefinitionCatalog">The executable run-definition catalog used for resolution.</param>
    /// <param name="executionOrchestrator">The execution orchestrator used for run lifecycle ownership.</param>
    /// <param name="runtimeHostService">The runtime host service used for runtime lifecycle status checks.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when any supplied value is null.
    /// </exception>
    public RunControlService(
        ISimulationRunDefinitionCatalog runDefinitionCatalog,
        SimulationExecutionOrchestrator executionOrchestrator,
        RuntimeHostService runtimeHostService)
    {
        ArgumentNullException.ThrowIfNull(runDefinitionCatalog);
        ArgumentNullException.ThrowIfNull(executionOrchestrator);
        ArgumentNullException.ThrowIfNull(runtimeHostService);

        this.runDefinitionCatalog = runDefinitionCatalog;
        this.executionOrchestrator = executionOrchestrator;
        this.runtimeHostService = runtimeHostService;
    }

    /// <summary>
    /// Gets the canonical current run status.
    /// </summary>
    /// <returns>The canonical run status response.</returns>
    public RunStatusResponse GetStatus()
    {
        return ToResponse(executionOrchestrator.GetStatus());
    }

    /// <summary>
    /// Starts a simulation run through the existing runtime startup and execution seams.
    /// </summary>
    /// <param name="request">The bounded run start request.</param>
    /// <param name="cancellationToken">The token that cancels the start operation.</param>
    /// <returns>The canonical run status after startup.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="request" /> is null.
    /// </exception>
    /// <exception cref="RunControlConflictException">
    /// Thrown when a run is already active or when the runtime host is already active.
    /// </exception>
    public async ValueTask<RunStatusResponse> StartAsync(
        StartRunRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (executionOrchestrator.GetStatus().IsActive)
        {
            throw new RunControlConflictException(
                "The simulation run is already active.");
        }

        if (runtimeHostService.GetStatus().IsRunning)
        {
            throw new RunControlConflictException(
                "The runtime is already active and must be inactive before a run can start.");
        }

        var executableDefinition = runDefinitionCatalog.GetRequired(
            SimulationRunDefinitionId.From(request.RunDefinitionId!));
        var exposureChoices = Runtime.RuntimeStartRequestMapper.MapExposureChoices(
            request.ExposureChoices,
            nameof(request.ExposureChoices),
            true);
        var duplicateSelectedSignalId = exposureChoices
            .GroupBy(static choice => choice.SourceSignalId)
            .FirstOrDefault(static group => group.Count() > 1)?
            .Key;

        if (duplicateSelectedSignalId is not null)
        {
            throw new ArgumentException(
                $"Run start requests must select unique simulation signals. Duplicate signal id: '{duplicateSelectedSignalId}'.",
                nameof(request));
        }

        var allowedSignalIds = executableDefinition.ExposableSignals
            .ToHashSet();
        var disallowedSignalId = exposureChoices
            .Select(static choice => choice.SourceSignalId)
            .FirstOrDefault(signalId => !allowedSignalIds.Contains(signalId));

        if (disallowedSignalId is not null)
        {
            throw new ArgumentException(
                $"Run start requests can expose only signals approved by the selected run definition. Unknown signal id: '{disallowedSignalId}'.",
                nameof(request));
        }

        var runtimeStartRequest = Runtime.RuntimeStartRequestMapper.BuildRuntimeStartRequest(
            request.AdapterKey!,
            request.ProfileId!,
            request.EndpointHost!,
            request.EndpointPort,
            request.NodeIdPrefix,
            exposureChoices);
        var startRequest = new SimulationRunStartRequest(
            executableDefinition,
            runtimeStartRequest,
            executableDefinition.DefaultExecutionSettings);
        var startResult = await executionOrchestrator.StartAsync(startRequest, cancellationToken);

        return ToResponse(startResult.Status);
    }

    /// <summary>
    /// Stops the active simulation run and returns the canonical run status after stop.
    /// </summary>
    /// <param name="cancellationToken">The token that cancels the stop operation.</param>
    /// <returns>The canonical run status response after stop.</returns>
    /// <exception cref="RunControlConflictException">
    /// Thrown when the run is not currently active.
    /// </exception>
    public async ValueTask<RunStatusResponse> StopAsync(CancellationToken cancellationToken)
    {
        if (!executionOrchestrator.GetStatus().IsActive)
        {
            throw new RunControlConflictException(
                "The simulation run is not currently active.");
        }

        return ToResponse(await executionOrchestrator.StopAsync(cancellationToken));
    }

    private static RunStatusResponse ToResponse(SimulationRunStatus status)
    {
        ArgumentNullException.ThrowIfNull(status);

        return new RunStatusResponse(
            status.IsActive,
            status.ActiveRunId?.Value,
            status.ActiveRunDefinitionId?.Value,
            status.RuntimeAdapterKey,
            status.EndpointUrl,
            status.ActiveProfileId?.Value,
            status.ExposedNodeCount,
            status.LastCompletedTick,
            status.LastFault?.FaultCode,
            status.LastFault?.Message,
            status.LastFault?.OccurredAtUtc);
    }
}
