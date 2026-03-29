namespace StateKernel.Runtime.Abstractions;

/// <summary>
/// Defines the lifecycle and value-publication contract that concrete runtime adapters implement.
/// </summary>
public interface IRuntimeAdapter
{
    /// <summary>
    /// Gets the descriptor exposed by the runtime adapter.
    /// </summary>
    RuntimeAdapterDescriptor Descriptor { get; }

    /// <summary>
    /// Starts the runtime adapter from the supplied compiled runtime plan and endpoint settings.
    /// </summary>
    /// <param name="request">The validated runtime start request.</param>
    /// <param name="cancellationToken">The token that cancels the start operation.</param>
    /// <returns>The runtime start result.</returns>
    ValueTask<RuntimeStartResult> StartAsync(
        RuntimeStartRequest request,
        CancellationToken cancellationToken);

    /// <summary>
    /// Applies the supplied runtime value updates to the externally exposed runtime surface.
    /// </summary>
    /// <param name="updates">The ordered runtime value updates to publish.</param>
    /// <param name="cancellationToken">The token that cancels the update operation.</param>
    /// <returns>A task representing the asynchronous update operation.</returns>
    ValueTask ApplyUpdatesAsync(
        IReadOnlyList<RuntimeValueUpdate> updates,
        CancellationToken cancellationToken);

    /// <summary>
    /// Stops the runtime adapter and releases any externally visible runtime resources.
    /// </summary>
    /// <param name="cancellationToken">The token that cancels the stop operation.</param>
    /// <returns>The runtime stop result.</returns>
    ValueTask<RuntimeStopResult> StopAsync(CancellationToken cancellationToken);
}
