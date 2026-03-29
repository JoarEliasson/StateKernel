namespace StateKernel.Runtime.UaNet;

/// <summary>
/// Provides stable constants used by the baseline UA .NET runtime adapter path.
/// </summary>
public static class UaNetRuntimeConstants
{
    /// <summary>
    /// Gets the stable adapter key for the UA .NET runtime adapter path.
    /// </summary>
    public const string AdapterKey = "ua-net";

    /// <summary>
    /// Gets the stable namespace URI used by the baseline UA runtime exposure nodes.
    /// </summary>
    public const string NamespaceUri = "urn:statekernel:runtime:ua-net";

    /// <summary>
    /// Gets the baseline relative browse path for the projected signals folder.
    /// </summary>
    public const string SignalsFolderName = "Signals";

    /// <summary>
    /// Gets the baseline endpoint path suffix used by the UA .NET runtime adapter.
    /// </summary>
    public const string EndpointPath = "/StateKernel/Runtime";
}
