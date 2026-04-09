namespace StateKernel.RuntimeHost.Execution;

/// <summary>
/// Provides the bounded executable run definitions available to the execution seam.
/// </summary>
public sealed class SimulationRunDefinitionCatalog : ISimulationRunDefinitionCatalog
{
    private readonly Dictionary<SimulationRunDefinitionId, ISimulationExecutableRunDefinition> definitionsById;

    /// <summary>
    /// Initializes a new instance of the <see cref="SimulationRunDefinitionCatalog" /> type.
    /// </summary>
    /// <param name="definitions">The executable run definitions to register.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="definitions" /> is null.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the supplied definitions contain null entries or duplicate identifiers.
    /// </exception>
    public SimulationRunDefinitionCatalog(IEnumerable<ISimulationExecutableRunDefinition> definitions)
    {
        ArgumentNullException.ThrowIfNull(definitions);

        var materializedDefinitions = definitions.ToArray();

        if (Array.Exists(materializedDefinitions, static definition => definition is null))
        {
            throw new InvalidOperationException(
                "Simulation run-definition catalogs cannot contain null definitions.");
        }

        var duplicateDefinitionId = materializedDefinitions
            .GroupBy(static definition => definition.Id)
            .FirstOrDefault(static group => group.Count() > 1)?
            .Key;

        if (duplicateDefinitionId is not null)
        {
            throw new InvalidOperationException(
                $"Simulation run-definition identifiers must be unique. Duplicate id: '{duplicateDefinitionId}'.");
        }

        All = Array.AsReadOnly(
            materializedDefinitions
                .OrderBy(static definition => definition.Id.Value, StringComparer.Ordinal)
                .ToArray());
        definitionsById = All.ToDictionary(static definition => definition.Id);
    }

    /// <summary>
    /// Gets the deterministic set of executable run definitions.
    /// </summary>
    public IReadOnlyList<ISimulationExecutableRunDefinition> All { get; }

    /// <summary>
    /// Creates the default bounded run-definition catalog for the current baseline.
    /// </summary>
    /// <returns>The default bounded run-definition catalog.</returns>
    public static SimulationRunDefinitionCatalog CreateDefault()
    {
        return new SimulationRunDefinitionCatalog(
        [
            new BaselineConstantSourceSimulationRunDefinition(),
        ]);
    }

    /// <inheritdoc />
    public ISimulationExecutableRunDefinition GetRequired(SimulationRunDefinitionId id)
    {
        ArgumentNullException.ThrowIfNull(id);

        if (!definitionsById.TryGetValue(id, out var definition))
        {
            throw new InvalidOperationException(
                $"No executable simulation run definition is registered for id '{id}'.");
        }

        return definition;
    }
}
