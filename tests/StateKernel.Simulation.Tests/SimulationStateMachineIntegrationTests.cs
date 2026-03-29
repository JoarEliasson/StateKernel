using StateKernel.Simulation.Activation;
using StateKernel.Simulation.Behaviors;
using StateKernel.Simulation.Clock;
using StateKernel.Simulation.Context;
using StateKernel.Simulation.Modes;
using StateKernel.Simulation.Scheduling;
using StateKernel.Simulation.StateMachines;

namespace StateKernel.Simulation.Tests;

public sealed class SimulationStateMachineIntegrationTests
{
    private static readonly SimulationStateId IdleState = SimulationStateId.From("IdleState");
    private static readonly SimulationStateId RunState = SimulationStateId.From("RunState");
    private static readonly SimulationStateId WarmupState = SimulationStateId.From("WarmupState");
    private static readonly SimulationMode IdleMode = SimulationMode.From("Idle");
    private static readonly SimulationMode RunMode = SimulationMode.From("Run");

    [Fact]
    public void CurrentStateId_StartsAtTheInitialState_AndTickOneUsesItsMappedMode()
    {
        var result = RunScenario(
            CreateDefinition(
                IdleState,
                [
                    new SimulationStateTransitionDefinition(
                        IdleState,
                        RunState,
                        new CompletedTickMatchCondition(2)),
                ]),
            CreateModeBindings(
                new KeyValuePair<SimulationStateId, SimulationMode>(IdleState, IdleMode),
                new KeyValuePair<SimulationStateId, SimulationMode>(RunState, RunMode)),
            IdleMode,
            3,
            new BehaviorDefinition(
                "idle-only",
                ExecutionCadence.EveryTick,
                0,
                new ConstantBehavior(5.0),
                new ModeMatchActivationPolicy(IdleMode)),
            new BehaviorDefinition(
                "run-only",
                ExecutionCadence.EveryTick,
                10,
                new ConstantBehavior(9.0),
                new ModeMatchActivationPolicy(RunMode)));

        Assert.Equal(IdleState, result.InitialStateId);
        Assert.Equal(["1:idle-only:5", "2:idle-only:5", "3:run-only:9"], result.OutputTrace);
        Assert.Equal(["0:IdleState", "1:IdleState", "2:RunState", "3:RunState"], result.StateTrace);
    }

    [Fact]
    public void Transitions_AreEvaluatedAgainstTheCompletedTickAndPreTransitionCurrentState()
    {
        var result = RunScenario(
            CreateDefinition(
                IdleState,
                [
                    new SimulationStateTransitionDefinition(
                        IdleState,
                        RunState,
                        new CompletedTickMatchCondition(2)),
                ]),
            CreateModeBindings(
                new KeyValuePair<SimulationStateId, SimulationMode>(IdleState, IdleMode),
                new KeyValuePair<SimulationStateId, SimulationMode>(RunState, RunMode)),
            IdleMode,
            3,
            new BehaviorDefinition(
                "idle-only",
                ExecutionCadence.EveryTick,
                0,
                new ConstantBehavior(5.0),
                new ModeMatchActivationPolicy(IdleMode)),
            new BehaviorDefinition(
                "run-only",
                ExecutionCadence.EveryTick,
                10,
                new ConstantBehavior(9.0),
                new ModeMatchActivationPolicy(RunMode)));

        Assert.Equal(["2:IdleState->RunState"], result.StateTransitionTrace);
        Assert.Equal(["1:Idle", "2:Run", "3:Run"], result.ModeTrace);
        Assert.Equal(["1:idle-only:5", "2:idle-only:5", "3:run-only:9"], result.OutputTrace);
    }

