namespace StateKernel.Runtime.Abstractions.Composition;

/// <summary>
/// Captures the validated inputs required to compose deterministic runtime exposure artifacts.
/// </summary>
public sealed class RuntimeCompositionRequest
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RuntimeCompositionRequest" /> type.
    /// </summary>
    /// <param name="adapterKey">The stable adapter key carried through composition.</param>
    /// <param name="signalSelections">The runtime-facing signal selections to compose.</param>
    /// <param name="defaults">The baseline composition defaults to apply.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="adapterKey" /> is null, empty, or whitespace.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="signalSelections" /> or <paramref name="defaults" /> is null.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <paramref name="signalSelections" /> contains null entries or duplicate selected
    /// source signal identifiers.
    /// </exception>
    public RuntimeCompositionRequest(
        string adapterKey,
        IEnumerable<RuntimeSignalSelection> signalSelections,
        RuntimeCompositionDefaults defaults)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(adapterKey);
        ArgumentNullException.ThrowIfNull(signalSelections);
        ArgumentNullException.ThrowIfNull(defaults);

        var materializedSelections = signalSelections.ToArray();

        if (Array.Exists(materializedSelections, static selection => selection is null))
        {
            throw new InvalidOperationException(
                "Runtime composition requests cannot contain null signal selections.");
        }

        var duplicateSignalId = materializedSelections
            .GroupBy(static selection => selection.SourceSignalId)
            .FirstOrDefault(static group => group.Count() > 1)?
            .Key;

        if (duplicateSignalId is not null)
        {
            throw new InvalidOperationException(
                $"Runtime composition requests must use unique selected source signal identifiers. Duplicate signal id: '{duplicateSignalId}'.");
        }

        AdapterKey = adapterKey.Trim();
        SignalSelections = Array.AsReadOnly(materializedSelections);
        Defaults = defaults;
    }

    /// <summary>
    /// Gets the stable adapter key carried through composition.
    /// </summary>
    public string AdapterKey { get; }

    /// <summary>
    /// Gets the runtime-facing signal selections to compose.
    /// </summary>
    public IReadOnlyList<RuntimeSignalSelection> SignalSelections { get; }

    /// <summary>
    /// Gets the baseline composition defaults to apply.
    /// </summary>
    public RuntimeCompositionDefaults Defaults { get; }
}
