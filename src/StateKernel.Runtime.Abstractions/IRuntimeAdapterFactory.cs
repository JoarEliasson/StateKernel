namespace StateKernel.Runtime.Abstractions;

/// <summary>
/// Creates runtime adapter instances for a single advertised descriptor.
/// </summary>
public interface IRuntimeAdapterFactory
{
    /// <summary>
    /// Gets the descriptor advertised by the factory and all adapters it creates.
    /// </summary>
    RuntimeAdapterDescriptor Descriptor { get; }

    /// <summary>
    /// Creates a new runtime adapter instance.
    /// </summary>
    /// <returns>A fresh runtime adapter instance.</returns>
    IRuntimeAdapter CreateAdapter();
}
