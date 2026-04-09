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
    /// <param name="supportedEndpointProfiles">
    /// The deterministically ordered endpoint/security profile identifiers supported by the adapter.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="key" /> or <paramref name="displayName" /> is null, empty, or whitespace.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="capabilities" /> or <paramref name="supportedEndpointProfiles" /> is null.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <paramref name="supportedEndpointProfiles" /> contains null entries or duplicates.
    /// </exception>
    public RuntimeAdapterDescriptor(
        string key,
        string displayName,
        RuntimeCapabilitySet capabilities,
        IEnumerable<RuntimeEndpointProfileId> supportedEndpointProfiles)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        ArgumentNullException.ThrowIfNull(capabilities);
        ArgumentNullException.ThrowIfNull(supportedEndpointProfiles);

        var materializedProfileIds = supportedEndpointProfiles.ToArray();

        if (Array.Exists(materializedProfileIds, static profileId => profileId is null))
        {
            throw new InvalidOperationException(
                "Runtime adapter descriptors cannot contain null supported endpoint/profile identifiers.");
        }

        var duplicateProfileId = materializedProfileIds
            .GroupBy(static profileId => profileId)
            .FirstOrDefault(static group => group.Count() > 1)?
            .Key;

        if (duplicateProfileId is not null)
        {
            throw new InvalidOperationException(
                $"Runtime adapter descriptors must use unique supported endpoint/profile identifiers. Duplicate profile id: '{duplicateProfileId}'.");
        }

        Key = key.Trim();
        DisplayName = displayName.Trim();
        Capabilities = capabilities;
        SupportedEndpointProfiles = Array.AsReadOnly(
            materializedProfileIds
                .OrderBy(static profileId => profileId.Value, StringComparer.Ordinal)
                .ToArray());
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

    /// <summary>
    /// Gets the deterministically ordered endpoint/security profile identifiers supported by the adapter.
    /// </summary>
    public IReadOnlyList<RuntimeEndpointProfileId> SupportedEndpointProfiles { get; }
}
