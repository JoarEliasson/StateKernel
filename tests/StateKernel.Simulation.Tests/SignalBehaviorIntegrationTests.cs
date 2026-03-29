using StateKernel.Simulation.Activation;
using StateKernel.Simulation.Behaviors;
using StateKernel.Simulation.Clock;
using StateKernel.Simulation.Context;
using StateKernel.Simulation.Modes;
using StateKernel.Simulation.Scheduling;
using StateKernel.Simulation.Signals;
using StateKernel.Simulation.StateMachines;
using System.Globalization;

namespace StateKernel.Simulation.Tests;

public sealed class SignalBehaviorIntegrationTests
{
    private static readonly TimeSpan TickInterval = TimeSpan.FromMilliseconds(10);
    private static readonly SimulationSignalId SourceSignal = SimulationSignalId.From("Source");
    private static readonly SimulationMode IdleMode = SimulationMode.From("Idle");
    private static readonly SimulationMode RunMode = SimulationMode.From("Run");
    private static readonly SimulationStateId IdleState = SimulationStateId.From("IdleState");
    private static readonly SimulationStateId RunState = SimulationStateId.From("RunState");
    private static readonly SimulationStateId WarmupState = SimulationStateId.From("WarmupState");

    [Fact]
    public void PassThroughDerivedBehavior_ReadsAPreviouslyCommittedSignalValue()
    {
        var result = RunScenario(
            2,
            new BehaviorDefinition(
                "source",
                ExecutionCadence.EveryTick,
                0,
                new ConstantBehavior(5.0),
                new AlwaysActivePolicy(),
                SourceSignal),
            new BehaviorDefinition(
                "derived",
                new ExecutionCadence(2),
                10,
                new PassThroughFromSignalBehavior(SourceSignal),
                new AlwaysActivePolicy()));

        Assert.Equal(["1:source:5", "2:source:5", "2:derived:5"], result.OutputTrace);
    }

    [Fact]
    public void OffsetDerivedBehavior_ReadsAPreviouslyCommittedSignalValueAndAppliesTheOffset()
    {
        var result = RunScenario(
            2,
            new BehaviorDefinition(
                "source",
                ExecutionCadence.EveryTick,
                0,
                new ConstantBehavior(5.0),
                new AlwaysActivePolicy(),
                SourceSignal),
            new BehaviorDefinition(
                "derived",
                new ExecutionCadence(2),
                10,
                new OffsetFromSignalBehavior(SourceSignal, 1.5),
                new AlwaysActivePolicy()));

        Assert.Equal(["1:source:5", "2:source:5", "2:derived:6.5"], result.OutputTrace);
    }

    [Fact]
    public void SameTickUpstreamProduction_IsUnavailableToDerivedReadsEvenWhenUpstreamRunsEarlier()
    {
        var recorder = new BehaviorExecutionRecorder();
        var modeController = new SimulationModeController(RunMode);
        var signalStore = new SimulationSignalValueStore();
        var scheduler = CreateScheduler(
            [
                new BehaviorScheduledWork(
                    "source",
                    new ExecutionCadence(2),
                    0,
                    new ConstantBehavior(5.0),
                    new AlwaysActivePolicy(),
                    modeController,
                    signalStore,
                    recorder,
                    SourceSignal),
                new BehaviorScheduledWork(
                    "derived",
                    new ExecutionCadence(2),
                    10,
                    new PassThroughFromSignalBehavior(SourceSignal),
                    new AlwaysActivePolicy(),
                    modeController,
                    signalStore,
                    recorder),
            ]);

        scheduler.RunNextTick();

        var exception = Assert.Throws<InvalidOperationException>(() => scheduler.RunNextTick());

        Assert.Contains(SourceSignal.Value, exception.Message);
        Assert.Equal(["2:source:5"], ToOutputTrace(recorder.Records));
    }

    [Fact]
    public void DerivedBehaviors_HoldTheLastCommittedSignalValueAcrossNonProducingTicks()
    {
        var result = RunScenario(
            3,
            new BehaviorDefinition(
                "source",
                new ExecutionCadence(2),
                0,
                new ConstantBehavior(5.0),
                new AlwaysActivePolicy(),
                SourceSignal),
            new BehaviorDefinition(
                "derived",
                new ExecutionCadence(3),
                10,
                new PassThroughFromSignalBehavior(SourceSignal),
                new AlwaysActivePolicy()));

        Assert.Equal(["2:source:5", "3:derived:5"], result.OutputTrace);
    }

