namespace StateKernel.ControlApi.Contracts.Runtime;

/// <summary>
/// Captures one approved simulation-side signal exposure choice submitted through the control API.
/// </summary>
public sealed class StartRuntimeSignalExposureChoiceRequest
{
    /// <summary>
    /// Gets or sets the canonical simulation signal identifier to expose.
    /// </summary>
    public string? SourceSignalId { get; init; }

    /// <summary>
    /// Gets or sets the explicit runtime node identifier override when one is supplied.
    /// </summary>
    public string? TargetNodeIdOverride { get; init; }

    /// <summary>
    /// Gets or sets the optional display name override.
    /// </summary>
    public string? DisplayNameOverride { get; init; }
}
