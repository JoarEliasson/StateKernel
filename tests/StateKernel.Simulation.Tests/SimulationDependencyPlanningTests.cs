using StateKernel.Simulation.Activation;
using StateKernel.Simulation.Behaviors;
using StateKernel.Simulation.Clock;
using StateKernel.Simulation.Context;
using StateKernel.Simulation.Exceptions;
using StateKernel.Simulation.Modes;
using StateKernel.Simulation.Scheduling;
using StateKernel.Simulation.Signals;
using StateKernel.Simulation.Signals.Dependencies;
using System.Globalization;

namespace StateKernel.Simulation.Tests;

public sealed class SimulationDependencyPlanningTests
{
    private static readonly TimeSpan TickInterval = TimeSpan.FromMilliseconds(10);
    private static readonly SimulationMode RunMode = SimulationMode.From("Run");
    private static readonly SimulationSignalId SourceSignal = SimulationSignalId.From("Source");
    private static readonly SimulationSignalId OtherSignal = SimulationSignalId.From("Other");

    [Fact]
    public void PassThroughFromSignalBehavior_DeclaresStableRequiredSignalMetadata()
    {
        var behavior = new PassThroughFromSignalBehavior(SourceSignal);

        Assert.Same(behavior.RequiredSignalIds, behavior.RequiredSignalIds);
        Assert.Collection(
            behavior.RequiredSignalIds,
            requiredSignal => Assert.Equal(SourceSignal, requiredSignal));
    }

    [Fact]
    public void OffsetFromSignalBehavior_DeclaresStableRequiredSignalMetadata()
    {
        var behavior = new OffsetFromSignalBehavior(SourceSignal, 1.5);

        Assert.Same(behavior.RequiredSignalIds, behavior.RequiredSignalIds);
        Assert.Collection(
            behavior.RequiredSignalIds,
            requiredSignal => Assert.Equal(SourceSignal, requiredSignal));
    }

    [Fact]
    public void CreatePlan_SucceedsWhenDeclaredDependenciesReferenceKnownPublishedSignals()
    {
        var schedulerPlan = CreateSchedulerPlan(
            new BehaviorDefinition(
                "source",
                ExecutionCadence.EveryTick,
                0,
                new ConstantBehavior(5.0),
                SourceSignal),
            new BehaviorDefinition(
                "derived",
                new ExecutionCadence(2),
                10,
                new OffsetFromSignalBehavior(SourceSignal, 1.5)));

        var dependencyPlan = SimulationDependencyPlanner.CreatePlan(schedulerPlan);

        Assert.Equal(
            [new SimulationPublishedSignal(SourceSignal, "source")],
            dependencyPlan.PublishedSignals);
        Assert.Equal(
            [new SimulationSignalDependencyBinding("derived", SourceSignal, "source")],
            dependencyPlan.DependencyBindings);
    }

    [Fact]
    public void CreatePlan_IgnoresScheduledWorkWithoutDeclaredSignalDependencies()
    {
        var schedulerPlan = CreateSchedulerPlan(
            new BehaviorDefinition(
                "source",
                ExecutionCadence.EveryTick,
                0,
                new ConstantBehavior(5.0),
                SourceSignal),
            new BehaviorDefinition(
                "independent",
                new ExecutionCadence(2),
                10,
                new ConstantBehavior(3.0)));

        var dependencyPlan = SimulationDependencyPlanner.CreatePlan(schedulerPlan);

        Assert.Equal(
            [new SimulationPublishedSignal(SourceSignal, "source")],
            dependencyPlan.PublishedSignals);
        Assert.Empty(dependencyPlan.DependencyBindings);
    }

    [Fact]
    public void CreatePlan_FailsClearlyWhenADeclaredDependencyReferencesAnUnknownPublishedSignal()
    {
        var schedulerPlan = CreateSchedulerPlan(
            new BehaviorDefinition(
                "source",
                ExecutionCadence.EveryTick,
                0,
                new ConstantBehavior(5.0),
                OtherSignal),
            new BehaviorDefinition(
                "derived",
                new ExecutionCadence(2),
                10,
                new PassThroughFromSignalBehavior(SourceSignal)));

        var exception = Assert.Throws<SimulationConfigurationException>(
            () => SimulationDependencyPlanner.CreatePlan(schedulerPlan));

        Assert.Contains("derived", exception.Message);
        Assert.Contains(SourceSignal.Value, exception.Message);
    }

    [Fact]
    public void CreatePlan_FailsClearlyWhenABehaviorDeclaresDuplicateRequiredSignals()
    {
        var schedulerPlan = CreateSchedulerPlan(
            new BehaviorDefinition(
                "source",
                ExecutionCadence.EveryTick,
                0,
                new ConstantBehavior(5.0),
                SourceSignal),
            new BehaviorDefinition(
                "derived",
                new ExecutionCadence(2),
                10,
                new DuplicateDependencyBehavior(SourceSignal)));

        var exception = Assert.Throws<SimulationConfigurationException>(
            () => SimulationDependencyPlanner.CreatePlan(schedulerPlan));

        Assert.Contains("derived", exception.Message);
        Assert.Contains(SourceSignal.Value, exception.Message);
    }

