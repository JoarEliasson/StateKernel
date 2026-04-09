namespace StateKernel.ControlApi.Contracts.Run;

/// <summary>
/// Represents the canonical run status payload exposed by the control API.
/// </summary>
/// <param name="IsActive">Indicates whether a simulation run is currently active.</param>
/// <param name="RunId">The active run identifier when the run is active.</param>
/// <param name="RunDefinitionId">The active run-definition identifier when the run is active.</param>
/// <param name="AdapterKey">The active runtime adapter key when the run is active.</param>
/// <param name="EndpointUrl">The externally readable runtime endpoint URL when attached.</param>
/// <param name="ProfileId">The active runtime profile identifier when the run is active.</param>
/// <param name="ProjectedNodeCount">The projected node count when the run is active.</param>
/// <param name="LastCompletedTick">The last deterministic tick completed by the run.</param>
/// <param name="FaultCode">The bounded retained fault code when the run is inactive because of an internal failure.</param>
/// <param name="FaultMessage">The bounded retained fault message when the run is inactive because of an internal failure.</param>
/// <param name="FaultOccurredAtUtc">The UTC timestamp at which the retained fault occurred.</param>
public sealed record RunStatusResponse(
    bool IsActive,
    string? RunId,
    string? RunDefinitionId,
    string? AdapterKey,
    string? EndpointUrl,
    string? ProfileId,
    int? ProjectedNodeCount,
    long? LastCompletedTick,
    string? FaultCode,
    string? FaultMessage,
    DateTimeOffset? FaultOccurredAtUtc);
