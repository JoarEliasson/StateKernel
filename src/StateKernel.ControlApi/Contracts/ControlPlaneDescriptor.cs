namespace StateKernel.ControlApi.Contracts;

/// <summary>
/// Describes the initial composition exposed by the StateKernel control plane.
/// </summary>
/// <param name="ProductName">The public product name.</param>
/// <param name="Subtitle">The public product subtitle.</param>
/// <param name="Modules">The high-level modules managed through the control plane.</param>
public sealed record ControlPlaneDescriptor(
    string ProductName,
    string Subtitle,
    IReadOnlyList<string> Modules)
{
    private static readonly IReadOnlyList<string> DefaultModules = Array.AsReadOnly(
        new[]
        {
            "StateKernel.ControlApi",
            "StateKernel.RuntimeHost",
            "StateKernel.Simulation",
            "StateKernel.Runtime.UaNet",
            "StateKernel.Observability",
        });

    /// <summary>
    /// Creates the default descriptor returned by the Phase 0 control API.
    /// </summary>
    /// <returns>The default control plane descriptor.</returns>
    public static ControlPlaneDescriptor CreateDefault()
    {
        return new ControlPlaneDescriptor(
            "StateKernel",
            "OPC UA Scenario Studio for Integration, Security, and Benchmarking",
            DefaultModules);
    }
}
