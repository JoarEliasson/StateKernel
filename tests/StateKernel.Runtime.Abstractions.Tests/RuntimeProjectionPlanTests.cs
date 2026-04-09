using StateKernel.Runtime.Abstractions;
using StateKernel.Simulation.Signals;

namespace StateKernel.Runtime.Abstractions.Tests;

public sealed class RuntimeProjectionPlanTests
{
    private static readonly SimulationSignalId SourceSignal = SimulationSignalId.From("Source");
    private static readonly SimulationSignalId SecondarySignal = SimulationSignalId.From("Secondary");

    [Fact]
    public void RuntimeNodeId_FromRejectsNullEmptyAndWhitespace()
    {
        Assert.ThrowsAny<ArgumentException>(() => RuntimeNodeId.From(null!));
        Assert.ThrowsAny<ArgumentException>(() => RuntimeNodeId.From(string.Empty));
        Assert.ThrowsAny<ArgumentException>(() => RuntimeNodeId.From("   "));
    }

    [Fact]
    public void RuntimeNodeId_ForSignalUsesTheBaselineSignalsPath()
    {
        var nodeId = RuntimeNodeId.ForSignal(SourceSignal);

        Assert.Equal("Signals/Source", nodeId.Value);
    }

    [Fact]
    public void RuntimeProjectionPlans_RejectDuplicateSourceSignals()
    {
        var duplicateSignalId = Assert.Throws<InvalidOperationException>(() =>
            new RuntimeProjectionPlan(
            [
                new SimulationSignalProjection(SourceSignal, RuntimeNodeId.From("Signals/A")),
                new SimulationSignalProjection(SourceSignal, RuntimeNodeId.From("Signals/B")),
            ]));

        Assert.Contains(SourceSignal.Value, duplicateSignalId.Message);
    }

    [Fact]
    public void RuntimeProjectionPlans_RejectDuplicateTargetNodeIds()
    {
        var duplicateNodeId = Assert.Throws<InvalidOperationException>(() =>
            new RuntimeProjectionPlan(
            [
                new SimulationSignalProjection(SourceSignal, RuntimeNodeId.From("Signals/Shared")),
                new SimulationSignalProjection(SecondarySignal, RuntimeNodeId.From("Signals/Shared")),
            ]));

        Assert.Contains("Signals/Shared", duplicateNodeId.Message);
    }

    [Fact]
    public void RuntimeProjectionPlans_OrderProjectionsDeterministicallyBySignalId()
    {
        var plan = new RuntimeProjectionPlan(
        [
            new SimulationSignalProjection(SecondarySignal, RuntimeNodeId.From("Signals/Secondary")),
            new SimulationSignalProjection(SourceSignal, RuntimeNodeId.From("Signals/Source")),
        ]);

        Assert.Collection(
            plan.Projections,
            projection => Assert.Equal(SecondarySignal, projection.SourceSignalId),
            projection => Assert.Equal(SourceSignal, projection.SourceSignalId));
    }

    [Fact]
    public void CompiledRuntimePlans_ExposeDeterministicBindingsFromProjectionPlans()
    {
        var compiledPlan = new CompiledRuntimePlan(
            new RuntimeProjectionPlan(
            [
                new SimulationSignalProjection(SecondarySignal, RuntimeNodeId.From("Signals/Secondary"), "Secondary Display"),
                new SimulationSignalProjection(SourceSignal, RuntimeNodeId.From("Signals/Source"), "Source Display"),
            ]));

        Assert.Collection(
            compiledPlan.Bindings,
            binding =>
            {
                Assert.Equal(SecondarySignal, binding.SourceSignalId);
                Assert.Equal("Signals/Secondary", binding.TargetNodeId.Value);
                Assert.Equal("Secondary Display", binding.DisplayName);
            },
            binding =>
            {
                Assert.Equal(SourceSignal, binding.SourceSignalId);
                Assert.Equal("Signals/Source", binding.TargetNodeId.Value);
                Assert.Equal("Source Display", binding.DisplayName);
            });
    }

    [Fact]
    public void RuntimeValueUpdates_RejectNonFiniteValuesAndNegativeTicks()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new RuntimeValueUpdate(SourceSignal, double.NaN, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new RuntimeValueUpdate(SourceSignal, 1.0, -1));
    }

    [Fact]
    public void RuntimeAdapterDescriptors_RejectInvalidInputs()
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            new RuntimeAdapterDescriptor(string.Empty, "UA", RuntimeCapabilitySet.Empty, [RuntimeEndpointProfiles.LocalDevelopment.Id]));
        Assert.ThrowsAny<ArgumentException>(() =>
            new RuntimeAdapterDescriptor("ua-net", " ", RuntimeCapabilitySet.Empty, [RuntimeEndpointProfiles.LocalDevelopment.Id]));
        Assert.Throws<ArgumentNullException>(() =>
            new RuntimeAdapterDescriptor("ua-net", "UA", null!, [RuntimeEndpointProfiles.LocalDevelopment.Id]));
        Assert.Throws<ArgumentNullException>(() =>
            new RuntimeAdapterDescriptor("ua-net", "UA", RuntimeCapabilitySet.Empty, null!));
    }
}
