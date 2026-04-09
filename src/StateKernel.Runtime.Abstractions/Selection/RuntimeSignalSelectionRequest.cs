namespace StateKernel.Runtime.Abstractions.Selection;

/// <summary>
/// Captures the validated approved simulation-side exposure choices used to derive runtime-facing selections.
/// </summary>
/// <remarks>
/// Input order is not semantically significant in this request. The selection service imposes
/// canonical signal-id ordering when it produces runtime-facing selections.
/// </remarks>
public sealed class RuntimeSignalSelectionRequest
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RuntimeSignalSelectionRequest" /> type.
    /// </summary>
    /// <param name="exposureChoices">The approved simulation-side exposure choices.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="exposureChoices" /> is null.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <paramref name="exposureChoices" /> contains null entries or duplicate selected
    /// source signal identifiers.
    /// </exception>
    public RuntimeSignalSelectionRequest(IEnumerable<SimulationSignalExposureChoice> exposureChoices)
    {
        ArgumentNullException.ThrowIfNull(exposureChoices);

        var materializedChoices = exposureChoices.ToArray();

        if (Array.Exists(materializedChoices, static choice => choice is null))
        {
            throw new InvalidOperationException(
                "Runtime signal-selection requests cannot contain null exposure choices.");
        }

        var duplicateSignalId = materializedChoices
            .GroupBy(static choice => choice.SourceSignalId)
            .FirstOrDefault(static group => group.Count() > 1)?
            .Key;

        if (duplicateSignalId is not null)
        {
            throw new InvalidOperationException(
                $"Runtime signal-selection requests must use unique selected source signal identifiers. Duplicate signal id: '{duplicateSignalId}'.");
        }

        ExposureChoices = Array.AsReadOnly(materializedChoices);
    }

    /// <summary>
    /// Gets the approved simulation-side exposure choices.
    /// </summary>
    public IReadOnlyCollection<SimulationSignalExposureChoice> ExposureChoices { get; }
}
