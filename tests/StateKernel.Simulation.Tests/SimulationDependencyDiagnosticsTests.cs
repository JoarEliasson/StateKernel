using StateKernel.Simulation.Activation;
using StateKernel.Simulation.Behaviors;
using StateKernel.Simulation.Clock;
using StateKernel.Simulation.Context;
using StateKernel.Simulation.Exceptions;
using StateKernel.Simulation.Modes;
using StateKernel.Simulation.Scheduling;
using StateKernel.Simulation.Signals;
using StateKernel.Simulation.Signals.Dependencies;
using StateKernel.Simulation.Signals.Dependencies.Diagnostics;
using StateKernel.Simulation.StateMachines;
using System.Globalization;

namespace StateKernel.Simulation.Tests;

public sealed class SimulationDependencyDiagnosticsTests
{
    private static readonly TimeSpan TickInterval = TimeSpan.FromMilliseconds(10);
    private static readonly SimulationSignalId SourceSignal = SimulationSignalId.From("Source");
    private static readonly SimulationMode IdleMode = SimulationMode.From("Idle");
    private static readonly SimulationMode RunMode = SimulationMode.From("Run");
    private static readonly SimulationStateId IdleState = SimulationStateId.From("IdleState");
    private static readonly SimulationStateId RunState = SimulationStateId.From("RunState");

    [Fact]
    public void DependencyDiagnosticsReport_ExposesOrderedDiagnosticsAndHasDiagnostics()
    {
        var firstDiagnostic = new SimulationDependencyDiagnostic(
            SimulationDependencyDiagnosticCode.SameTickDependencyUnavailable,
            "consumer-a",
            SourceSignal,
            "source",
            2,
            2,
            "First");
        var secondDiagnostic = new SimulationDependencyDiagnostic(
            SimulationDependencyDiagnosticCode.NoPriorProducerBeforeFirstConsumerTick,
            "consumer-b",
            SourceSignal,
            "source",
            4,
            6,
            "Second");
        var report = new SimulationDependencyDiagnosticsReport([firstDiagnostic, secondDiagnostic]);

        Assert.True(report.HasDiagnostics);
        Assert.Equal([firstDiagnostic, secondDiagnostic], report.Diagnostics);
        Assert.False(new SimulationDependencyDiagnosticsReport(Array.Empty<SimulationDependencyDiagnostic>()).HasDiagnostics);
    }

    [Theory]
    [InlineData(1, 2)]
    [InlineData(3, 6)]
    public void Analyze_ReturnsNoDiagnosticsForClearlySatisfiableFirstNeedSetups(
        int producerCadence,
        int consumerCadence)
    {
        var schedulerPlan = CreateSchedulerPlan(
            new BehaviorDefinition(
                "source",
                new ExecutionCadence(producerCadence),
                0,
                new ConstantBehavior(5.0),
                new AlwaysActivePolicy(),
                SourceSignal),
            new BehaviorDefinition(
                "consumer",
                new ExecutionCadence(consumerCadence),
                10,
                new PassThroughFromSignalBehavior(SourceSignal),
                new AlwaysActivePolicy()));
        var dependencyPlan = SimulationDependencyPlanner.CreatePlan(schedulerPlan);

        var report = SimulationDependencyDiagnosticsAnalyzer.Analyze(schedulerPlan, dependencyPlan);

        Assert.False(report.HasDiagnostics);
        Assert.Empty(report.Diagnostics);
    }

    [Fact]
    public void Analyze_FlagsSameTickDependencyUnavailableWhenFirstNeedAndFirstPublishShareTheSameTick()
    {
        var schedulerPlan = CreateSchedulerPlan(
            new BehaviorDefinition(
                "source",
                new ExecutionCadence(2),
                0,
                new ConstantBehavior(5.0),
                new AlwaysActivePolicy(),
                SourceSignal),
            new BehaviorDefinition(
                "consumer",
                new ExecutionCadence(2),
                10,
                new PassThroughFromSignalBehavior(SourceSignal),
                new AlwaysActivePolicy()));
        var dependencyPlan = SimulationDependencyPlanner.CreatePlan(schedulerPlan);

        var report = SimulationDependencyDiagnosticsAnalyzer.Analyze(schedulerPlan, dependencyPlan);

        var diagnostic = Assert.Single(report.Diagnostics);
        Assert.Equal(SimulationDependencyDiagnosticCode.SameTickDependencyUnavailable, diagnostic.Code);
        Assert.Equal("consumer", diagnostic.ConsumerWorkKey);
        Assert.Equal("source", diagnostic.ProducerWorkKey);
        Assert.Equal(SourceSignal, diagnostic.RequiredSignalId);
        Assert.Equal(2, diagnostic.ConsumerFirstDueTick);
        Assert.Equal(2, diagnostic.ProducerFirstDueTick);
        Assert.Contains("first need only", diagnostic.Message);
    }

