using StateKernel.Runtime.Abstractions;

namespace StateKernel.Runtime.UaNet;

/// <summary>
/// Creates baseline UA .NET runtime adapter instances.
/// </summary>
public sealed class UaNetRuntimeAdapterFactory : IRuntimeAdapterFactory
{
    /// <inheritdoc />
    public RuntimeAdapterDescriptor Descriptor => UaNetRuntimeAdapterCatalog.Default;

    /// <inheritdoc />
    public IRuntimeAdapter CreateAdapter()
    {
        return new UaNetRuntimeAdapter();
    }
}
