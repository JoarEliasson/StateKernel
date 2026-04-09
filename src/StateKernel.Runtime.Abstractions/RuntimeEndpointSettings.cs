namespace StateKernel.Runtime.Abstractions;

/// <summary>
/// Defines the endpoint host settings used to host a runtime adapter.
/// </summary>
public sealed class RuntimeEndpointSettings
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RuntimeEndpointSettings" /> type.
    /// </summary>
    /// <param name="host">The endpoint host name or address.</param>
    /// <param name="port">
    /// The TCP port to bind. A value of <c>0</c> requests a test-friendly dynamically resolved port.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="host" /> is null, empty, or whitespace.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="port" /> is negative or greater than 65535.
    /// </exception>
    public RuntimeEndpointSettings(string host, int port)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(host);

        if (port is < 0 or > 65535)
        {
            throw new ArgumentOutOfRangeException(
                nameof(port),
                "Runtime endpoint ports must be between 0 and 65535.");
        }

        Host = host.Trim();
        Port = port;
    }

    /// <summary>
    /// Gets the endpoint host name or address.
    /// </summary>
    public string Host { get; }

    /// <summary>
    /// Gets the TCP port to bind. A value of <c>0</c> indicates that the adapter should resolve a free port.
    /// </summary>
    public int Port { get; }

    /// <summary>
    /// Creates loopback endpoint settings with the supplied port.
    /// </summary>
    /// <param name="port">The requested loopback port.</param>
    /// <returns>Loopback endpoint settings.</returns>
    public static RuntimeEndpointSettings Loopback(int port = 0)
    {
        return new RuntimeEndpointSettings("127.0.0.1", port);
    }
}