    [Theory]
    [InlineData(2, 1)]
    [InlineData(6, 4)]
    public void Analyze_FlagsNoPriorProducerBeforeFirstConsumerTickWhenFirstNeedPrecedesFirstPublish(
        int producerCadence,
        int consumerCadence)
    {
        var schedulerPlan = CreateSchedulerPlan(
            new BehaviorDefinition(
                "source",
                new ExecutionCadence(producerCadence),
                0,
                new ConstantBehavior(5.0),
                new AlwaysActivePolicy(),
                SourceSignal),
            new BehaviorDefinition(
                "consumer",
                new ExecutionCadence(consumerCadence),
                10,
                new PassThroughFromSignalBehavior(SourceSignal),
                new AlwaysActivePolicy()));
        var dependencyPlan = SimulationDependencyPlanner.CreatePlan(schedulerPlan);

        var report = SimulationDependencyDiagnosticsAnalyzer.Analyze(schedulerPlan, dependencyPlan);

        var diagnostic = Assert.Single(report.Diagnostics);
        Assert.Equal(
            SimulationDependencyDiagnosticCode.NoPriorProducerBeforeFirstConsumerTick,
            diagnostic.Code);
        Assert.Equal(consumerCadence, diagnostic.ConsumerFirstDueTick);
        Assert.Equal(producerCadence, diagnostic.ProducerFirstDueTick);
        Assert.Contains("first need only", diagnostic.Message);
    }

    [Fact]
    public void RepeatedAnalysis_ProducesIdenticalDiagnosticsInDependencyBindingOrder()
    {
        var schedulerPlan = CreateSchedulerPlan(
            new BehaviorDefinition(
                "source",
                new ExecutionCadence(2),
                0,
                new ConstantBehavior(5.0),
                new AlwaysActivePolicy(),
                SourceSignal),
            new BehaviorDefinition(
                "consumer-a",
                new ExecutionCadence(2),
                5,
                new PassThroughFromSignalBehavior(SourceSignal),
                new AlwaysActivePolicy()),
            new BehaviorDefinition(
                "consumer-b",
                new ExecutionCadence(2),
                10,
                new OffsetFromSignalBehavior(SourceSignal, 1.0),
                new AlwaysActivePolicy()));
        var dependencyPlan = SimulationDependencyPlanner.CreatePlan(schedulerPlan);

        var firstReport = SimulationDependencyDiagnosticsAnalyzer.Analyze(schedulerPlan, dependencyPlan);
        var secondReport = SimulationDependencyDiagnosticsAnalyzer.Analyze(schedulerPlan, dependencyPlan);

        Assert.Equal(firstReport.Diagnostics, secondReport.Diagnostics);
        Assert.Equal(
            ["consumer-a", "consumer-b"],
            firstReport.Diagnostics.Select(static diagnostic => diagnostic.ConsumerWorkKey).ToArray());
    }

    [Fact]
    public void Analyze_ThrowsClearlyWhenDependencyPlanReferencesWorkKeysMissingFromTheSchedulerPlan()
    {
        var schedulerPlan = CreateSchedulerPlan(
            new BehaviorDefinition(
                "source",
                ExecutionCadence.EveryTick,
                0,
                new ConstantBehavior(5.0),
                new AlwaysActivePolicy(),
                SourceSignal),
            new BehaviorDefinition(
                "consumer",
                new ExecutionCadence(2),
                10,
                new PassThroughFromSignalBehavior(SourceSignal),
                new AlwaysActivePolicy()));
        var dependencyPlan = new SimulationDependencyPlan(
            [new SimulationPublishedSignal(SourceSignal, "source")],
            [new SimulationSignalDependencyBinding("missing-consumer", SourceSignal, "source")]);

        var exception = Assert.Throws<SimulationConfigurationException>(
            () => SimulationDependencyDiagnosticsAnalyzer.Analyze(schedulerPlan, dependencyPlan));

        Assert.Contains("missing-consumer", exception.Message);
    }

    [Fact]
    public void BuildingADiagnosticsReport_DoesNotChangeRuntimeOutputsCommittedSignalsOrStateModeBehavior()
    {
        var withoutDiagnostics = RunStateDrivenScenario(createDiagnosticsReport: false);
        var withDiagnostics = RunStateDrivenScenario(createDiagnosticsReport: true);

        Assert.Equal(withoutDiagnostics.OutputTrace, withDiagnostics.OutputTrace);
        Assert.Equal(withoutDiagnostics.CommittedSignalTrace, withDiagnostics.CommittedSignalTrace);
        Assert.Equal(withoutDiagnostics.FinalStateId, withDiagnostics.FinalStateId);
        Assert.Equal(withoutDiagnostics.FinalMode, withDiagnostics.FinalMode);
    }

