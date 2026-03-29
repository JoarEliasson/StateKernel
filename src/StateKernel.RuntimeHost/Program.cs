using StateKernel.RuntimeHost.Hosting;

namespace StateKernel.RuntimeHost;

/// <summary>
/// Hosts the StateKernel runtime execution process.
/// </summary>
public static class Program
{
    /// <summary>
    /// Starts the runtime host process.
    /// </summary>
    /// <param name="args">The command-line arguments supplied to the process.</param>
    /// <returns>The process exit code.</returns>
    public static int Main(string[] args)
    {
        var descriptor = RuntimeHostDescriptor.Default;
        Console.WriteLine(
            $"{descriptor.HostName} initialized with {descriptor.ManagedSubsystems.Count} managed subsystems.");
        return 0;
    }
}
