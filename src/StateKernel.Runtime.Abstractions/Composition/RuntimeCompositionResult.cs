namespace StateKernel.Runtime.Abstractions.Composition;

/// <summary>
/// Represents the pair of validated runtime exposure artifacts produced by composition.
/// </summary>
/// <remarks>
/// This is a convenience artifact only. It exists to return both the validated
/// <see cref="RuntimeProjectionPlan" /> and the adapter-consumable <see cref="CompiledRuntimePlan" />
/// together without becoming a start request, diagnostics object, or orchestration artifact.
/// </remarks>
public sealed class RuntimeCompositionResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RuntimeCompositionResult" /> type.
    /// </summary>
    /// <param name="adapterKey">The stable adapter key preserved from the composition request.</param>
    /// <param name="projectionPlan">The validated runtime projection plan.</param>
    /// <param name="compiledRuntimePlan">The compiled runtime plan derived from the projection plan.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="adapterKey" /> is null, empty, or whitespace.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="projectionPlan" /> or <paramref name="compiledRuntimePlan" /> is null.
    /// </exception>
    public RuntimeCompositionResult(
        string adapterKey,
        RuntimeProjectionPlan projectionPlan,
        CompiledRuntimePlan compiledRuntimePlan)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(adapterKey);
        ArgumentNullException.ThrowIfNull(projectionPlan);
        ArgumentNullException.ThrowIfNull(compiledRuntimePlan);

        AdapterKey = adapterKey.Trim();
        ProjectionPlan = projectionPlan;
        CompiledRuntimePlan = compiledRuntimePlan;
    }

    /// <summary>
    /// Gets the stable adapter key preserved from the composition request.
    /// </summary>
    public string AdapterKey { get; }

    /// <summary>
    /// Gets the validated runtime projection plan.
    /// </summary>
    public RuntimeProjectionPlan ProjectionPlan { get; }

    /// <summary>
    /// Gets the adapter-consumable compiled runtime plan.
    /// </summary>
    public CompiledRuntimePlan CompiledRuntimePlan { get; }
}
