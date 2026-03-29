namespace StateKernel.Simulation.Signals.Dependencies;

/// <summary>
/// Represents the immutable declared signal dependency plan for one validated scheduler plan.
/// </summary>
/// <remarks>
/// This type is a planning and validation artifact only. It records discovered published signals
/// and resolved declared dependencies for inspection and tests. It does not drive runtime
/// execution ordering and does not alter committed-snapshot timing or same-tick visibility rules.
/// </remarks>
public sealed class SimulationDependencyPlan
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SimulationDependencyPlan" /> type.
    /// </summary>
    /// <param name="publishedSignals">The published signals discovered during planning.</param>
    /// <param name="dependencyBindings">
    /// The resolved dependency bindings discovered during planning.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="publishedSignals" /> or <paramref name="dependencyBindings" />
    /// is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when either input contains a null entry.
    /// </exception>
    public SimulationDependencyPlan(
        IEnumerable<SimulationPublishedSignal> publishedSignals,
        IEnumerable<SimulationSignalDependencyBinding> dependencyBindings)
    {
        ArgumentNullException.ThrowIfNull(publishedSignals);
        ArgumentNullException.ThrowIfNull(dependencyBindings);

        var materializedPublishedSignals = publishedSignals.ToArray();
        var materializedDependencyBindings = dependencyBindings.ToArray();

        if (Array.Exists(materializedPublishedSignals, static item => item is null))
        {
            throw new ArgumentException(
                "Dependency plans cannot contain null published signal entries.",
                nameof(publishedSignals));
        }

        if (Array.Exists(materializedDependencyBindings, static item => item is null))
        {
            throw new ArgumentException(
                "Dependency plans cannot contain null dependency binding entries.",
                nameof(dependencyBindings));
        }

        PublishedSignals = Array.AsReadOnly(materializedPublishedSignals);
        DependencyBindings = Array.AsReadOnly(materializedDependencyBindings);
    }

    /// <summary>
    /// Gets the published signals discovered during planning.
    /// </summary>
    public IReadOnlyList<SimulationPublishedSignal> PublishedSignals { get; }

    /// <summary>
    /// Gets the resolved dependency bindings discovered during planning.
    /// </summary>
    public IReadOnlyList<SimulationSignalDependencyBinding> DependencyBindings { get; }
}
