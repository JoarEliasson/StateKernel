using StateKernel.Runtime.Abstractions;

namespace StateKernel.RuntimeHost.Hosting;

/// <summary>
/// Represents the canonical read model for the runtime host's active adapter state.
/// </summary>
public sealed class RuntimeHostStatus
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RuntimeHostStatus" /> type.
    /// </summary>
    /// <param name="isRunning">Indicates whether a runtime adapter is currently active.</param>
    /// <param name="activeAdapterKey">The active adapter key when the host is running.</param>
    /// <param name="endpointUrl">The externally readable endpoint URL when the host is running.</param>
    /// <param name="activeProfileId">The active endpoint/profile identifier when the host is running.</param>
    /// <param name="exposedNodeCount">The number of exposed nodes when the host is running.</param>
    /// <param name="lastFault">
    /// The retained bounded fault information when the host is inactive because of an internal failure.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown when the supplied values do not match the running state.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="exposedNodeCount" /> is negative.
    /// </exception>
    private RuntimeHostStatus(
        bool isRunning,
        string? activeAdapterKey,
        string? endpointUrl,
        RuntimeEndpointProfileId? activeProfileId,
        int? exposedNodeCount,
        RuntimeHostFaultInfo? lastFault)
    {
        if (exposedNodeCount is < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(exposedNodeCount),
                "Exposed runtime node counts cannot be negative.");
        }

        if (isRunning)
        {
            if (string.IsNullOrWhiteSpace(activeAdapterKey))
            {
                throw new ArgumentException(
                    "Running runtime host status values must include an active adapter key.",
                    nameof(activeAdapterKey));
            }

            if (string.IsNullOrWhiteSpace(endpointUrl))
            {
                throw new ArgumentException(
                    "Running runtime host status values must include an endpoint URL.",
                    nameof(endpointUrl));
            }

            ArgumentNullException.ThrowIfNull(activeProfileId);

            if (exposedNodeCount is null)
            {
                throw new ArgumentException(
                    "Running runtime host status values must include an exposed node count.",
                    nameof(exposedNodeCount));
            }

            if (lastFault is not null)
            {
                throw new ArgumentException(
                    "Running runtime host status values cannot retain fault information.",
                    nameof(lastFault));
            }
        }
        else if (activeAdapterKey is not null ||
            endpointUrl is not null ||
            activeProfileId is not null ||
            exposedNodeCount is not null)
        {
            throw new ArgumentException(
                "Inactive runtime host status values cannot include active runtime metadata.");
        }

        IsRunning = isRunning;
        ActiveAdapterKey = activeAdapterKey?.Trim();
        EndpointUrl = endpointUrl?.Trim();
        ActiveProfileId = activeProfileId;
        ExposedNodeCount = exposedNodeCount;
        LastFault = lastFault;
    }

    /// <summary>
    /// Gets the inactive runtime host status.
    /// </summary>
    public static RuntimeHostStatus Inactive { get; } = new(false, null, null, null, null, null);

    /// <summary>
    /// Creates the canonical active runtime host status.
    /// </summary>
    /// <param name="activeAdapterKey">The active adapter key.</param>
    /// <param name="endpointUrl">The externally readable endpoint URL.</param>
    /// <param name="activeProfileId">The active endpoint/profile identifier.</param>
    /// <param name="exposedNodeCount">The number of exposed nodes.</param>
    /// <returns>The canonical active runtime host status.</returns>
    public static RuntimeHostStatus Active(
        string activeAdapterKey,
        string endpointUrl,
        RuntimeEndpointProfileId activeProfileId,
        int exposedNodeCount)
    {
        return new RuntimeHostStatus(
            true,
            activeAdapterKey,
            endpointUrl,
            activeProfileId,
            exposedNodeCount,
            null);
    }

    /// <summary>
    /// Creates an inactive runtime host status that retains bounded fault information.
    /// </summary>
    /// <param name="lastFault">The bounded retained fault information.</param>
    /// <returns>The faulted inactive runtime host status.</returns>
    public static RuntimeHostStatus Faulted(RuntimeHostFaultInfo lastFault)
    {
        ArgumentNullException.ThrowIfNull(lastFault);
        return new RuntimeHostStatus(false, null, null, null, null, lastFault);
    }

    /// <summary>
    /// Gets a value indicating whether a runtime adapter is currently active.
    /// </summary>
    public bool IsRunning { get; }

    /// <summary>
    /// Gets the active adapter key when the host is running.
    /// </summary>
    public string? ActiveAdapterKey { get; }

    /// <summary>
    /// Gets the externally readable endpoint URL when the host is running.
    /// </summary>
    public string? EndpointUrl { get; }

    /// <summary>
    /// Gets the active endpoint/profile identifier when the host is running.
    /// </summary>
    public RuntimeEndpointProfileId? ActiveProfileId { get; }

    /// <summary>
    /// Gets the number of exposed nodes when the host is running.
    /// </summary>
    public int? ExposedNodeCount { get; }

    /// <summary>
    /// Gets the retained bounded fault information when the host is inactive after an internal failure.
    /// </summary>
    public RuntimeHostFaultInfo? LastFault { get; }
}
