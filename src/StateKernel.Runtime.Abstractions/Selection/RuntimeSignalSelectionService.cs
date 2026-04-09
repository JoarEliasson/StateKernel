using StateKernel.Runtime.Abstractions.Composition;

namespace StateKernel.Runtime.Abstractions.Selection;

/// <summary>
/// Creates deterministic runtime-facing signal selections from approved simulation-side exposure choices.
/// </summary>
/// <remarks>
/// This service transforms validated upstream approval into runtime-facing selection inputs only. It
/// does not apply runtime composition defaults, validate effective runtime node identity collisions,
/// or inspect scheduler plans.
/// </remarks>
public static class RuntimeSignalSelectionService
{
    /// <summary>
    /// Creates deterministic runtime-facing signal selections from the supplied request.
    /// </summary>
    /// <param name="request">The runtime signal-selection request to transform.</param>
    /// <returns>The ordered runtime-facing signal selections.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="request" /> is null.
    /// </exception>
    public static RuntimeSignalSelectionResult CreateSelections(RuntimeSignalSelectionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var signalSelections = request.ExposureChoices
            .OrderBy(static choice => choice.SourceSignalId.Value, StringComparer.Ordinal)
            .Select(static choice =>
                new RuntimeSignalSelection(
                    choice.SourceSignalId,
                    choice.TargetNodeIdOverride,
                    choice.DisplayNameOverride))
            .ToArray();

        return new RuntimeSignalSelectionResult(signalSelections);
    }
}
