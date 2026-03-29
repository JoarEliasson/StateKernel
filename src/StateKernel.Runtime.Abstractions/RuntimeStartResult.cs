namespace StateKernel.Runtime.Abstractions;

/// <summary>
/// Represents the result of starting a runtime adapter.
/// </summary>
public sealed class RuntimeStartResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RuntimeStartResult" /> type.
    /// </summary>
    /// <param name="endpointUrl">The externally readable runtime endpoint URL.</param>
    /// <param name="exposedNodeCount">The number of exposed runtime nodes.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="endpointUrl" /> is null, empty, or whitespace.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="exposedNodeCount" /> is negative.
    /// </exception>
    public RuntimeStartResult(string endpointUrl, int exposedNodeCount)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(endpointUrl);

        if (exposedNodeCount < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(exposedNodeCount),
                "Exposed runtime node counts cannot be negative.");
        }

        EndpointUrl = endpointUrl.Trim();
        ExposedNodeCount = exposedNodeCount;
    }

    /// <summary>
    /// Gets the externally readable runtime endpoint URL.
    /// </summary>
    public string EndpointUrl { get; }

    /// <summary>
    /// Gets the number of exposed runtime nodes.
    /// </summary>
    public int ExposedNodeCount { get; }
}
