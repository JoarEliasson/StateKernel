namespace StateKernel.ControlApi.Contracts.Runtime;

/// <summary>
/// Represents the canonical runtime status payload exposed by the control API.
/// </summary>
/// <param name="IsActive">Indicates whether a runtime adapter is currently active.</param>
/// <param name="AdapterKey">The active adapter key when the runtime is active.</param>
/// <param name="EndpointUrl">The externally readable runtime endpoint URL when active.</param>
/// <param name="ProfileId">The active endpoint/profile identifier when active.</param>
/// <param name="ProjectedNodeCount">The projected node count when active.</param>
/// <param name="FaultCode">The bounded retained fault code when the runtime is inactive because of an internal failure.</param>
/// <param name="FaultMessage">The bounded retained fault message when the runtime is inactive because of an internal failure.</param>
/// <param name="FaultOccurredAtUtc">The UTC timestamp at which the retained fault occurred.</param>
public sealed record RuntimeStatusResponse(
    bool IsActive,
    string? AdapterKey,
    string? EndpointUrl,
    string? ProfileId,
    int? ProjectedNodeCount,
    string? FaultCode,
    string? FaultMessage,
    DateTimeOffset? FaultOccurredAtUtc);
