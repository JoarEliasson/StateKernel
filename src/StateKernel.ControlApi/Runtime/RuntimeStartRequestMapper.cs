using StateKernel.ControlApi.Contracts.Runtime;
using StateKernel.Runtime.Abstractions;
using StateKernel.Runtime.Abstractions.Composition;
using StateKernel.Runtime.Abstractions.Selection;
using StateKernel.Simulation.Signals;
using DomainRuntimeStartRequest = StateKernel.Runtime.Abstractions.RuntimeStartRequest;

namespace StateKernel.ControlApi.Runtime;

/// <summary>
/// Provides the bounded mechanical mapping from control API inputs to runtime start artifacts.
/// </summary>
internal static class RuntimeStartRequestMapper
{
    /// <summary>
    /// Maps bounded control API exposure choices into approved simulation-side exposure choices.
    /// </summary>
    /// <param name="exposureChoices">The bounded control API exposure choices to map.</param>
    /// <param name="argumentName">The argument name used in thrown exceptions.</param>
    /// <param name="requireNonEmpty">Indicates whether an empty set is invalid.</param>
    /// <returns>The mapped approved simulation-side exposure choices.</returns>
    public static SimulationSignalExposureChoice[] MapExposureChoices(
        IReadOnlyList<StartRuntimeSignalExposureChoiceRequest>? exposureChoices,
        string argumentName,
        bool requireNonEmpty)
    {
        ArgumentNullException.ThrowIfNull(exposureChoices);

        if (exposureChoices.Any(static choice => choice is null))
        {
            throw new ArgumentException(
                "Control API runtime exposure choices cannot contain null entries.",
                argumentName);
        }

        if (requireNonEmpty && exposureChoices.Count == 0)
        {
            throw new ArgumentException(
                "Control API run start requests must include at least one exposure choice.",
                argumentName);
        }

        return exposureChoices
            .Select(static choice =>
                new SimulationSignalExposureChoice(
                    SimulationSignalId.From(choice.SourceSignalId!),
                    choice.TargetNodeIdOverride is null
                        ? null
                        : RuntimeNodeId.From(choice.TargetNodeIdOverride),
                    choice.DisplayNameOverride))
            .ToArray();
    }

    /// <summary>
    /// Builds the validated runtime start request from already-mapped exposure choices.
    /// </summary>
    /// <param name="adapterKey">The adapter key to start.</param>
    /// <param name="profileId">The bounded runtime profile identifier.</param>
    /// <param name="endpointHost">The endpoint host name or address.</param>
    /// <param name="endpointPort">The endpoint port.</param>
    /// <param name="nodeIdPrefix">The optional runtime node-id prefix override.</param>
    /// <param name="exposureChoices">The already-mapped approved exposure choices.</param>
    /// <returns>The validated runtime start request.</returns>
    public static DomainRuntimeStartRequest BuildRuntimeStartRequest(
        string adapterKey,
        string profileId,
        string endpointHost,
        int endpointPort,
        string? nodeIdPrefix,
        IReadOnlyList<SimulationSignalExposureChoice> exposureChoices)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(adapterKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(profileId);
        ArgumentException.ThrowIfNullOrWhiteSpace(endpointHost);
        ArgumentNullException.ThrowIfNull(exposureChoices);

        var endpointProfile = RuntimeEndpointProfiles.GetRequired(
            RuntimeEndpointProfileId.From(profileId));
        var signalSelections = RuntimeSignalSelectionService.CreateSelections(
            new RuntimeSignalSelectionRequest(exposureChoices));
        var compositionDefaults = nodeIdPrefix is null
            ? RuntimeCompositionDefaults.Baseline
            : new RuntimeCompositionDefaults(nodeIdPrefix);
        var compositionResult = RuntimeCompositionService.Compose(
            new RuntimeCompositionRequest(
                adapterKey,
                signalSelections.SignalSelections,
                compositionDefaults));

        return new DomainRuntimeStartRequest(
            adapterKey,
            compositionResult.CompiledRuntimePlan,
            new RuntimeEndpointSettings(endpointHost, endpointPort),
            endpointProfile);
    }
}
