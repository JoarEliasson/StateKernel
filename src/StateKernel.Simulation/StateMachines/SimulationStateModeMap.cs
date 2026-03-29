using System.Collections.ObjectModel;
using StateKernel.Simulation.Exceptions;
using StateKernel.Simulation.Modes;

namespace StateKernel.Simulation.StateMachines;

/// <summary>
/// Represents the immutable explicit mapping from formal states to operating modes.
/// </summary>
/// <remarks>
/// This mapping is structurally immutable after construction. It owns the baseline
/// <c>State -> Mode</c> relationship without collapsing the two concepts into one type.
/// Multiple states may map to the same operating mode.
/// </remarks>
public sealed class SimulationStateModeMap
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SimulationStateModeMap" /> type.
    /// </summary>
    /// <param name="definition">The state-machine definition that the map must cover exactly.</param>
    /// <param name="mappings">The explicit state-to-mode bindings.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="definition" /> or <paramref name="mappings" /> is null.
    /// </exception>
    /// <exception cref="SimulationConfigurationException">
    /// Thrown when the bindings are null, duplicate, incomplete, or reference undefined states.
    /// </exception>
    public SimulationStateModeMap(
        SimulationStateMachineDefinition definition,
        IEnumerable<KeyValuePair<SimulationStateId, SimulationMode>> mappings)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(mappings);

        var materializedMappings = mappings.ToArray();

        if (Array.Exists(materializedMappings, binding => binding.Key is null))
        {
            throw new SimulationConfigurationException(
                "State-to-mode mappings cannot contain null state identifiers.");
        }

        if (Array.Exists(materializedMappings, binding => binding.Value is null))
        {
            throw new SimulationConfigurationException(
                "State-to-mode mappings cannot contain null modes.");
        }

        var duplicateBinding = materializedMappings
            .GroupBy(binding => binding.Key)
            .FirstOrDefault(group => group.Count() > 1)?
            .Key;

        if (duplicateBinding is not null)
        {
            throw new SimulationConfigurationException(
                $"Each formal state must map to exactly one operating mode. Duplicate state binding: '{duplicateBinding}'.");
        }

        var definedStateIds = definition.States
            .Select(state => state.Id)
            .ToHashSet();
        var undefinedBinding = materializedMappings
            .FirstOrDefault(binding => !definedStateIds.Contains(binding.Key));

        if (undefinedBinding.Key is not null)
        {
            throw new SimulationConfigurationException(
                $"State-to-mode mappings cannot reference undefined state '{undefinedBinding.Key}'.");
        }

        var bindingDictionary = materializedMappings.ToDictionary(
            binding => binding.Key,
            binding => binding.Value);
        var missingState = definedStateIds.FirstOrDefault(stateId => !bindingDictionary.ContainsKey(stateId));

        if (missingState is not null)
        {
            throw new SimulationConfigurationException(
                $"State-to-mode mappings must define an operating mode for formal state '{missingState}'.");
        }

        Definition = definition;
        Mappings = new ReadOnlyDictionary<SimulationStateId, SimulationMode>(bindingDictionary);
    }

    /// <summary>
    /// Gets the state-machine definition covered exactly by this mapping.
    /// </summary>
    public SimulationStateMachineDefinition Definition { get; }

    /// <summary>
    /// Gets the read-only explicit state-to-mode bindings.
    /// </summary>
    public IReadOnlyDictionary<SimulationStateId, SimulationMode> Mappings { get; }

    /// <summary>
    /// Gets the operating mode mapped to the supplied formal state.
    /// </summary>
    /// <param name="stateId">The formal state whose mapped operating mode is required.</param>
    /// <returns>The operating mode mapped to <paramref name="stateId" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="stateId" /> is null.
    /// </exception>
    /// <exception cref="SimulationConfigurationException">
    /// Thrown when <paramref name="stateId" /> is not covered by this mapping.
    /// </exception>
    public SimulationMode GetMode(SimulationStateId stateId)
    {
        ArgumentNullException.ThrowIfNull(stateId);

        if (!Mappings.TryGetValue(stateId, out var mode))
        {
            throw new SimulationConfigurationException(
                $"State-to-mode mappings do not define an operating mode for formal state '{stateId}'.");
        }

        return mode;
    }
}
