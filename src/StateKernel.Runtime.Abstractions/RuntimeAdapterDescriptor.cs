namespace StateKernel.Runtime.Abstractions;

/// <summary>
/// Describes a runtime adapter implementation available to the runtime host.
/// </summary>
public sealed record RuntimeAdapterDescriptor
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RuntimeAdapterDescriptor" /> type.
    /// </summary>
    /// <param name="key">The stable adapter key used by runtime host requests.</param>
    /// <param name="displayName">The human-readable adapter name.</param>
    /// <param name="capabilities">The capability set advertised by the adapter.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="key" /> or <paramref name="displayName" /> is null, empty, or whitespace.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="capabilities" /> is null.
    /// </exception>
    public RuntimeAdapterDescriptor(
        string key,
        string displayName,
        RuntimeCapabilitySet capabilities)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        ArgumentNullException.ThrowIfNull(capabilities);

        Key = key.Trim();
        DisplayName = displayName.Trim();
        Capabilities = capabilities;
    }

    /// <summary>
    /// Gets the stable adapter key used by runtime host requests.
    /// </summary>
    public string Key { get; }

    /// <summary>
    /// Gets the human-readable adapter name.
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// Gets the capability set advertised by the adapter.
    /// </summary>
    public RuntimeCapabilitySet Capabilities { get; }
}