    [Fact]
    public void RepeatedFreshRuns_ProduceIdenticalCommittedSignalAndOutputTraces()
    {
        var firstRun = RunScenario(
            4,
            new BehaviorDefinition(
                "source",
                ExecutionCadence.EveryTick,
                0,
                new LinearRampBehavior(10.0, 1.0),
                new AlwaysActivePolicy(),
                SourceSignal),
            new BehaviorDefinition(
                "derived",
                new ExecutionCadence(2),
                10,
                new OffsetFromSignalBehavior(SourceSignal, 5.0),
                new AlwaysActivePolicy()),
            SourceSignal);
        var secondRun = RunScenario(
            4,
            new BehaviorDefinition(
                "source",
                ExecutionCadence.EveryTick,
                0,
                new LinearRampBehavior(10.0, 1.0),
                new AlwaysActivePolicy(),
                SourceSignal),
            new BehaviorDefinition(
                "derived",
                new ExecutionCadence(2),
                10,
                new OffsetFromSignalBehavior(SourceSignal, 5.0),
                new AlwaysActivePolicy()),
            SourceSignal);

        Assert.Equal(firstRun.OutputTrace, secondRun.OutputTrace);
        Assert.Equal(firstRun.CommittedSignalTrace, secondRun.CommittedSignalTrace);
    }

    [Fact]
    public void StateDrivenModeChanges_RemainPostStepWithSignalAwareBehaviorExecution()
    {
        var modeController = new SimulationModeController(IdleMode);
        var signalStore = new SimulationSignalValueStore();
        var recorder = new BehaviorExecutionRecorder();
        var definition = new SimulationStateMachineDefinition(
            IdleState,
            [
                new SimulationStateDefinition(IdleState),
                new SimulationStateDefinition(RunState),
            ],
            [
                new SimulationStateTransitionDefinition(
                    IdleState,
                    RunState,
                    new CompletedTickMatchCondition(2)),
            ]);
        var coordinator = new SimulationStateMachineCoordinator(
            definition,
            new SimulationStateModeMap(
                definition,
                [
                    new KeyValuePair<SimulationStateId, SimulationMode>(IdleState, IdleMode),
                    new KeyValuePair<SimulationStateId, SimulationMode>(RunState, RunMode),
                ]),
            modeController);
        var scheduler = CreateScheduler(
            [
                new BehaviorScheduledWork(
                    "source",
                    ExecutionCadence.EveryTick,
                    0,
                    new ConstantBehavior(5.0),
                    new ModeMatchActivationPolicy(RunMode),
                    modeController,
                    signalStore,
                    recorder,
                    SourceSignal),
                new BehaviorScheduledWork(
                    "derived",
                    new ExecutionCadence(2),
                    10,
                    new OffsetFromSignalBehavior(SourceSignal, 1.0),
                    new ModeMatchActivationPolicy(RunMode),
                    modeController,
                    signalStore,
                    recorder),
            ]);

        for (var tick = 0; tick < 4; tick++)
        {
            var frame = scheduler.RunNextTick();
            coordinator.EvaluateAndApply(frame.Tick);
        }

        Assert.Equal(RunState, coordinator.CurrentStateId);
        Assert.Equal(RunMode, modeController.CurrentMode);
        Assert.Equal(["3:source:5", "4:source:5", "4:derived:6"], ToOutputTrace(recorder.Records));
    }

    [Fact]
    public void SameModeFormalStateChanges_DoNotDisturbSignalAwareBehaviorExecution()
    {
        var modeController = new SimulationModeController(RunMode);
        var signalStore = new SimulationSignalValueStore();
        var recorder = new BehaviorExecutionRecorder();
        var definition = new SimulationStateMachineDefinition(
            WarmupState,
            [
                new SimulationStateDefinition(IdleState),
                new SimulationStateDefinition(RunState),
                new SimulationStateDefinition(WarmupState),
            ],
            [
                new SimulationStateTransitionDefinition(
                    WarmupState,
                    RunState,
                    new CompletedTickMatchCondition(2)),
            ]);
        var coordinator = new SimulationStateMachineCoordinator(
            definition,
            new SimulationStateModeMap(
                definition,
                [
                    new KeyValuePair<SimulationStateId, SimulationMode>(IdleState, IdleMode),
                    new KeyValuePair<SimulationStateId, SimulationMode>(WarmupState, RunMode),
                    new KeyValuePair<SimulationStateId, SimulationMode>(RunState, RunMode),
                ]),
            modeController);
        var scheduler = CreateScheduler(
            [
                new BehaviorScheduledWork(
                    "source",
                    ExecutionCadence.EveryTick,
                    0,
                    new ConstantBehavior(5.0),
                    new ModeMatchActivationPolicy(RunMode),
                    modeController,
                    signalStore,
                    recorder,
                    SourceSignal),
                new BehaviorScheduledWork(
                    "derived",
                    new ExecutionCadence(2),
                    10,
                    new PassThroughFromSignalBehavior(SourceSignal),
                    new ModeMatchActivationPolicy(RunMode),
                    modeController,
                    signalStore,
                    recorder),
            ]);

        for (var tick = 0; tick < 3; tick++)
        {
            var frame = scheduler.RunNextTick();
            coordinator.EvaluateAndApply(frame.Tick);
        }

        Assert.Equal(RunState, coordinator.CurrentStateId);
        Assert.Equal(RunMode, modeController.CurrentMode);
        Assert.Equal(["1:source:5", "2:source:5", "2:derived:5", "3:source:5"], ToOutputTrace(recorder.Records));
    }