    [Fact]
    public void DiagnosticsCanFlagAFirstNeedIssueEvenWhenLaterRuntimeRecoveryIsPossible()
    {
        var schedulerPlan = CreateSchedulerPlan(
            new BehaviorDefinition(
                "source",
                new ExecutionCadence(6),
                0,
                new ConstantBehavior(5.0),
                new AlwaysActivePolicy(),
                SourceSignal),
            new BehaviorDefinition(
                "consumer",
                new ExecutionCadence(4),
                10,
                new PassThroughFromSignalBehavior(SourceSignal),
                new TickRangeActivationPolicy(8, 8)));
        var dependencyPlan = SimulationDependencyPlanner.CreatePlan(schedulerPlan);

        var report = SimulationDependencyDiagnosticsAnalyzer.Analyze(schedulerPlan, dependencyPlan);
        var runtimeResult = RunScenario(schedulerPlan, tickCount: 8, trackedSignals: [SourceSignal]);

        var diagnostic = Assert.Single(report.Diagnostics);
        Assert.Equal(
            SimulationDependencyDiagnosticCode.NoPriorProducerBeforeFirstConsumerTick,
            diagnostic.Code);
        Assert.Equal(["6:source:5", "8:consumer:5"], runtimeResult.OutputTrace);
        Assert.Equal("Source:5", runtimeResult.CommittedSignalTrace[^1].Split(':', 2)[1]);
    }

    private static StateDrivenScenarioResult RunStateDrivenScenario(bool createDiagnosticsReport)
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
        var schedulerPlan = new SimulationSchedulerPlan(
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

        if (createDiagnosticsReport)
        {
            var dependencyPlan = SimulationDependencyPlanner.CreatePlan(schedulerPlan);
            _ = SimulationDependencyDiagnosticsAnalyzer.Analyze(schedulerPlan, dependencyPlan);
        }

        var scheduler = CreateScheduler(schedulerPlan);
        var committedSignalTrace = new List<string>();

        for (var tick = 0; tick < 4; tick++)
        {
            var frame = scheduler.RunNextTick();
            coordinator.EvaluateAndApply(frame.Tick);
            var nextTick = frame.Tick.Advance(TickInterval);
            var committedSnapshot = signalStore.GetCommittedSnapshotForTick(nextTick);

            committedSignalTrace.Add(
                $"{nextTick.SequenceNumber}:{FormatCommittedSignals(committedSnapshot, [SourceSignal])}");
        }

        return new StateDrivenScenarioResult(
            ToOutputTrace(recorder.Records),
            committedSignalTrace.ToArray(),
            coordinator.CurrentStateId,
            modeController.CurrentMode);
    }

    private static ScenarioResult RunScenario(
        SimulationSchedulerPlan schedulerPlan,
        int tickCount,
        IEnumerable<SimulationSignalId> trackedSignals)
    {
        var scheduler = CreateScheduler(schedulerPlan);
        var signalStore = schedulerPlan.Buckets
            .SelectMany(static bucket => bucket.WorkItems)
            .OfType<BehaviorScheduledWork>()
            .Select(static work => work.SignalValueStore)
            .First();
        var recorder = schedulerPlan.Buckets
            .SelectMany(static bucket => bucket.WorkItems)
            .OfType<BehaviorScheduledWork>()
            .Select(static work => work.OutputSink)
            .OfType<BehaviorExecutionRecorder>()
            .First();
        var committedSignalTrace = new List<string>();

        for (var tick = 0; tick < tickCount; tick++)
        {
            var frame = scheduler.RunNextTick();
            var nextTick = frame.Tick.Advance(TickInterval);
            var committedSnapshot = signalStore.GetCommittedSnapshotForTick(nextTick);

            committedSignalTrace.Add(
                $"{nextTick.SequenceNumber}:{FormatCommittedSignals(committedSnapshot, trackedSignals)}");
        }

        return new ScenarioResult(ToOutputTrace(recorder.Records), committedSignalTrace.ToArray());
    }

    private static SimulationSchedulerPlan CreateSchedulerPlan(params BehaviorDefinition[] definitions)
    {
        var modeController = new SimulationModeController(RunMode);
        var signalStore = new SimulationSignalValueStore();
        var recorder = new BehaviorExecutionRecorder();

        return new SimulationSchedulerPlan(
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
    }

    private static DeterministicSimulationScheduler CreateScheduler(SimulationSchedulerPlan schedulerPlan)
    {
        var context = SimulationContext.CreateDeterministic(
            new SimulationClockSettings(TickInterval, 8),
            StateKernel.Simulation.Seed.SimulationSeed.FromInt32(100));

        return new DeterministicSimulationScheduler(context, schedulerPlan);
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

    private readonly record struct StateDrivenScenarioResult(
        string[] OutputTrace,
        string[] CommittedSignalTrace,
        SimulationStateId FinalStateId,
        SimulationMode FinalMode);
}
