namespace StateKernel.Simulation.Signals.Dependencies.Diagnostics;

/// <summary>
/// Represents the immutable advisory diagnostics report for one analyzed dependency plan.
/// </summary>
/// <remarks>
/// Reports are inspection-oriented artifacts only. They do not drive execution or alter runtime
/// behavior.
/// </remarks>
public sealed class SimulationDependencyDiagnosticsReport
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SimulationDependencyDiagnosticsReport" /> type.
    /// </summary>
    /// <param name="diagnostics">The ordered diagnostics produced by the analyzer.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="diagnostics" /> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="diagnostics" /> contains a null entry.
    /// </exception>
    public SimulationDependencyDiagnosticsReport(IEnumerable<SimulationDependencyDiagnostic> diagnostics)
    {
        ArgumentNullException.ThrowIfNull(diagnostics);

        var materializedDiagnostics = diagnostics.ToArray();

        if (Array.Exists(materializedDiagnostics, static diagnostic => diagnostic is null))
        {
            throw new ArgumentException(
                "Dependency diagnostics reports cannot contain null diagnostics.",
                nameof(diagnostics));
        }

        Diagnostics = Array.AsReadOnly(materializedDiagnostics);
    }

    /// <summary>
    /// Gets the ordered diagnostics produced by the analyzer.
    /// </summary>
    public IReadOnlyList<SimulationDependencyDiagnostic> Diagnostics { get; }

    /// <summary>
    /// Gets a value indicating whether the report contains any diagnostics.
    /// </summary>
    public bool HasDiagnostics => Diagnostics.Count > 0;
}
