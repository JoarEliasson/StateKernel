using StateKernel.Simulation.Signals;

namespace StateKernel.RuntimeHost.Execution;

/// <summary>
/// Defines the executable run-definition contract used by the baseline execution seam.
/// </summary>
/// <remarks>
/// This is intentionally an executable definition contract for the current slice rather than a
/// pure immutable data definition model. It describes one approved executable simulation shape and
/// can create fresh run-scoped components for each new run instance.
/// </remarks>
public interface ISimulationExecutableRunDefinition
{
    /// <summary>
    /// Gets the stable run-definition identifier.
    /// </summary>
    SimulationRunDefinitionId Id { get; }

    /// <summary>
    /// Gets the user-facing display name for the executable definition.
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// Gets the simulation signals that may be exposed for this executable definition.
    /// </summary>
    IReadOnlyList<SimulationSignalId> ExposableSignals { get; }

    /// <summary>
    /// Gets the default execution settings for runs created from this definition.
    /// </summary>
    SimulationExecutionSettings DefaultExecutionSettings { get; }

    /// <summary>
    /// Creates the fresh run-scoped components needed for one simulation run instance.
    /// </summary>
    /// <returns>The fresh run-scoped execution components.</returns>
    SimulationRunComponents CreateExecutionComponents();
}
