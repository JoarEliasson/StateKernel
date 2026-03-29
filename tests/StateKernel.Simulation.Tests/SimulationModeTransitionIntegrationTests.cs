using StateKernel.Simulation.Activation;
using StateKernel.Simulation.Behaviors;
using StateKernel.Simulation.Clock;
using StateKernel.Simulation.Context;
using StateKernel.Simulation.Modes;
using StateKernel.Simulation.Modes.Transitions;
using StateKernel.Simulation.Scheduling;

namespace StateKernel.Simulation.Tests;

public sealed class SimulationModeTransitionIntegrationTests
{
    private static readonly SimulationMode IdleMode = SimulationMode.From("Idle");
    private static readonly SimulationMode RunMode = SimulationMode.From("Run");

    [Fact]
    public void FirstExecutedTick_UsesTheInitialModeBeforeAnyLaterTransitionApplies()
    {
        var result = RunScenario(
            IdleMode,
            new TickMatchTransitionRule(2, RunMode),
            3,
            new BehaviorDefinition(
                "idle-only",
                ExecutionCadence.EveryTick,
                0,
                new ConstantBehavior(5.0),
                new ModeMatchActivationPolicy(IdleMode)));

        Assert.Equal(["1:idle-only:5", "2:idle-only:5"], result.OutputTrace);
        Assert.Equal(["2:Idle->Run"], result.TransitionTrace);
    }

    [Fact]
    public void TransitionRules_AreEvaluatedAgainstTheCompletedTickAndTakeEffectOnTheNextStep()
    {
        var result = RunScenario(
            IdleMode,
            new TickMatchTransitionRule(2, RunMode),
            3,
            new BehaviorDefinition(
                "run-only",
                ExecutionCadence.EveryTick,
                0,
                new ConstantBehavior(5.0),
                new ModeMatchActivationPolicy(RunMode)));

        Assert.Equal(["2:Idle->Run"], result.TransitionTrace);
        Assert.Equal(["3:run-only:5"], result.OutputTrace);
    }

    [Fact]
    public void RepeatedFreshRuns_ProduceIdenticalTransitionAndOutputTraces()
    {
        var firstRun = RunScenario(
            IdleMode,
            new TickMatchTransitionRule(2, RunMode),
            4,
            new BehaviorDefinition(
                "run-only",
                ExecutionCadence.EveryTick,
                0,
                new ConstantBehavior(5.0),
                new ModeMatchActivationPolicy(RunMode)),
            new BehaviorDefinition(
                "always",
                new ExecutionCadence(2),
                10,
                new ConstantBehavior(9.0),
                new AlwaysActivePolicy()));
        var secondRun = RunScenario(
            IdleMode,
            new TickMatchTransitionRule(2, RunMode),
            4,
            new BehaviorDefinition(
                "run-only",
                ExecutionCadence.EveryTick,
                0,
                new ConstantBehavior(5.0),
                new ModeMatchActivationPolicy(RunMode)),
            new BehaviorDefinition(
                "always",
                new ExecutionCadence(2),
                10,
                new ConstantBehavior(9.0),
                new AlwaysActivePolicy()));

        Assert.Equal(firstRun.TransitionTrace, secondRun.TransitionTrace);
        Assert.Equal(firstRun.OutputTrace, secondRun.OutputTrace);
    }

    [Fact]
    public void Transitions_ChangeActivationWithoutChangingCadenceOrDueDetermination()
    {
        var recorder = new BehaviorExecutionRecorder();
        var modeController = new SimulationModeController(IdleMode);
        var activationPolicy = new RecordingModeMatchPolicy(RunMode);
        var transitions = new List<SimulationModeTransition>();
        var scheduler = CreateScheduler(
            [
                new BehaviorScheduledWork(
                    "mode-gated",
                    new ExecutionCadence(2),
                    0,
                    new ConstantBehavior(5.0),
                    activationPolicy,
                    modeController,
                    recorder),
            ]);
        var coordinator = new SimulationModeTransitionCoordinator(
            modeController,
            new TickMatchTransitionRule(2, RunMode));

        for (var tick = 0; tick < 4; tick++)
        {
            var frame = scheduler.RunNextTick();
            var transition = coordinator.EvaluateAndApply(frame.Tick);

            if (transition is not null)
            {
                transitions.Add(transition);
            }
        }

        Assert.Equal([2L, 4L], activationPolicy.EvaluatedTicks);
        Assert.Equal(["2:Idle->Run"], ToTransitionTrace(transitions));
        Assert.Equal(["4:mode-gated:5"], ToOutputTrace(recorder.Records));
    }

