using StateKernel.ControlApi.Contracts.Runtime;
using StateKernel.Runtime.Abstractions;
using StateKernel.RuntimeHost.Hosting;

namespace StateKernel.ControlApi.Runtime;

/// <summary>
/// Orchestrates the bounded runtime control API flow through the existing selection, composition,
/// and runtime host seams.
/// </summary>
public sealed class RuntimeControlService
{
    private readonly RuntimeHostService runtimeHostService;

    /// <summary>
    /// Initializes a new instance of the <see cref="RuntimeControlService" /> type.
    /// </summary>
    /// <param name="runtimeHostService">The runtime host service used for lifecycle orchestration.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="runtimeHostService" /> is null.
    /// </exception>
    public RuntimeControlService(RuntimeHostService runtimeHostService)
    {
        ArgumentNullException.ThrowIfNull(runtimeHostService);
        this.runtimeHostService = runtimeHostService;
    }

    /// <summary>
    /// Gets the canonical current runtime status.
    /// </summary>
    /// <returns>The canonical runtime status response.</returns>
    public RuntimeStatusResponse GetStatus()
    {
        return ToResponse(runtimeHostService.GetStatus());
    }

    /// <summary>
    /// Composes and starts the runtime host from the supplied bounded control API request.
    /// </summary>
    /// <param name="request">The bounded runtime start request.</param>
    /// <param name="cancellationToken">The token that cancels the start operation.</param>
    /// <returns>The canonical runtime status response after startup.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="request" /> is null.
    /// </exception>
    /// <exception cref="RuntimeControlConflictException">
    /// Thrown when the runtime is already active.
    /// </exception>
    public async ValueTask<RuntimeStatusResponse> StartAsync(
        StartRuntimeRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (runtimeHostService.GetStatus().IsRunning)
        {
            throw new RuntimeControlConflictException(
                "The runtime is already active.");
        }

        var exposureChoices = RuntimeStartRequestMapper.MapExposureChoices(
            request.ExposureChoices,
            nameof(request.ExposureChoices),
            false);
        var startRequest = RuntimeStartRequestMapper.BuildRuntimeStartRequest(
            request.AdapterKey!,
            request.ProfileId!,
            request.EndpointHost!,
            request.EndpointPort,
            request.NodeIdPrefix,
            exposureChoices);

        _ = await runtimeHostService.StartAsync(startRequest, cancellationToken);

        return ToResponse(runtimeHostService.GetStatus());
    }

    /// <summary>
    /// Stops the active runtime host and returns the canonical runtime status after stop.
    /// </summary>
    /// <param name="cancellationToken">The token that cancels the stop operation.</param>
    /// <returns>The canonical runtime status response after stop.</returns>
    /// <exception cref="RuntimeControlConflictException">
    /// Thrown when the runtime is not currently active.
    /// </exception>
    public async ValueTask<RuntimeStatusResponse> StopAsync(CancellationToken cancellationToken)
    {
        if (!runtimeHostService.GetStatus().IsRunning)
        {
            throw new RuntimeControlConflictException(
                "The runtime is not currently active.");
        }

        _ = await runtimeHostService.StopAsync(cancellationToken);

        return ToResponse(runtimeHostService.GetStatus());
    }

    private static RuntimeStatusResponse ToResponse(RuntimeHostStatus status)
    {
        ArgumentNullException.ThrowIfNull(status);

        return new RuntimeStatusResponse(
            status.IsRunning,
            status.ActiveAdapterKey,
            status.EndpointUrl,
            status.ActiveProfileId?.Value,
            status.ExposedNodeCount,
            status.LastFault?.FaultCode,
            status.LastFault?.Message,
            status.LastFault?.OccurredAtUtc);
    }
}
