namespace StateKernel.Simulation.StateMachines;

/// <summary>
/// Represents a formal simulation state definition.
/// </summary>
public sealed class SimulationStateDefinition
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SimulationStateDefinition" /> type.
    /// </summary>
    /// <param name="id">The canonical formal state identifier.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="id" /> is null.
    /// </exception>
    public SimulationStateDefinition(SimulationStateId id)
    {
        ArgumentNullException.ThrowIfNull(id);
        Id = id;
    }

    /// <summary>
    /// Gets the canonical formal state identifier.
    /// </summary>
    public SimulationStateId Id { get; }
}
