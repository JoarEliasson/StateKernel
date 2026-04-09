using StateKernel.Runtime.Abstractions.Composition;

namespace StateKernel.Runtime.Abstractions.Selection;

/// <summary>
/// Represents the deterministic runtime-facing signal selections derived from approved exposure choices.
/// </summary>
/// <remarks>
/// This is an inspection-friendly artifact only. It exists to expose the ordered
/// <see cref="RuntimeSignalSelection" /> values produced by the selection seam without becoming a
/// diagnostics, composition, or orchestration object.
/// </remarks>
public sealed class RuntimeSignalSelectionResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RuntimeSignalSelectionResult" /> type.
    /// </summary>
    /// <param name="signalSelections">The runtime-facing signal selections.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="signalSelections" /> is null.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <paramref name="signalSelections" /> contains null entries.
    /// </exception>
    public RuntimeSignalSelectionResult(IEnumerable<RuntimeSignalSelection> signalSelections)
    {
        ArgumentNullException.ThrowIfNull(signalSelections);

        var materializedSelections = signalSelections.ToArray();

        if (Array.Exists(materializedSelections, static selection => selection is null))
        {
            throw new InvalidOperationException(
                "Runtime signal-selection results cannot contain null runtime-facing selections.");
        }

        SignalSelections = Array.AsReadOnly(
            materializedSelections
                .OrderBy(static selection => selection.SourceSignalId.Value, StringComparer.Ordinal)
                .ToArray());
    }

    /// <summary>
    /// Gets the deterministically ordered runtime-facing signal selections.
    /// </summary>
    public IReadOnlyList<RuntimeSignalSelection> SignalSelections { get; }
}
