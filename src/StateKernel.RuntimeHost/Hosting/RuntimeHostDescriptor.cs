namespace StateKernel.RuntimeHost.Hosting;

/// <summary>
/// Describes the responsibilities currently owned by the runtime host process.
/// </summary>
/// <param name="HostName">The runtime host process name.</param>
/// <param name="ManagedSubsystems">The subsystems coordinated by the runtime host.</param>
public sealed record RuntimeHostDescriptor(string HostName, IReadOnlyList<string> ManagedSubsystems)
{
    private static readonly IReadOnlyList<string> DefaultManagedSubsystems = Array.AsReadOnly(
        new[]
        {
            "StateKernel.Simulation",
            "StateKernel.Runtime.Abstractions",
            "StateKernel.Observability",
        });

    /// <summary>
    /// Gets the default runtime host descriptor for the solution foundation.
    /// </summary>
    public static RuntimeHostDescriptor Default { get; } = new(
        "StateKernel.RuntimeHost",
        DefaultManagedSubsystems);
}
