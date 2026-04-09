namespace StateKernel.ControlApi.Contracts.Runtime;

/// <summary>
/// Captures the bounded control API request used to compose and start a runtime adapter.
/// </summary>
public sealed class StartRuntimeRequest
{
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
    /// Gets or sets the approved signal exposure choices to transform and compose.
    /// </summary>
    public IReadOnlyList<StartRuntimeSignalExposureChoiceRequest>? ExposureChoices { get; init; }
}
