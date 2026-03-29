using StateKernel.Simulation.Signals;

namespace StateKernel.Simulation.Behaviors;

/// <summary>
/// Describes a behavior that declares deterministic upstream signal dependencies.
/// </summary>
/// <remarks>
/// This contract is planning metadata only. <see cref="RequiredSignalIds" /> must be
/// deterministic, duplicate-free, stable across repeated reads, and safe to enumerate multiple
/// times. Declared dependencies do not change runtime execution ordering, committed-snapshot
/// timing, or same-tick visibility rules in this baseline slice.
/// </remarks>
public interface ISignalDependentBehavior
{
    /// <summary>
    /// Gets the declared required upstream signal identifiers for the behavior.
    /// </summary>
    IReadOnlyCollection<SimulationSignalId> RequiredSignalIds { get; }
}
