namespace StateKernel.RuntimeHost.Execution;

/// <summary>
/// Resolves the bounded executable run definitions available to the execution seam.
/// </summary>
public interface ISimulationRunDefinitionCatalog
{
    /// <summary>
    /// Gets the deterministic set of executable run definitions.
    /// </summary>
    IReadOnlyList<ISimulationExecutableRunDefinition> All { get; }

    /// <summary>
    /// Resolves a required executable run definition by identifier.
    /// </summary>
    /// <param name="id">The run-definition identifier to resolve.</param>
    /// <returns>The required executable run definition.</returns>
    ISimulationExecutableRunDefinition GetRequired(SimulationRunDefinitionId id);
}
