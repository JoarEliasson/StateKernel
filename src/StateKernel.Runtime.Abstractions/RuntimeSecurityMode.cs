namespace StateKernel.Runtime.Abstractions;

/// <summary>
/// Identifies the bounded runtime endpoint security mode selected for a runtime profile.
/// </summary>
public enum RuntimeSecurityMode
{
    /// <summary>
    /// Indicates an explicitly insecure endpoint with no message security.
    /// </summary>
    None,

    /// <summary>
    /// Indicates an endpoint that requires signed and encrypted messages.
    /// </summary>
    SignAndEncrypt,
}
