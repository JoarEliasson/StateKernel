using StateKernel.Runtime.Abstractions.Composition;
using StateKernel.Runtime.Abstractions.Selection;
using StateKernel.Simulation.Signals;

namespace StateKernel.Runtime.Abstractions.Tests;

public sealed class RuntimeSelectionTests
{
    private static readonly SimulationSignalId AlphaSignal = SimulationSignalId.From("Alpha");
    private static readonly SimulationSignalId BravoSignal = SimulationSignalId.From("Bravo");
    private static readonly SimulationSignalId CharlieSignal = SimulationSignalId.From("Charlie");

    [Fact]
    public void SimulationSignalExposureChoices_RejectWhitespaceDisplayNameOverrides()
    {
        Assert.Throws<ArgumentException>(() =>
            new SimulationSignalExposureChoice(AlphaSignal, displayNameOverride: "   "));
    }

    [Fact]
    public void SimulationSignalExposureChoices_TrimDisplayNameOverridesAndPreserveExplicitNodeIds()
    {
        var explicitNodeId = RuntimeNodeId.From("Runtime/Bravo");
        var choice = new SimulationSignalExposureChoice(
            BravoSignal,
            explicitNodeId,
            "  Bravo Display  ");

        Assert.Same(explicitNodeId, choice.TargetNodeIdOverride);
        Assert.Equal("Bravo Display", choice.DisplayNameOverride);
    }

    [Fact]
    public void RuntimeSignalSelectionRequests_RejectNullExposureChoices()
    {
        var exposureChoices = new SimulationSignalExposureChoice?[]
        {
            new(AlphaSignal),
            null,
        };

        Assert.Throws<InvalidOperationException>(() =>
            new RuntimeSignalSelectionRequest(exposureChoices!));
    }

    [Fact]
    public void RuntimeSignalSelectionRequests_RejectDuplicateSelectedSignals()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            new RuntimeSignalSelectionRequest(
            [
                new SimulationSignalExposureChoice(AlphaSignal),
                new SimulationSignalExposureChoice(AlphaSignal, RuntimeNodeId.From("Runtime/Alpha")),
            ]));

        Assert.Contains(AlphaSignal.Value, exception.Message);
    }

    [Fact]
    public void RuntimeSignalSelectionServices_OrderSelectionsBySignalIdRegardlessOfInputOrderOrOverrides()
    {
        var result = RuntimeSignalSelectionService.CreateSelections(
            new RuntimeSignalSelectionRequest(
            [
                new SimulationSignalExposureChoice(CharlieSignal, displayNameOverride: "Charlie Display"),
                new SimulationSignalExposureChoice(BravoSignal, RuntimeNodeId.From("Runtime/Bravo")),
                new SimulationSignalExposureChoice(AlphaSignal),
            ]));

        Assert.Collection(
            result.SignalSelections,
            selection =>
            {
                Assert.Equal(AlphaSignal, selection.SourceSignalId);
                Assert.Null(selection.TargetNodeId);
                Assert.Null(selection.DisplayNameOverride);
            },
            selection =>
            {
                Assert.Equal(BravoSignal, selection.SourceSignalId);
                Assert.Equal("Runtime/Bravo", selection.TargetNodeId!.Value);
                Assert.Null(selection.DisplayNameOverride);
            },
            selection =>
            {
                Assert.Equal(CharlieSignal, selection.SourceSignalId);
                Assert.Null(selection.TargetNodeId);
                Assert.Equal("Charlie Display", selection.DisplayNameOverride);
            });
    }

    [Fact]
    public void RuntimeSignalSelectionServices_ProduceEquivalentSelectionsForEquivalentRequests()
    {
        var firstResult = RuntimeSignalSelectionService.CreateSelections(
            new RuntimeSignalSelectionRequest(
            [
                new SimulationSignalExposureChoice(CharlieSignal, displayNameOverride: "Charlie"),
                new SimulationSignalExposureChoice(AlphaSignal),
                new SimulationSignalExposureChoice(BravoSignal, RuntimeNodeId.From("Runtime/Bravo")),
            ]));
        var secondResult = RuntimeSignalSelectionService.CreateSelections(
            new RuntimeSignalSelectionRequest(
            [
                new SimulationSignalExposureChoice(BravoSignal, RuntimeNodeId.From("Runtime/Bravo")),
                new SimulationSignalExposureChoice(CharlieSignal, displayNameOverride: "Charlie"),
                new SimulationSignalExposureChoice(AlphaSignal),
            ]));

        Assert.Equal(
            firstResult.SignalSelections.Select(static selection => (selection.SourceSignalId.Value, NodeId: selection.TargetNodeId?.Value, selection.DisplayNameOverride)),
            secondResult.SignalSelections.Select(static selection => (selection.SourceSignalId.Value, NodeId: selection.TargetNodeId?.Value, selection.DisplayNameOverride)));
    }

    [Fact]
    public void RuntimeSignalSelectionResults_FeedDirectlyIntoRuntimeCompositionRequests()
    {
        var selectionResult = RuntimeSignalSelectionService.CreateSelections(
            new RuntimeSignalSelectionRequest(
            [
                new SimulationSignalExposureChoice(BravoSignal, RuntimeNodeId.From("Runtime/Bravo"), "Bravo Display"),
                new SimulationSignalExposureChoice(AlphaSignal),
            ]));
        var compositionResult = RuntimeCompositionService.Compose(
            new RuntimeCompositionRequest(
                "ua-net",
                selectionResult.SignalSelections,
                RuntimeCompositionDefaults.Baseline));

        Assert.Collection(
            compositionResult.ProjectionPlan.Projections,
            projection =>
            {
                Assert.Equal(AlphaSignal, projection.SourceSignalId);
                Assert.Equal("Signals/Alpha", projection.TargetNodeId.Value);
                Assert.Equal("Alpha", projection.DisplayName);
            },
            projection =>
            {
                Assert.Equal(BravoSignal, projection.SourceSignalId);
                Assert.Equal("Runtime/Bravo", projection.TargetNodeId.Value);
                Assert.Equal("Bravo Display", projection.DisplayName);
            });
    }

    [Fact]
    public void DirectRuntimeCompositionRequests_RemainSupportedWithoutSelectionService()
    {
        var compositionResult = RuntimeCompositionService.Compose(
            new RuntimeCompositionRequest(
                "ua-net",
                [
                    new RuntimeSignalSelection(AlphaSignal),
                ],
                RuntimeCompositionDefaults.Baseline));

        var projection = Assert.Single(compositionResult.ProjectionPlan.Projections);

        Assert.Equal(AlphaSignal, projection.SourceSignalId);
        Assert.Equal("Signals/Alpha", projection.TargetNodeId.Value);
    }
}
