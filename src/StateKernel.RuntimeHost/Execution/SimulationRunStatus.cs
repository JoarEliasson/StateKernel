using StateKernel.Runtime.Abstractions;

namespace StateKernel.RuntimeHost.Execution;

/// <summary>
/// Represents the canonical read model for the active simulation run state.
/// </summary>
public sealed class SimulationRunStatus
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SimulationRunStatus" /> type.
    /// </summary>
    /// <param name="isActive">Indicates whether a simulation run is currently active.</param>
    /// <param name="activeRunId">The active run identifier when a run is active.</param>
    /// <param name="activeRunDefinitionId">The active run-definition identifier when active.</param>
    /// <param name="runtimeAdapterKey">The configured runtime adapter key when a run is active.</param>
    /// <param name="activeProfileId">The configured runtime profile identifier when a run is active.</param>
    /// <param name="endpointUrl">The externally readable runtime endpoint URL when attached.</param>
    /// <param name="exposedNodeCount">The number of exposed runtime nodes when attached.</param>
    /// <param name="lastCompletedTick">The last deterministic tick completed by the run.</param>
    /// <param name="runtimeAttached">Indicates whether the active run currently has an attached runtime.</param>
    /// <param name="lastFault">
    /// The retained bounded fault information when the run is inactive because of an internal failure.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown when the supplied metadata does not match the requested active state.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="exposedNodeCount" /> or <paramref name="lastCompletedTick" /> is negative.
    /// </exception>
    private SimulationRunStatus(
        bool isActive,
        SimulationRunId? activeRunId,
        SimulationRunDefinitionId? activeRunDefinitionId,
        string? runtimeAdapterKey,
        RuntimeEndpointProfileId? activeProfileId,
        string? endpointUrl,
        int? exposedNodeCount,
        long? lastCompletedTick,
        bool runtimeAttached,
        SimulationRunFaultInfo? lastFault)
    {
        if (exposedNodeCount is < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(exposedNodeCount),
                "Exposed runtime node counts cannot be negative.");
        }

        if (lastCompletedTick is < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(lastCompletedTick),
                "Completed tick values cannot be negative.");
        }

        if (isActive)
        {
            ArgumentNullException.ThrowIfNull(activeRunId);
            ArgumentNullException.ThrowIfNull(activeRunDefinitionId);

            if (string.IsNullOrWhiteSpace(runtimeAdapterKey))
            {
                throw new ArgumentException(
                    "Active simulation run status values must include a runtime adapter key.",
                    nameof(runtimeAdapterKey));
            }

            ArgumentNullException.ThrowIfNull(activeProfileId);

            if (!runtimeAttached)
            {
                throw new ArgumentException(
                    "Active simulation run status values must indicate an attached runtime.",
                    nameof(runtimeAttached));
            }

            if (string.IsNullOrWhiteSpace(endpointUrl))
            {
                throw new ArgumentException(
                    "Active simulation run status values must include an endpoint URL.",
                    nameof(endpointUrl));
            }

            if (exposedNodeCount is null)
            {
                throw new ArgumentException(
                    "Active simulation run status values must include an exposed node count.",
                    nameof(exposedNodeCount));
            }

            if (lastFault is not null)
            {
                throw new ArgumentException(
                    "Active simulation run status values cannot retain fault information.",
                    nameof(lastFault));
            }
        }
        else if (activeRunId is not null ||
            activeRunDefinitionId is not null ||
            runtimeAdapterKey is not null ||
            activeProfileId is not null ||
            endpointUrl is not null ||
            exposedNodeCount is not null ||
            lastCompletedTick is not null ||
            runtimeAttached)
        {
            throw new ArgumentException(
                "Inactive simulation run status values cannot include active run metadata.");
        }

        IsActive = isActive;
        ActiveRunId = activeRunId;
        ActiveRunDefinitionId = activeRunDefinitionId;
        RuntimeAdapterKey = runtimeAdapterKey?.Trim();
        ActiveProfileId = activeProfileId;
        EndpointUrl = endpointUrl?.Trim();
        ExposedNodeCount = exposedNodeCount;
        LastCompletedTick = lastCompletedTick;
        RuntimeAttached = runtimeAttached;
        LastFault = lastFault;
    }

    /// <summary>
    /// Gets the inactive simulation run status.
    /// </summary>
    public static SimulationRunStatus Inactive { get; } = new(
        false,
        null,
        null,
        null,
        null,
        null,
        null,
        null,
        false,
        null);

    /// <summary>
    /// Creates the canonical active simulation run status.
    /// </summary>
    /// <param name="activeRunId">The active run identifier.</param>
    /// <param name="activeRunDefinitionId">The active run-definition identifier.</param>
    /// <param name="runtimeAdapterKey">The attached runtime adapter key.</param>
    /// <param name="activeProfileId">The attached runtime profile identifier.</param>
    /// <param name="endpointUrl">The externally readable runtime endpoint URL.</param>
    /// <param name="exposedNodeCount">The number of exposed runtime nodes.</param>
    /// <param name="lastCompletedTick">
    /// The last completed deterministic tick, or null only before the first manual step completes.
    /// </param>
    /// <returns>The canonical active simulation run status.</returns>
    public static SimulationRunStatus Active(
        SimulationRunId activeRunId,
        SimulationRunDefinitionId activeRunDefinitionId,
        string runtimeAdapterKey,
        RuntimeEndpointProfileId activeProfileId,
        string endpointUrl,
        int exposedNodeCount,
        long? lastCompletedTick)
    {
        return new SimulationRunStatus(
            true,
            activeRunId,
            activeRunDefinitionId,
            runtimeAdapterKey,
            activeProfileId,
            endpointUrl,
            exposedNodeCount,
            lastCompletedTick,
            true,
            null);
    }

    /// <summary>
    /// Creates an inactive simulation run status that retains bounded fault information.
    /// </summary>
    /// <param name="lastFault">The bounded retained fault information.</param>
    /// <returns>The faulted inactive simulation run status.</returns>
    public static SimulationRunStatus Faulted(SimulationRunFaultInfo lastFault)
    {
        ArgumentNullException.ThrowIfNull(lastFault);
        return new SimulationRunStatus(
            false,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            false,
            lastFault);
    }

    /// <summary>
    /// Gets a value indicating whether a simulation run is currently active.
    /// </summary>
    public bool IsActive { get; }

    /// <summary>
    /// Gets the active run identifier when a run is active.
    /// </summary>
    public SimulationRunId? ActiveRunId { get; }

    /// <summary>
    /// Gets the active run-definition identifier when a run is active.
    /// </summary>
    public SimulationRunDefinitionId? ActiveRunDefinitionId { get; }

    /// <summary>
    /// Gets the configured runtime adapter key when a run is active.
    /// </summary>
    public string? RuntimeAdapterKey { get; }

    /// <summary>
    /// Gets the configured runtime endpoint/profile identifier when a run is active.
    /// </summary>
    public RuntimeEndpointProfileId? ActiveProfileId { get; }

    /// <summary>
    /// Gets the externally readable runtime endpoint URL when the run has an attached runtime.
    /// </summary>
    public string? EndpointUrl { get; }

    /// <summary>
    /// Gets the number of exposed runtime nodes when the run has an attached runtime.
    /// </summary>
    public int? ExposedNodeCount { get; }

    /// <summary>
    /// Gets the last deterministic tick completed by the run.
    /// </summary>
    public long? LastCompletedTick { get; }

    /// <summary>
    /// Gets a value indicating whether the run currently has an attached runtime.
    /// </summary>
    public bool RuntimeAttached { get; }

    /// <summary>
    /// Gets the retained bounded fault information when the run is inactive after an internal failure.
    /// </summary>
    public SimulationRunFaultInfo? LastFault { get; }
}