    [Fact]
    public void RepeatedFreshRuns_ProduceIdenticalStateModeAndOutputTraces()
    {
        var definition = CreateDefinition(
            IdleState,
            [
                new SimulationStateTransitionDefinition(
                    IdleState,
                    RunState,
                    new CompletedTickAtOrAfterCondition(2)),
                new SimulationStateTransitionDefinition(
                    RunState,
                    WarmupState,
                    new CompletedTickMatchCondition(4)),
            ],
            WarmupState);
        var modeBindings = CreateModeBindings(
            new KeyValuePair<SimulationStateId, SimulationMode>(IdleState, IdleMode),
            new KeyValuePair<SimulationStateId, SimulationMode>(RunState, RunMode),
            new KeyValuePair<SimulationStateId, SimulationMode>(WarmupState, RunMode));

        var firstRun = RunScenario(
            definition,
            modeBindings,
            IdleMode,
            5,
            new BehaviorDefinition(
                "idle-only",
                ExecutionCadence.EveryTick,
                0,
                new ConstantBehavior(5.0),
                new ModeMatchActivationPolicy(IdleMode)),
            new BehaviorDefinition(
                "run-ramp",
                new ExecutionCadence(2),
                10,
                new LinearRampBehavior(10.0, 1.0),
                new ModeMatchActivationPolicy(RunMode)));
        var secondRun = RunScenario(
            definition,
            modeBindings,
            IdleMode,
            5,
            new BehaviorDefinition(
                "idle-only",
                ExecutionCadence.EveryTick,
                0,
                new ConstantBehavior(5.0),
                new ModeMatchActivationPolicy(IdleMode)),
            new BehaviorDefinition(
                "run-ramp",
                new ExecutionCadence(2),
                10,
                new LinearRampBehavior(10.0, 1.0),
                new ModeMatchActivationPolicy(RunMode)));

        Assert.Equal(firstRun.StateTrace, secondRun.StateTrace);
        Assert.Equal(firstRun.ModeTrace, secondRun.ModeTrace);
        Assert.Equal(firstRun.OutputTrace, secondRun.OutputTrace);
        Assert.Equal(firstRun.StateTransitionTrace, secondRun.StateTransitionTrace);
    }

    [Fact]
    public void AtOrAfterConditions_ProduceDeterministicStateAndModeProgression()
    {
        var result = RunScenario(
            CreateDefinition(
                IdleState,
                [
                    new SimulationStateTransitionDefinition(
                        IdleState,
                        RunState,
                        new CompletedTickAtOrAfterCondition(2)),
                    new SimulationStateTransitionDefinition(
                        RunState,
                        WarmupState,
                        new CompletedTickMatchCondition(4)),
                ],
                WarmupState),
            CreateModeBindings(
                new KeyValuePair<SimulationStateId, SimulationMode>(IdleState, IdleMode),
                new KeyValuePair<SimulationStateId, SimulationMode>(RunState, RunMode),
                new KeyValuePair<SimulationStateId, SimulationMode>(WarmupState, IdleMode)),
            IdleMode,
            5,
            new BehaviorDefinition(
                "idle-only",
                ExecutionCadence.EveryTick,
                0,
                new ConstantBehavior(5.0),
                new ModeMatchActivationPolicy(IdleMode)),
            new BehaviorDefinition(
                "run-only",
                ExecutionCadence.EveryTick,
                10,
                new ConstantBehavior(9.0),
                new ModeMatchActivationPolicy(RunMode)));

        Assert.Equal(["2:IdleState->RunState", "4:RunState->WarmupState"], result.StateTransitionTrace);
        Assert.Equal(["1:Idle", "2:Run", "3:Run", "4:Idle", "5:Idle"], result.ModeTrace);
        Assert.Equal(
            ["1:idle-only:5", "2:idle-only:5", "3:run-only:9", "4:run-only:9", "5:idle-only:5"],
            result.OutputTrace);
    }

    [Fact]
    public void StateMachineDrivenModeChanges_PreserveAcceptedPostStepSemantics()
    {
        var recorder = new BehaviorExecutionRecorder();
        var modeController = new SimulationModeController(IdleMode);
        var activationPolicy = new RecordingModeMatchPolicy(RunMode);
        var definition = CreateDefinition(
            IdleState,
            [
                new SimulationStateTransitionDefinition(
                    IdleState,
                    RunState,
                    new CompletedTickMatchCondition(2)),
            ]);
        var coordinator = CreateCoordinator(
            definition,
            CreateModeBindings(
                new KeyValuePair<SimulationStateId, SimulationMode>(IdleState, IdleMode),
                new KeyValuePair<SimulationStateId, SimulationMode>(RunState, RunMode)),
            modeController);
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

        for (var step = 0; step < 4; step++)
        {
            var frame = scheduler.RunNextTick();
            coordinator.EvaluateAndApply(frame.Tick);
        }

        Assert.Equal([2L, 4L], activationPolicy.EvaluatedTicks);
        Assert.Equal(["4:mode-gated:5"], ToOutputTrace(recorder.Records));
        Assert.Equal(RunState, coordinator.CurrentStateId);
        Assert.Equal(RunMode, modeController.CurrentMode);
    }

