namespace StateKernel.Runtime.Abstractions;

/// <summary>
/// Represents a bounded runtime endpoint/security profile selected for runtime startup.
/// </summary>
public sealed class RuntimeEndpointProfile
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RuntimeEndpointProfile" /> type.
    /// </summary>
    /// <param name="id">The stable profile identifier.</param>
    /// <param name="displayName">The human-readable profile name.</param>
    /// <param name="securityMode">The bounded runtime security mode.</param>
    /// <param name="securityPolicy">The bounded runtime security policy.</param>
    /// <param name="requiresLoopbackEndpoint">
    /// Indicates whether the selected profile requires a loopback endpoint host.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="id" /> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="displayName" /> is null, empty, or whitespace, or when
    /// <paramref name="securityMode" /> and <paramref name="securityPolicy" /> do not form one
    /// of the bounded valid combinations for this slice.
    /// </exception>
    public RuntimeEndpointProfile(
        RuntimeEndpointProfileId id,
        string displayName,
        RuntimeSecurityMode securityMode,
        RuntimeSecurityPolicy securityPolicy,
        bool requiresLoopbackEndpoint)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);

        var usesInsecureSecurity = securityMode == RuntimeSecurityMode.None &&
            securityPolicy == RuntimeSecurityPolicy.None;
        var usesBoundedSecureSecurity = securityMode == RuntimeSecurityMode.SignAndEncrypt &&
            securityPolicy == RuntimeSecurityPolicy.Basic256Sha256;

        if (!usesInsecureSecurity && !usesBoundedSecureSecurity)
        {
            throw new ArgumentException(
                "Runtime endpoint profiles must use either None/None or SignAndEncrypt/Basic256Sha256 in this slice.");
        }

        Id = id;
        DisplayName = displayName.Trim();
        SecurityMode = securityMode;
        SecurityPolicy = securityPolicy;
        RequiresLoopbackEndpoint = requiresLoopbackEndpoint;
    }

    /// <summary>
    /// Gets the stable profile identifier.
    /// </summary>
    public RuntimeEndpointProfileId Id { get; }

    /// <summary>
    /// Gets the human-readable profile name.
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// Gets the bounded runtime security mode.
    /// </summary>
    public RuntimeSecurityMode SecurityMode { get; }

    /// <summary>
    /// Gets the bounded runtime security policy.
    /// </summary>
    public RuntimeSecurityPolicy SecurityPolicy { get; }

    /// <summary>
    /// Gets a value indicating whether the profile requires a loopback endpoint host.
    /// </summary>
    public bool RequiresLoopbackEndpoint { get; }
}
