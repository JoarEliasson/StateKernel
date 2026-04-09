namespace StateKernel.Runtime.Abstractions.Composition;

/// <summary>
/// Composes deterministic runtime exposure artifacts from runtime-facing signal selections.
/// </summary>
public static class RuntimeCompositionService
{
    /// <summary>
    /// Composes a validated runtime projection plan and compiled runtime plan from the supplied request.
    /// </summary>
    /// <param name="request">The runtime composition request to compose.</param>
    /// <returns>The composed runtime projection and compiled runtime artifacts.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="request" /> is null.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the request resolves to duplicate effective runtime node identifiers after
    /// applying defaults and explicit overrides.
    /// </exception>
    public static RuntimeCompositionResult Compose(RuntimeCompositionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var orderedSelections = request.SignalSelections
            .OrderBy(static selection => selection.SourceSignalId.Value, StringComparer.Ordinal)
            .ToArray();

        var duplicateEffectiveNodeId = orderedSelections
            .Select(selection => ResolveTargetNodeId(selection, request.Defaults))
            .GroupBy(static nodeId => nodeId)
            .FirstOrDefault(static group => group.Count() > 1)?
            .Key;

        if (duplicateEffectiveNodeId is not null)
        {
            throw new InvalidOperationException(
                $"Runtime composition requests must resolve to unique effective runtime node identifiers after applying defaults and overrides. Duplicate node id: '{duplicateEffectiveNodeId}'.");
        }

        var projections = orderedSelections
            .Select(selection =>
                new SimulationSignalProjection(
                    selection.SourceSignalId,
                    ResolveTargetNodeId(selection, request.Defaults),
                    selection.DisplayNameOverride ?? selection.SourceSignalId.Value))
            .ToArray();

        var projectionPlan = new RuntimeProjectionPlan(projections);
        var compiledRuntimePlan = new CompiledRuntimePlan(projectionPlan);

        return new RuntimeCompositionResult(
            request.AdapterKey,
            projectionPlan,
            compiledRuntimePlan);
    }

    private static RuntimeNodeId ResolveTargetNodeId(
        RuntimeSignalSelection selection,
        RuntimeCompositionDefaults defaults)
    {
        return selection.TargetNodeId ??
            RuntimeNodeId.From($"{defaults.NodeIdPrefix}/{selection.SourceSignalId.Value}");
    }
}
