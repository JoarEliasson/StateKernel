namespace StateKernel.Runtime.Abstractions;

/// <summary>
/// Identifies the bounded runtime endpoint security policy selected for a runtime profile.
/// </summary>
public enum RuntimeSecurityPolicy
{
    /// <summary>
    /// Indicates that no security policy is applied.
    /// </summary>
    None,

    /// <summary>
    /// Indicates the Basic256Sha256 security policy.
    /// </summary>
    Basic256Sha256,
}
