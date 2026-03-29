using StateKernel.Simulation.Behaviors;
using StateKernel.Simulation.Clock;
using StateKernel.Simulation.Signals;

namespace StateKernel.Simulation.Tests;

public sealed class DerivedSignalBehaviorTests
{
    private static readonly SimulationSignalId SourceSignal = SimulationSignalId.From("Source");

    [Fact]
    public void PassThroughFromSignalBehavior_ReturnsTheCommittedUpstreamValue()
    {
        var behavior = new PassThroughFromSignalBehavior(SourceSignal);

        var sample = behavior.Evaluate(CreateContextWithCommittedSignal(SourceSignal, 5.0));

        Assert.Equal(5.0, sample.Value);
    }

    [Fact]
    public void OffsetFromSignalBehavior_AddsTheConfiguredOffsetToTheCommittedUpstreamValue()
    {
        var behavior = new OffsetFromSignalBehavior(SourceSignal, 1.5);

        var sample = behavior.Evaluate(CreateContextWithCommittedSignal(SourceSignal, 5.0));

        Assert.Equal(6.5, sample.Value);
    }

    [Fact]
    public void DerivedSignalBehaviors_RejectInvalidConfiguration()
    {
        Assert.Throws<ArgumentNullException>(() => new PassThroughFromSignalBehavior(null!));
        Assert.Throws<ArgumentNullException>(() => new OffsetFromSignalBehavior(null!, 1.0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new OffsetFromSignalBehavior(SourceSignal, double.NaN));
    }

    [Fact]
    public void DerivedSignalBehaviors_FailClearlyWhenARequiredCommittedSignalIsUnavailable()
    {
        var behavior = new PassThroughFromSignalBehavior(SourceSignal);
        var tick = new SimulationTick(1, TimeSpan.FromMilliseconds(10));
        var context = new BehaviorExecutionContext(tick, SimulationSignalSnapshot.Empty);

        var exception = Assert.Throws<InvalidOperationException>(() => behavior.Evaluate(context));

        Assert.Contains(SourceSignal.Value, exception.Message);
    }

    private static BehaviorExecutionContext CreateContextWithCommittedSignal(
        SimulationSignalId signalId,
        double value)
    {
        var store = new SimulationSignalValueStore();
        var producingTick = new SimulationTick(1, TimeSpan.FromMilliseconds(10));
        var readingTick = new SimulationTick(2, TimeSpan.FromMilliseconds(20));

        store.GetCommittedSnapshotForTick(producingTick);
        store.RecordProducedValue(
            new SimulationSignalValue(
                signalId,
                producingTick,
                new BehaviorSample(value)));

        return new BehaviorExecutionContext(
            readingTick,
            store.GetCommittedSnapshotForTick(readingTick));
    }
}
