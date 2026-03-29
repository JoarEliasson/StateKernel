using StateKernel.Simulation.Scheduling;

namespace StateKernel.Simulation.Signals;

/// <summary>
/// Describes scheduled work that publishes a deterministic simulation signal.
/// </summary>
/// <remarks>
/// This is an optional capability seam for validation and inspection only. It does not change the
/// generic scheduler work contract.
/// </remarks>
public interface ISignalProducingWork : IScheduledWork
{
    /// <summary>
    /// Gets the published signal identifier, if the work publishes one.
    /// </summary>
    SimulationSignalId? ProducedSignalId { get; }
}