    [Fact]
    public void FormalStateChanges_CanOccurWithoutMutatingModeOrChangingActivation()
    {
        var result = RunScenario(
            CreateDefinition(
                WarmupState,
                [
                    new SimulationStateTransitionDefinition(
                        WarmupState,
                        RunState,
                        new CompletedTickAtOrAfterCondition(2)),
                ],
                WarmupState),
            CreateModeBindings(
                new KeyValuePair<SimulationStateId, SimulationMode>(IdleState, IdleMode),
                new KeyValuePair<SimulationStateId, SimulationMode>(WarmupState, RunMode),
                new KeyValuePair<SimulationStateId, SimulationMode>(RunState, RunMode)),
            RunMode,
            3,
            new BehaviorDefinition(
                "run-only",
                ExecutionCadence.EveryTick,
                0,
                new ConstantBehavior(5.0),
                new ModeMatchActivationPolicy(RunMode)),
            new BehaviorDefinition(
                "idle-only",
                ExecutionCadence.EveryTick,
                10,
                new ConstantBehavior(9.0),
                new ModeMatchActivationPolicy(IdleMode)));

        Assert.Equal(["2:WarmupState->RunState"], result.StateTransitionTrace);
        Assert.Equal(["1:Run", "2:Run", "3:Run"], result.ModeTrace);
        Assert.Equal(["1:run-only:5", "2:run-only:5", "3:run-only:5"], result.OutputTrace);
        Assert.Equal(["0:WarmupState", "1:WarmupState", "2:RunState", "3:RunState"], result.StateTrace);
    }

    private static ScenarioResult RunScenario(
        SimulationStateMachineDefinition definition,
        KeyValuePair<SimulationStateId, SimulationMode>[] modeBindings,
        SimulationMode initialMode,
        int tickCount,
        params BehaviorDefinition[] definitions)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(modeBindings);
        ArgumentNullException.ThrowIfNull(initialMode);

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
        var coordinator = CreateCoordinator(definition, modeBindings, modeController);
        var stateTrace = new List<string> { $"0:{coordinator.CurrentStateId}" };
        var modeTrace = new List<string>();
        var stateTransitions = new List<SimulationStateTransition>();

        for (var tick = 0; tick < tickCount; tick++)
        {
            var frame = scheduler.RunNextTick();
            var transition = coordinator.EvaluateAndApply(frame.Tick);

            if (transition is not null)
            {
                stateTransitions.Add(transition);
            }

            stateTrace.Add($"{frame.Tick.SequenceNumber}:{coordinator.CurrentStateId}");
            modeTrace.Add($"{frame.Tick.SequenceNumber}:{modeController.CurrentMode}");
        }

        return new ScenarioResult(
            definition.InitialStateId,
            stateTrace.ToArray(),
            modeTrace.ToArray(),
            ToOutputTrace(recorder.Records),
            ToStateTransitionTrace(stateTransitions));
    }

    private static SimulationStateMachineDefinition CreateDefinition(
        SimulationStateId initialStateId,
        IEnumerable<SimulationStateTransitionDefinition> transitions,
        params SimulationStateId[] extraStates)
    {
        var states = new List<SimulationStateDefinition>
        {
            new(IdleState),
            new(RunState),
        };

        states.AddRange(extraStates.Select(stateId => new SimulationStateDefinition(stateId)));

        return new SimulationStateMachineDefinition(initialStateId, states, transitions);
    }

    private static SimulationStateMachineDefinition CreateDefinition(
        SimulationStateId initialStateId)
    {
        return CreateDefinition(initialStateId, Array.Empty<SimulationStateTransitionDefinition>());
    }

    private static SimulationStateMachineCoordinator CreateCoordinator(
        SimulationStateMachineDefinition definition,
        KeyValuePair<SimulationStateId, SimulationMode>[] modeBindings,
        SimulationModeController modeController)
    {
        var stateModeMap = new SimulationStateModeMap(definition, modeBindings);
        return new SimulationStateMachineCoordinator(definition, stateModeMap, modeController);
    }

    private static KeyValuePair<SimulationStateId, SimulationMode>[] CreateModeBindings(
        params KeyValuePair<SimulationStateId, SimulationMode>[] modeBindings)
    {
        return modeBindings;
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

    private static string[] ToStateTransitionTrace(IEnumerable<SimulationStateTransition> transitions)
    {
        return transitions
            .Select(transition => $"{transition.CompletedTick.SequenceNumber}:{transition.PreviousStateId}->{transition.NextStateId}")
            .ToArray();
    }

    private readonly record struct BehaviorDefinition(
        string Key,
        ExecutionCadence Cadence,
        int Order,
        IBehavior Behavior,
        IBehaviorActivationPolicy ActivationPolicy);

    private readonly record struct ScenarioResult(
        SimulationStateId InitialStateId,
        string[] StateTrace,
        string[] ModeTrace,
        string[] OutputTrace,
        string[] StateTransitionTrace);

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