    private static ScenarioResult RunScenario(
        int tickCount,
        BehaviorDefinition firstDefinition,
        BehaviorDefinition secondDefinition,
        params SimulationSignalId[] trackedSignals)
    {
        return RunScenario(tickCount, [firstDefinition, secondDefinition], trackedSignals);
    }

    private static ScenarioResult RunScenario(
        int tickCount,
        IEnumerable<BehaviorDefinition> definitions,
        params SimulationSignalId[] trackedSignals)
    {
        if (tickCount <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(tickCount),
                "Scenarios must run at least one deterministic tick.");
        }

        var modeController = new SimulationModeController(RunMode);
        var signalStore = new SimulationSignalValueStore();
        var recorder = new BehaviorExecutionRecorder();
        var scheduler = CreateScheduler(
            definitions
                .Select(definition => (IScheduledWork)new BehaviorScheduledWork(
                    definition.Key,
                    definition.Cadence,
                    definition.Order,
                    definition.Behavior,
                    definition.ActivationPolicy,
                    modeController,
                    signalStore,
                    recorder,
                    definition.ProducedSignalId))
                .ToArray());
        var committedSignalTrace = new List<string>();

        for (var tick = 0; tick < tickCount; tick++)
        {
            var frame = scheduler.RunNextTick();
            var nextTick = frame.Tick.Advance(TickInterval);
            var committedSnapshot = signalStore.GetCommittedSnapshotForTick(nextTick);

            committedSignalTrace.Add(
                $"{nextTick.SequenceNumber}:{FormatCommittedSignals(committedSnapshot, trackedSignals)}");
        }

        return new ScenarioResult(
            ToOutputTrace(recorder.Records),
            committedSignalTrace.ToArray());
    }

    private static DeterministicSimulationScheduler CreateScheduler(IEnumerable<IScheduledWork> workItems)
    {
        var context = SimulationContext.CreateDeterministic(
            new SimulationClockSettings(TickInterval, 8),
            StateKernel.Simulation.Seed.SimulationSeed.FromInt32(100));
        var plan = new SimulationSchedulerPlan(workItems);
        return new DeterministicSimulationScheduler(context, plan);
    }

    private static string FormatCommittedSignals(
        SimulationSignalSnapshot snapshot,
        IEnumerable<SimulationSignalId> trackedSignals)
    {
        var formattedSignals = trackedSignals
            .Select(signalId => snapshot.TryGetValue(signalId, out var value)
                ? $"{signalId}:{value.Sample.Value.ToString("G17", CultureInfo.InvariantCulture)}"
                : $"{signalId}:<missing>")
            .ToArray();

        return formattedSignals.Length == 0
            ? "<none>"
            : string.Join(", ", formattedSignals);
    }

    private static string[] ToOutputTrace(IEnumerable<BehaviorExecutionRecord> records)
    {
        return records
            .Select(record => $"{record.Tick.SequenceNumber}:{record.BehaviorKey}:{record.Sample.Value.ToString("G17", CultureInfo.InvariantCulture)}")
            .ToArray();
    }

    private readonly record struct BehaviorDefinition(
        string Key,
        ExecutionCadence Cadence,
        int Order,
        IBehavior Behavior,
        IBehaviorActivationPolicy ActivationPolicy,
        SimulationSignalId? ProducedSignalId = null);

    private readonly record struct ScenarioResult(
        string[] OutputTrace,
        string[] CommittedSignalTrace);
}
