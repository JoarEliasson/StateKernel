using System.Collections;
using System.Collections.ObjectModel;

namespace StateKernel.Runtime.Abstractions;

/// <summary>
/// Represents the immutable capability set advertised by a runtime adapter.
/// </summary>
public sealed class RuntimeCapabilitySet : IReadOnlyCollection<RuntimeCapability>
{
    private readonly HashSet<RuntimeCapability> capabilities;
    private readonly ReadOnlyCollection<RuntimeCapability> orderedCapabilities;

    /// <summary>
    /// Initializes a new instance of the <see cref="RuntimeCapabilitySet" /> type.
    /// </summary>
    /// <param name="capabilities">The capabilities to include in the set.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="capabilities" /> is null.
    /// </exception>
    public RuntimeCapabilitySet(IEnumerable<RuntimeCapability> capabilities)
    {
        ArgumentNullException.ThrowIfNull(capabilities);
        this.capabilities = new HashSet<RuntimeCapability>(capabilities);
        orderedCapabilities = Array.AsReadOnly(
            this.capabilities
                .OrderBy(static capability => capability)
                .ToArray());
    }

    /// <summary>
    /// Gets an empty runtime capability set.
    /// </summary>
    public static RuntimeCapabilitySet Empty { get; } = new(Array.Empty<RuntimeCapability>());

    /// <summary>
    /// Gets the number of capabilities in the set.
    /// </summary>
    public int Count => orderedCapabilities.Count;

    /// <summary>
    /// Determines whether the set contains the requested capability.
    /// </summary>
    /// <param name="capability">The capability to check.</param>
    /// <returns><see langword="true" /> when the capability is present; otherwise, <see langword="false" />.</returns>
    public bool Supports(RuntimeCapability capability)
    {
        return capabilities.Contains(capability);
    }

    /// <summary>
    /// Returns an enumerator for the capability set.
    /// </summary>
    /// <returns>An enumerator for the capability set.</returns>
    public IEnumerator<RuntimeCapability> GetEnumerator()
    {
        return orderedCapabilities.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