    [Fact]
    public void Transitions_AreAppliedOnlyBetweenSchedulerStepsAndNeverMidBucket()
    {
        var recorder = new BehaviorExecutionRecorder();
        var modeController = new SimulationModeController(RunMode);
        var scheduler = CreateScheduler(
            [
                new BehaviorScheduledWork(
                    "run-alpha",
                    ExecutionCadence.EveryTick,
                    0,
                    new ConstantBehavior(1.0),
                    new ModeMatchActivationPolicy(RunMode),
                    modeController,
                    recorder),
                new BehaviorScheduledWork(
                    "run-beta",
                    ExecutionCadence.EveryTick,
                    10,
                    new ConstantBehavior(2.0),
                    new ModeMatchActivationPolicy(RunMode),
                    modeController,
                    recorder),
            ]);
        var coordinator = new SimulationModeTransitionCoordinator(
            modeController,
            new TickMatchTransitionRule(1, IdleMode));
        var appliedTransitions = new List<SimulationModeTransition>();

        var firstFrame = scheduler.RunNextTick();
        var firstTransition = coordinator.EvaluateAndApply(firstFrame.Tick);
        if (firstTransition is not null)
        {
            appliedTransitions.Add(firstTransition);
        }

        var secondFrame = scheduler.RunNextTick();
        var secondTransition = coordinator.EvaluateAndApply(secondFrame.Tick);
        if (secondTransition is not null)
        {
            appliedTransitions.Add(secondTransition);
        }

        Assert.Equal(["1:Run->Idle"], ToTransitionTrace(appliedTransitions));
        Assert.Equal(["1:run-alpha:1", "1:run-beta:2"], ToOutputTrace(recorder.Records));
    }

    private static ScenarioResult RunScenario(
        SimulationMode initialMode,
        ISimulationModeTransitionRule rule,
        int tickCount,
        params BehaviorDefinition[] definitions)
    {
        ArgumentNullException.ThrowIfNull(initialMode);
        ArgumentNullException.ThrowIfNull(rule);

        if (tickCount <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(tickCount),
                "Scenarios must run at least one deterministic tick.");
        }

        var recorder = new BehaviorExecutionRecorder();
        var modeController = new SimulationModeController(initialMode);
        var scheduler = CreateScheduler(
            definitions
                .Select(definition => (IScheduledWork)new BehaviorScheduledWork(
                    definition.Key,
                    definition.Cadence,
                    definition.Order,
                    definition.Behavior,
                    definition.ActivationPolicy,
                    modeController,
                    recorder))
                .ToArray());
        var coordinator = new SimulationModeTransitionCoordinator(modeController, rule);
        var transitions = new List<SimulationModeTransition>();

        for (var tick = 0; tick < tickCount; tick++)
        {
            var frame = scheduler.RunNextTick();
            var transition = coordinator.EvaluateAndApply(frame.Tick);

            if (transition is not null)
            {
                transitions.Add(transition);
            }
        }

        return new ScenarioResult(
            ToOutputTrace(recorder.Records),
            ToTransitionTrace(transitions));
    }

    private static DeterministicSimulationScheduler CreateScheduler(IEnumerable<IScheduledWork> workItems)
    {
        var context = SimulationContext.CreateDeterministic(
            new SimulationClockSettings(TimeSpan.FromMilliseconds(10), 8),
            StateKernel.Simulation.Seed.SimulationSeed.FromInt32(100));
        var plan = new SimulationSchedulerPlan(workItems);
        return new DeterministicSimulationScheduler(context, plan);
    }

    private static string[] ToOutputTrace(IEnumerable<BehaviorExecutionRecord> records)
    {
        return records
            .Select(record => $"{record.Tick.SequenceNumber}:{record.BehaviorKey}:{record.Sample.Value:G17}")
            .ToArray();
    }

    private static string[] ToTransitionTrace(IEnumerable<SimulationModeTransition> transitions)
    {
        return transitions
            .Select(transition => $"{transition.Tick.SequenceNumber}:{transition.PreviousMode}->{transition.NextMode}")
            .ToArray();
    }

    private readonly record struct BehaviorDefinition(
        string Key,
        ExecutionCadence Cadence,
        int Order,
        IBehavior Behavior,
        IBehaviorActivationPolicy ActivationPolicy);

    private readonly record struct ScenarioResult(
        string[] OutputTrace,
        string[] TransitionTrace);

    private sealed class RecordingModeMatchPolicy : IBehaviorActivationPolicy
    {
        public RecordingModeMatchPolicy(SimulationMode requiredMode)
        {
            ArgumentNullException.ThrowIfNull(requiredMode);
            RequiredMode = requiredMode;
        }

        public List<long> EvaluatedTicks { get; } = [];

        public SimulationMode RequiredMode { get; }

        public bool IsActive(BehaviorActivationContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            EvaluatedTicks.Add(context.CurrentTick.SequenceNumber);
            return context.CurrentMode == RequiredMode;
        }
    }
}
