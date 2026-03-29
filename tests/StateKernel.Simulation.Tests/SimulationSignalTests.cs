using StateKernel.Simulation.Activation;
using StateKernel.Simulation.Behaviors;
using StateKernel.Simulation.Clock;
using StateKernel.Simulation.Context;
using StateKernel.Simulation.Exceptions;
using StateKernel.Simulation.Modes;
using StateKernel.Simulation.Scheduling;
using StateKernel.Simulation.Signals;

namespace StateKernel.Simulation.Tests;

public sealed class SimulationSignalTests
{
    private static readonly SimulationSignalId SourceSignal = SimulationSignalId.From("Source");
    private static readonly SimulationSignalId SecondarySignal = SimulationSignalId.From("Secondary");
    private static readonly SimulationMode RunMode = SimulationMode.From("Run");

    [Fact]
    public void SimulationSignalId_FromRejectsNullEmptyAndWhitespace()
    {
        Assert.ThrowsAny<ArgumentException>(() => SimulationSignalId.From(null!));
        Assert.ThrowsAny<ArgumentException>(() => SimulationSignalId.From(string.Empty));
        Assert.ThrowsAny<ArgumentException>(() => SimulationSignalId.From("   "));
    }

    [Fact]
    public void SimulationSignalId_FromTrimsInputAndUsesOrdinalEquality()
    {
        var left = SimulationSignalId.From("  Source  ");
        var right = SimulationSignalId.From("Source");
        var differentCase = SimulationSignalId.From("source");

        Assert.Equal("Source", left.Value);
        Assert.Equal(left, right);
        Assert.NotEqual(left, differentCase);
    }

    [Fact]
    public void EmptySnapshots_DoNotContainCommittedValues()
    {
        var snapshot = SimulationSignalSnapshot.Empty;

        Assert.Equal(0, snapshot.Count);
        Assert.False(snapshot.TryGetValue(SourceSignal, out _));

        var exception = Assert.Throws<InvalidOperationException>(() => snapshot.GetRequiredValue(SourceSignal));

        Assert.Contains(SourceSignal.Value, exception.Message);
    }

    [Fact]
    public void SignalValueStore_SameTickSnapshotAccessIsIdempotentAndDoesNotRevealSameTickWrites()
    {
        var store = new SimulationSignalValueStore();
        var tick = CreateTick(1);

        var firstSnapshot = store.GetCommittedSnapshotForTick(tick);
        store.RecordProducedValue(new SimulationSignalValue(SourceSignal, tick, new BehaviorSample(5.0)));
        var secondSnapshot = store.GetCommittedSnapshotForTick(tick);

        Assert.Same(firstSnapshot, secondSnapshot);
        Assert.False(secondSnapshot.TryGetValue(SourceSignal, out _));
    }

    [Fact]
    public void SignalValueStore_HoldsTheLastCommittedSignalValueAcrossNonProducingTicks()
    {
        var store = new SimulationSignalValueStore();
        var firstTick = CreateTick(1);
        var secondTick = CreateTick(2);
        var thirdTick = CreateTick(3);

        store.GetCommittedSnapshotForTick(firstTick);
        store.RecordProducedValue(new SimulationSignalValue(SourceSignal, firstTick, new BehaviorSample(5.0)));

        var secondTickSnapshot = store.GetCommittedSnapshotForTick(secondTick);
        var thirdTickSnapshot = store.GetCommittedSnapshotForTick(thirdTick);

        Assert.Equal(5.0, secondTickSnapshot.GetRequiredValue(SourceSignal).Sample.Value);
        Assert.Same(secondTickSnapshot, thirdTickSnapshot);
        Assert.Equal(5.0, thirdTickSnapshot.GetRequiredValue(SourceSignal).Sample.Value);
    }

    [Fact]
    public void SignalValueStore_MakesMultipleSignalsFromOneTickVisibleTogetherOnTheNextTick()
    {
        var store = new SimulationSignalValueStore();
        var firstTick = CreateTick(1);
        var secondTick = CreateTick(2);

        store.GetCommittedSnapshotForTick(firstTick);
        store.RecordProducedValue(new SimulationSignalValue(SourceSignal, firstTick, new BehaviorSample(5.0)));
        store.RecordProducedValue(new SimulationSignalValue(SecondarySignal, firstTick, new BehaviorSample(9.0)));

        var committedSnapshot = store.GetCommittedSnapshotForTick(secondTick);

        Assert.Equal(2, committedSnapshot.Count);
        Assert.Equal(5.0, committedSnapshot.GetRequiredValue(SourceSignal).Sample.Value);
        Assert.Equal(9.0, committedSnapshot.GetRequiredValue(SecondarySignal).Sample.Value);
    }

    [Fact]
    public void SchedulerPlans_RejectDuplicateProducedSignalIdentifiersEvenWhenWorkKeysDiffer()
    {
        var recorder = new BehaviorExecutionRecorder();
        var modeController = new SimulationModeController(RunMode);
        var signalStore = new SimulationSignalValueStore();

        var exception = Assert.Throws<SimulationConfigurationException>(() =>
            new SimulationSchedulerPlan(
                [
                    new BehaviorScheduledWork(
                        "source-a",
                        ExecutionCadence.EveryTick,
                        0,
                        new ConstantBehavior(5.0),
                        new AlwaysActivePolicy(),
                        modeController,
                        signalStore,
                        recorder,
                        SourceSignal),
                    new BehaviorScheduledWork(
                        "source-b",
                        ExecutionCadence.EveryTick,
                        10,
                        new ConstantBehavior(9.0),
                        new AlwaysActivePolicy(),
                        modeController,
                        signalStore,
                        recorder,
                        SourceSignal),
                ]));

        Assert.Contains("Duplicate signal id", exception.Message);
    }

    private static SimulationTick CreateTick(long sequenceNumber)
    {
        return new SimulationTick(sequenceNumber, TimeSpan.FromMilliseconds(sequenceNumber * 10));
    }
}
