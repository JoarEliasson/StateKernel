using StateKernel.Runtime.Abstractions.Composition;
using StateKernel.Simulation.Signals;

namespace StateKernel.Runtime.Abstractions.Tests;

public sealed class RuntimeCompositionTests
{
    private static readonly SimulationSignalId AlphaSignal = SimulationSignalId.From("Alpha");
    private static readonly SimulationSignalId BravoSignal = SimulationSignalId.From("Bravo");

    [Fact]
    public void RuntimeSignalSelections_RejectWhitespaceDisplayNameOverrides()
    {
        Assert.Throws<ArgumentException>(() =>
            new RuntimeSignalSelection(AlphaSignal, displayNameOverride: "   "));
    }

    [Fact]
    public void RuntimeCompositionDefaults_BaselineUsesSignalsPrefix()
    {
        Assert.Equal("Signals", RuntimeCompositionDefaults.Baseline.NodeIdPrefix);
    }

    [Fact]
    public void RuntimeCompositionDefaults_NormalizePrefixesByTrimmingWhitespaceAndSlashes()
    {
        var defaults = new RuntimeCompositionDefaults(" /Signals/ ");
        var result = RuntimeCompositionService.Compose(
            new RuntimeCompositionRequest(
                "ua-net",
                [
                    new RuntimeSignalSelection(AlphaSignal),
                ],
                defaults));

        Assert.Equal("Signals", defaults.NodeIdPrefix);
        Assert.Equal("Signals/Alpha", result.ProjectionPlan.Projections[0].TargetNodeId.Value);
    }

    [Fact]
    public void RuntimeCompositionRequests_RejectDuplicateSelectedSignals()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            new RuntimeCompositionRequest(
                "ua-net",
                [
                    new RuntimeSignalSelection(AlphaSignal),
                    new RuntimeSignalSelection(AlphaSignal, RuntimeNodeId.From("Signals/Alpha-Override")),
                ],
                RuntimeCompositionDefaults.Baseline));

        Assert.Contains(AlphaSignal.Value, exception.Message);
    }

    [Fact]
    public void RuntimeCompositionServices_ApplyDefaultsForNodeIdsAndDisplayNames()
    {
        var result = RuntimeCompositionService.Compose(
            new RuntimeCompositionRequest(
                "ua-net",
                [
                    new RuntimeSignalSelection(AlphaSignal),
                ],
                RuntimeCompositionDefaults.Baseline));

        var projection = Assert.Single(result.ProjectionPlan.Projections);

        Assert.Equal("ua-net", result.AdapterKey);
        Assert.Equal(AlphaSignal, projection.SourceSignalId);
        Assert.Equal("Signals/Alpha", projection.TargetNodeId.Value);
        Assert.Equal("Alpha", projection.DisplayName);
    }

    [Fact]
    public void RuntimeCompositionServices_TrimDisplayNameOverridesAndPreserveExplicitNodeIds()
    {
        var explicitNodeId = RuntimeNodeId.From("Runtime/Bravo");
        var result = RuntimeCompositionService.Compose(
            new RuntimeCompositionRequest(
                "ua-net",
                [
                    new RuntimeSignalSelection(BravoSignal, explicitNodeId, "  Bravo Display  "),
                ],
                RuntimeCompositionDefaults.Baseline));

        var projection = Assert.Single(result.ProjectionPlan.Projections);

        Assert.Same(explicitNodeId, projection.TargetNodeId);
        Assert.Equal("Bravo Display", projection.DisplayName);
    }

    [Fact]
    public void RuntimeCompositionServices_RejectDuplicateEffectiveNodeIdsAfterApplyingDefaults()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            RuntimeCompositionService.Compose(
                new RuntimeCompositionRequest(
                    "ua-net",
                    [
                        new RuntimeSignalSelection(AlphaSignal, RuntimeNodeId.From("Signals/Shared")),
                        new RuntimeSignalSelection(BravoSignal, RuntimeNodeId.From("Signals/Shared")),
                    ],
                    RuntimeCompositionDefaults.Baseline)));

        Assert.Contains("Signals/Shared", exception.Message);
    }

    [Fact]
    public void RuntimeCompositionServices_OrderArtifactsBySignalIdRegardlessOfInputOrderAndOverrides()
    {
        var result = RuntimeCompositionService.Compose(
            new RuntimeCompositionRequest(
                "ua-net",
                [
                    new RuntimeSignalSelection(BravoSignal, RuntimeNodeId.From("AAA/CustomBravo")),
                    new RuntimeSignalSelection(AlphaSignal),
                ],
                RuntimeCompositionDefaults.Baseline));

        Assert.Collection(
            result.ProjectionPlan.Projections,
            projection =>
            {
                Assert.Equal(AlphaSignal, projection.SourceSignalId);
                Assert.Equal("Signals/Alpha", projection.TargetNodeId.Value);
            },
            projection =>
            {
                Assert.Equal(BravoSignal, projection.SourceSignalId);
                Assert.Equal("AAA/CustomBravo", projection.TargetNodeId.Value);
            });

        Assert.Collection(
            result.CompiledRuntimePlan.Bindings,
            binding => Assert.Equal(AlphaSignal, binding.SourceSignalId),
            binding => Assert.Equal(BravoSignal, binding.SourceSignalId));
    }

    [Fact]
    public void RuntimeCompositionServices_ProduceEquivalentArtifactsForEquivalentRequests()
    {
        var firstResult = RuntimeCompositionService.Compose(
            new RuntimeCompositionRequest(
                "ua-net",
                [
                    new RuntimeSignalSelection(BravoSignal, RuntimeNodeId.From("Runtime/Bravo"), "Bravo"),
                    new RuntimeSignalSelection(AlphaSignal),
                ],
                RuntimeCompositionDefaults.Baseline));
        var secondResult = RuntimeCompositionService.Compose(
            new RuntimeCompositionRequest(
                "ua-net",
                [
                    new RuntimeSignalSelection(AlphaSignal),
                    new RuntimeSignalSelection(BravoSignal, RuntimeNodeId.From("Runtime/Bravo"), "Bravo"),
                ],
                RuntimeCompositionDefaults.Baseline));

        Assert.Equal(
            firstResult.ProjectionPlan.Projections.Select(static projection => (projection.SourceSignalId.Value, projection.TargetNodeId.Value, projection.DisplayName)),
            secondResult.ProjectionPlan.Projections.Select(static projection => (projection.SourceSignalId.Value, projection.TargetNodeId.Value, projection.DisplayName)));
        Assert.Equal(
            firstResult.CompiledRuntimePlan.Bindings.Select(static binding => (binding.SourceSignalId.Value, binding.TargetNodeId.Value, binding.DisplayName)),
            secondResult.CompiledRuntimePlan.Bindings.Select(static binding => (binding.SourceSignalId.Value, binding.TargetNodeId.Value, binding.DisplayName)));
    }

    [Fact]
    public void DirectLowerLevelRuntimePlanConstruction_RemainsSupportedAlongsideComposition()
    {
        var projectionPlan = new RuntimeProjectionPlan(
        [
            new SimulationSignalProjection(AlphaSignal, RuntimeNodeId.From("Signals/Alpha"), "Alpha"),
        ]);
        var compiledRuntimePlan = new CompiledRuntimePlan(projectionPlan);

        var binding = Assert.Single(compiledRuntimePlan.Bindings);

        Assert.Equal(AlphaSignal, binding.SourceSignalId);
        Assert.Equal("Signals/Alpha", binding.TargetNodeId.Value);
    }
}
