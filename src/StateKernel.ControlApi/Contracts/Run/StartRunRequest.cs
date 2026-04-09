using StateKernel.ControlApi.Contracts.Runtime;

namespace StateKernel.ControlApi.Contracts.Run;

/// <summary>
/// Captures the bounded control API request used to start a simulation run and its attached runtime.
/// </summary>
/// <remarks>
/// This request is intentionally a baseline orchestration contract for the current slice. It is
/// not yet the final long-term control-plane contract for run authoring or scenario execution.
/// </remarks>
public sealed class StartRunRequest
{
    /// <summary>
    /// Gets or sets the executable run-definition identifier to start.
    /// </summary>
    public string? RunDefinitionId { get; init; }

    /// <summary>
    /// Gets or sets the stable runtime adapter key.
    /// </summary>
    public string? AdapterKey { get; init; }

    /// <summary>
    /// Gets or sets the stable runtime endpoint/profile identifier.
    /// </summary>
    public string? ProfileId { get; init; }

    /// <summary>
    /// Gets or sets the endpoint host name or address.
    /// </summary>
    public string? EndpointHost { get; init; }

    /// <summary>
    /// Gets or sets the endpoint TCP port. A value of <c>0</c> requests an ephemeral port.
    /// </summary>
    public int EndpointPort { get; init; }

    /// <summary>
    /// Gets or sets the optional runtime node-id prefix override used during composition.
    /// </summary>
    public string? NodeIdPrefix { get; init; }

    /// <summary>
    /// Gets or sets the approved signal exposure choices to transform, validate, and compose.
    /// </summary>
    public IReadOnlyList<StartRuntimeSignalExposureChoiceRequest>? ExposureChoices { get; init; }
}
