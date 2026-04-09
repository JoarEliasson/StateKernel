namespace StateKernel.RuntimeHost.Hosting;

/// <summary>
/// Defines the closed set of bounded runtime host fault codes.
/// </summary>
public static class RuntimeHostFaultCodes
{
    /// <summary>
    /// The runtime adapter could not be started.
    /// </summary>
    public const string RuntimeStartFailed = "runtime-start-failed";

    /// <summary>
    /// The bounded secure runtime profile could not be realized safely.
    /// </summary>
    public const string SecureStartupFailed = "secure-startup-failed";

    /// <summary>
    /// The runtime adapter failed while applying published value updates.
    /// </summary>
    public const string RuntimeApplyFailed = "runtime-apply-failed";

    /// <summary>
    /// The runtime adapter failed while stopping.
    /// </summary>
    public const string RuntimeStopFailed = "runtime-stop-failed";
}
