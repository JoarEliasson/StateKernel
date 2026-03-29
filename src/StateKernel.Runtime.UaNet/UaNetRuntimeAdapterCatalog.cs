using StateKernel.Runtime.Abstractions;

namespace StateKernel.Runtime.UaNet;

/// <summary>
/// Provides descriptor metadata for the .NET-based OPC UA runtime adapter path.
/// </summary>
public static class UaNetRuntimeAdapterCatalog
{
    private static readonly RuntimeCapability[] SupportedCapabilities =
    [
        RuntimeCapability.ReadOnlyValueExposure,
    ];

    /// <summary>
    /// Gets the default descriptor for the baseline UA .NET adapter path.
    /// </summary>
    public static RuntimeAdapterDescriptor Default { get; } = new(
        UaNetRuntimeConstants.AdapterKey,
        ".NET OPC UA Runtime Adapter",
        new RuntimeCapabilitySet(SupportedCapabilities));
}