    [Fact]
    public void RepeatedPlanCreation_ProducesIdenticalPublishedSignalsAndDependencyBindings()
    {
        var schedulerPlan = CreateSchedulerPlan(
            new BehaviorDefinition(
                "source",
                ExecutionCadence.EveryTick,
                0,
                new ConstantBehavior(5.0),
                SourceSignal),
            new BehaviorDefinition(
                "derived",
                new ExecutionCadence(2),
                10,
                new OffsetFromSignalBehavior(SourceSignal, 2.0)));
        var firstPlan = SimulationDependencyPlanner.CreatePlan(schedulerPlan);
        var secondPlan = SimulationDependencyPlanner.CreatePlan(schedulerPlan);

        Assert.Equal(firstPlan.PublishedSignals, secondPlan.PublishedSignals);
        Assert.Equal(firstPlan.DependencyBindings, secondPlan.DependencyBindings);
    }

    [Fact]
    public void CreatingADependencyPlan_DoesNotChangeRuntimeSignalExecutionBehavior()
    {
        var withoutPlanning = RunScenario(createDependencyPlan: false);
        var withPlanning = RunScenario(createDependencyPlan: true);

        Assert.Equal(withoutPlanning.OutputTrace, withPlanning.OutputTrace);
        Assert.Equal(withoutPlanning.CommittedSignalTrace, withPlanning.CommittedSignalTrace);
    }

    private static ScenarioResult RunScenario(bool createDependencyPlan)
    {
        var modeController = new SimulationModeController(RunMode);
        var signalStore = new SimulationSignalValueStore();
        var recorder = new BehaviorExecutionRecorder();
        var schedulerPlan = new SimulationSchedulerPlan(
            [
                new BehaviorScheduledWork(
                    "source",
                    ExecutionCadence.EveryTick,
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
                    new OffsetFromSignalBehavior(SourceSignal, 1.5),
                    new AlwaysActivePolicy(),
                    modeController,
                    signalStore,
                    recorder),
            ]);

        if (createDependencyPlan)
        {
            _ = SimulationDependencyPlanner.CreatePlan(schedulerPlan);
        }

        var scheduler = CreateScheduler(schedulerPlan);
        var committedSignalTrace = new List<string>();

        for (var tick = 0; tick < 2; tick++)
        {
            var frame = scheduler.RunNextTick();
            var nextTick = frame.Tick.Advance(TickInterval);
            var committedSnapshot = signalStore.GetCommittedSnapshotForTick(nextTick);

            committedSignalTrace.Add(
                $"{nextTick.SequenceNumber}:{FormatCommittedSignal(committedSnapshot, SourceSignal)}");
        }

        return new ScenarioResult(
            recorder.Records
                .Select(record => $"{record.Tick.SequenceNumber}:{record.BehaviorKey}:{record.Sample.Value.ToString("G17", CultureInfo.InvariantCulture)}")
                .ToArray(),
            committedSignalTrace.ToArray());
    }

    private static SimulationSchedulerPlan CreateSchedulerPlan(
        BehaviorDefinition firstDefinition,
        BehaviorDefinition secondDefinition)
    {
        var modeController = new SimulationModeController(RunMode);
        var signalStore = new SimulationSignalValueStore();
        var recorder = new BehaviorExecutionRecorder();

        return new SimulationSchedulerPlan(
            [
                CreateScheduledWork(firstDefinition, modeController, signalStore, recorder),
                CreateScheduledWork(secondDefinition, modeController, signalStore, recorder),
            ]);
    }

    private static BehaviorScheduledWork CreateScheduledWork(
        BehaviorDefinition definition,
        SimulationModeController modeController,
        SimulationSignalValueStore signalStore,
        BehaviorExecutionRecorder recorder)
    {
        return new BehaviorScheduledWork(
            definition.Key,
            definition.Cadence,
            definition.Order,
            definition.Behavior,
            new AlwaysActivePolicy(),
            modeController,
            signalStore,
            recorder,
            definition.ProducedSignalId);
    }

    private static DeterministicSimulationScheduler CreateScheduler(SimulationSchedulerPlan schedulerPlan)
    {
        var context = SimulationContext.CreateDeterministic(
            new SimulationClockSettings(TickInterval, 8),
            StateKernel.Simulation.Seed.SimulationSeed.FromInt32(100));

        return new DeterministicSimulationScheduler(context, schedulerPlan);
    }

    private static string FormatCommittedSignal(
        SimulationSignalSnapshot snapshot,
        SimulationSignalId signalId)
    {
        return snapshot.TryGetValue(signalId, out var value)
            ? $"{signalId}:{value.Sample.Value.ToString("G17", CultureInfo.InvariantCulture)}"
            : $"{signalId}:<missing>";
    }

    private readonly record struct BehaviorDefinition(
        string Key,
        ExecutionCadence Cadence,
        int Order,
        IBehavior Behavior,
        SimulationSignalId? ProducedSignalId = null);

    private readonly record struct ScenarioResult(
        string[] OutputTrace,
        string[] CommittedSignalTrace);

    private sealed class DuplicateDependencyBehavior : IBehavior, ISignalDependentBehavior
    {
        public DuplicateDependencyBehavior(SimulationSignalId requiredSignalId)
        {
            ArgumentNullException.ThrowIfNull(requiredSignalId);

            RequiredSignalIds = Array.AsReadOnly([requiredSignalId, requiredSignalId]);
        }

        public IReadOnlyCollection<SimulationSignalId> RequiredSignalIds { get; }

        public BehaviorSample Evaluate(BehaviorExecutionContext context)
        {
            ArgumentNullException.ThrowIfNull(context);
            return new BehaviorSample(0.0);
        }
    }
}
