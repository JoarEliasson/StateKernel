namespace StateKernel.Runtime.Abstractions;

/// <summary>
/// Provides the bounded runtime endpoint/security profiles supported in the current baseline.
/// </summary>
public static class RuntimeEndpointProfiles
{
    /// <summary>
    /// Gets the low-friction loopback-oriented development profile.
    /// </summary>
    public static RuntimeEndpointProfile LocalDevelopment { get; } = new(
        RuntimeEndpointProfileId.From("local-dev"),
        "Local Development (Insecure)",
        RuntimeSecurityMode.None,
        RuntimeSecurityPolicy.None,
        true);

    /// <summary>
    /// Gets the strongest bounded secure profile supported in this slice.
    /// </summary>
    public static RuntimeEndpointProfile BaselineSecure { get; } = new(
        RuntimeEndpointProfileId.From("baseline-secure"),
        "Baseline Secure",
        RuntimeSecurityMode.SignAndEncrypt,
        RuntimeSecurityPolicy.Basic256Sha256,
        false);

    /// <summary>
    /// Gets the deterministic set of bounded runtime endpoint/security profiles.
    /// </summary>
    public static IReadOnlyList<RuntimeEndpointProfile> All { get; } = Array.AsReadOnly(
        new[]
        {
            LocalDevelopment,
            BaselineSecure,
        });

    private static readonly Dictionary<RuntimeEndpointProfileId, RuntimeEndpointProfile> ProfilesById =
        All.ToDictionary(static profile => profile.Id);

    /// <summary>
    /// Resolves a required runtime endpoint/security profile by identifier.
    /// </summary>
    /// <param name="id">The profile identifier to resolve.</param>
    /// <returns>The required runtime endpoint/security profile.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="id" /> is null.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no bounded profile exists for the supplied identifier.
    /// </exception>
    public static RuntimeEndpointProfile GetRequired(RuntimeEndpointProfileId id)
    {
        ArgumentNullException.ThrowIfNull(id);

        if (!ProfilesById.TryGetValue(id, out var profile))
        {
            throw new InvalidOperationException(
                $"No runtime endpoint/security profile is registered for profile id '{id}'.");
        }

        return profile;
    }
}
