using StateKernel.Simulation.Signals;

namespace StateKernel.Runtime.Abstractions;

/// <summary>
/// Represents the immutable compiled runtime exposure artifact consumed by runtime adapters.
/// </summary>
public sealed class CompiledRuntimePlan
{
    private readonly Dictionary<SimulationSignalId, RuntimeNodeBinding> bindingsBySignalId;

    /// <summary>
    /// Initializes a new instance of the <see cref="CompiledRuntimePlan" /> type from a runtime projection plan.
    /// </summary>
    /// <param name="projectionPlan">The runtime projection plan to compile.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="projectionPlan" /> is null.
    /// </exception>
    public CompiledRuntimePlan(RuntimeProjectionPlan projectionPlan)
        : this(
            (projectionPlan ?? throw new ArgumentNullException(nameof(projectionPlan)))
                .Projections
                .Select(RuntimeNodeBinding.FromProjection))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CompiledRuntimePlan" /> type from explicit normalized bindings.
    /// </summary>
    /// <param name="bindings">The bindings to compile.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="bindings" /> is null.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the plan contains null bindings, duplicate source signals, or duplicate node identifiers.
    /// </exception>
    public CompiledRuntimePlan(IEnumerable<RuntimeNodeBinding> bindings)
    {
        ArgumentNullException.ThrowIfNull(bindings);

        var materializedBindings = bindings.ToArray();

        if (Array.Exists(materializedBindings, static binding => binding is null))
        {
            throw new InvalidOperationException(
                "Compiled runtime plans cannot contain null bindings.");
        }

        var duplicateSignalId = materializedBindings
            .GroupBy(static binding => binding.SourceSignalId)
            .FirstOrDefault(static group => group.Count() > 1)?
            .Key;

        if (duplicateSignalId is not null)
        {
            throw new InvalidOperationException(
                $"Compiled runtime plans must use unique source signal identifiers. Duplicate signal id: '{duplicateSignalId}'.");
        }

        var duplicateNodeId = materializedBindings
            .GroupBy(static binding => binding.TargetNodeId)
            .FirstOrDefault(static group => group.Count() > 1)?
            .Key;

        if (duplicateNodeId is not null)
        {
            throw new InvalidOperationException(
                $"Compiled runtime plans must use unique target runtime node identifiers. Duplicate node id: '{duplicateNodeId}'.");
        }

        var orderedBindings = materializedBindings
            .OrderBy(static binding => binding.SourceSignalId.Value, StringComparer.Ordinal)
            .ToArray();

        Bindings = Array.AsReadOnly(orderedBindings);
        bindingsBySignalId = orderedBindings.ToDictionary(static binding => binding.SourceSignalId);
    }

    /// <summary>
    /// Gets the deterministically ordered runtime node bindings.
    /// </summary>
    public IReadOnlyList<RuntimeNodeBinding> Bindings { get; }

    /// <summary>
    /// Attempts to get a compiled runtime node binding for the supplied source signal identifier.
    /// </summary>
    /// <param name="signalId">The source simulation signal identifier.</param>
    /// <param name="binding">The resolved binding when one exists.</param>
    /// <returns>
    /// <see langword="true" /> when a binding exists for <paramref name="signalId" />; otherwise,
    /// <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="signalId" /> is null.
    /// </exception>
    public bool TryGetBinding(
        SimulationSignalId signalId,
        out RuntimeNodeBinding binding)
    {
        ArgumentNullException.ThrowIfNull(signalId);

        if (bindingsBySignalId.TryGetValue(signalId, out var resolvedBinding))
        {
            binding = resolvedBinding;
            return true;
        }

        binding = null!;
        return false;
    }

    /// <summary>
    /// Gets a required compiled runtime node binding for the supplied source signal identifier.
    /// </summary>
    /// <param name="signalId">The source simulation signal identifier.</param>
    /// <returns>The required compiled runtime node binding.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="signalId" /> is null.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the signal is not projected in the compiled runtime plan.
    /// </exception>
    public RuntimeNodeBinding GetRequiredBinding(SimulationSignalId signalId)
    {
        if (TryGetBinding(signalId, out var binding))
        {
            return binding;
        }

        throw new InvalidOperationException(
            $"A projected runtime binding for signal '{signalId}' was required but was not available.");
    }
}
