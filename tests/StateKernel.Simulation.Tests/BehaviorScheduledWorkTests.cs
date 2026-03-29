using StateKernel.Simulation.Activation;
using StateKernel.Simulation.Behaviors;
using StateKernel.Simulation.Clock;
using StateKernel.Simulation.Context;
using StateKernel.Simulation.Modes;
using StateKernel.Simulation.Scheduling;

namespace StateKernel.Simulation.Tests;

public sealed class BehaviorScheduledWorkTests
{
    private static readonly SimulationMode IdleMode = SimulationMode.From("Idle");
    private static readonly SimulationMode RunMode = SimulationMode.From("Run");
    private static readonly SimulationMode MaintenanceMode = SimulationMode.From("Maintenance");

    [Fact]
    public void RunTicks_RecordsBehaviorOutputsInStableOrder()
    {
        var recorder = new BehaviorExecutionRecorder();
        var modeController = new SimulationModeController(RunMode);
        var scheduler = CreateScheduler(
            [
                new BehaviorScheduledWork("ramp", ExecutionCadence.EveryTick, 20, new LinearRampBehavior(10.0, 2.0), new AlwaysActivePolicy(), modeController, recorder),
                new BehaviorScheduledWork("constant", ExecutionCadence.EveryTick, 10, new ConstantBehavior(5.0), new AlwaysActivePolicy(), modeController, recorder),
                new BehaviorScheduledWork("slow", new ExecutionCadence(2), 0, new ConstantBehavior(99.0), new AlwaysActivePolicy(), modeController, recorder),
            ]);

        scheduler.RunTicks(3);

        var expectedTrace = new[]
        {
            "1:constant:5",
            "1:ramp:12",
            "2:constant:5",
            "2:ramp:14",
            "2:slow:99",
            "3:constant:5",
            "3:ramp:16",
        };

        Assert.Equal(expectedTrace, ToTrace(recorder.Records));
    }

    [Fact]
    public void Cadence_ChangesSamplingPointsWithoutChangingRampFunction()
    {
        var everyTickRecorder = new BehaviorExecutionRecorder();
        var everyTwoRecorder = new BehaviorExecutionRecorder();
        var everyTickModeController = new SimulationModeController(RunMode);
        var everyTwoModeController = new SimulationModeController(RunMode);

        var everyTickScheduler = CreateScheduler(
            [new BehaviorScheduledWork("ramp", ExecutionCadence.EveryTick, 0, new LinearRampBehavior(10.0, 2.0), new AlwaysActivePolicy(), everyTickModeController, everyTickRecorder)]);
        var everyTwoScheduler = CreateScheduler(
            [new BehaviorScheduledWork("ramp", new ExecutionCadence(2), 0, new LinearRampBehavior(10.0, 2.0), new AlwaysActivePolicy(), everyTwoModeController, everyTwoRecorder)]);

        everyTickScheduler.RunTicks(4);
        everyTwoScheduler.RunTicks(4);

        var expectedEveryTick = new[]
        {
            "1:ramp:12",
            "2:ramp:14",
            "3:ramp:16",
            "4:ramp:18",
        };
        var expectedEveryTwo = new[]
        {
            "2:ramp:14",
            "4:ramp:18",
        };

        Assert.Equal(expectedEveryTick, ToTrace(everyTickRecorder.Records));
        Assert.Equal(expectedEveryTwo, ToTrace(everyTwoRecorder.Records));
    }

    [Fact]
    public void EquivalentFreshRuns_ProduceIdenticalRecordedOutputSequences()
    {
        var firstTrace = RunTraceWithModeSequence(
            [RunMode, IdleMode, RunMode, RunMode],
            new BehaviorDefinition("constant", ExecutionCadence.EveryTick, 10, new ConstantBehavior(5.0), new AlwaysActivePolicy()),
            new BehaviorDefinition("mode-ramp", ExecutionCadence.EveryTick, 20, new LinearRampBehavior(10.0, 1.0), new ModeMatchActivationPolicy(RunMode)));
        var secondTrace = RunTraceWithModeSequence(
            [RunMode, IdleMode, RunMode, RunMode],
            new BehaviorDefinition("constant", ExecutionCadence.EveryTick, 10, new ConstantBehavior(5.0), new AlwaysActivePolicy()),
            new BehaviorDefinition("mode-ramp", ExecutionCadence.EveryTick, 20, new LinearRampBehavior(10.0, 1.0), new ModeMatchActivationPolicy(RunMode)));

        Assert.Equal(firstTrace, secondTrace);
    }

    [Fact]
    public void EquivalentPlans_ProduceIdenticalRecordedOutputRegardlessOfRegistrationOrder()
    {
        var firstTrace = RunTrace(
            new BehaviorDefinition("ramp", new ExecutionCadence(2), 20, new LinearRampBehavior(10.0, 1.0), new AlwaysActivePolicy()),
            new BehaviorDefinition("constant", ExecutionCadence.EveryTick, 10, new ConstantBehavior(5.0), new AlwaysActivePolicy()));
        var secondTrace = RunTrace(
            new BehaviorDefinition("constant", ExecutionCadence.EveryTick, 10, new ConstantBehavior(5.0), new AlwaysActivePolicy()),
            new BehaviorDefinition("ramp", new ExecutionCadence(2), 20, new LinearRampBehavior(10.0, 1.0), new AlwaysActivePolicy()));

        Assert.Equal(firstTrace, secondTrace);
    }

    [Fact]
    public void DueButInactiveWork_ProducesNoOutputRecord()
    {
        var recorder = new BehaviorExecutionRecorder();
        var modeController = new SimulationModeController(IdleMode);
        var scheduler = CreateScheduler(
            [
                new BehaviorScheduledWork(
                    "inactive",
                    ExecutionCadence.EveryTick,
                    0,
                    new ConstantBehavior(5.0),
                    new ModeMatchActivationPolicy(RunMode),
                    modeController,
                    recorder),
            ]);

        scheduler.RunNextTick();

        Assert.Empty(recorder.Records);
    }

    [Fact]
    public void DueButInactiveWork_DoesNotInvokeTheSink()
    {
        var sink = new CountingSink();
        var modeController = new SimulationModeController(IdleMode);
        var scheduler = CreateScheduler(
            [
                new BehaviorScheduledWork(
                    "inactive",
                    ExecutionCadence.EveryTick,
                    0,
                    new ConstantBehavior(5.0),
                    new ModeMatchActivationPolicy(RunMode),
                    modeController,
                    sink),
            ]);

        scheduler.RunNextTick();

        Assert.Equal(0, sink.CallCount);
    }

    [Fact]
    public void DueAndActiveWork_ProducesAnOutputRecord()
    {
        var recorder = new BehaviorExecutionRecorder();
        var modeController = new SimulationModeController(RunMode);
        var scheduler = CreateScheduler(
            [
                new BehaviorScheduledWork(
                    "active",
                    ExecutionCadence.EveryTick,
                    0,
                    new ConstantBehavior(5.0),
                    new ModeMatchActivationPolicy(RunMode),
                    modeController,
                    recorder),
            ]);

        scheduler.RunNextTick();

        var expectedTrace = new[] { "1:active:5" };

        Assert.Equal(expectedTrace, ToTrace(recorder.Records));
    }

    [Fact]
    public void InactiveWork_DoesNotEvaluateTheBehavior()
    {
        var recorder = new BehaviorExecutionRecorder();
        var modeController = new SimulationModeController(IdleMode);
        var scheduler = CreateScheduler(
            [
                new BehaviorScheduledWork(
                    "inactive",
                    ExecutionCadence.EveryTick,
                    0,
                    new ThrowingBehavior(),
                    new ModeMatchActivationPolicy(RunMode),
                    modeController,
                    recorder),
            ]);

        scheduler.RunNextTick();

        Assert.Empty(recorder.Records);
    }

    [Fact]
    public void MixedActiveAndInactiveDueWork_PreservesStableOrderingAmongActiveOutputsUnderTheCurrentMode()
    {
        var recorder = new BehaviorExecutionRecorder();
        var modeController = new SimulationModeController(RunMode);
        var scheduler = CreateScheduler(
            [
                new BehaviorScheduledWork(
                    "idle-low",
                    ExecutionCadence.EveryTick,
                    0,
                    new ConstantBehavior(1.0),
                    new ModeMatchActivationPolicy(IdleMode),
                    modeController,
                    recorder),
                new BehaviorScheduledWork(
                    "run-mid",
                    ExecutionCadence.EveryTick,
                    10,
                    new ConstantBehavior(5.0),
                    new AlwaysActivePolicy(),
                    modeController,
                    recorder),
                new BehaviorScheduledWork(
                    "maintenance-high",
                    ExecutionCadence.EveryTick,
                    15,
                    new ConstantBehavior(9.0),
                    new ModeMatchActivationPolicy(MaintenanceMode),
                    modeController,
                    recorder),
                new BehaviorScheduledWork(
                    "run-top",
                    ExecutionCadence.EveryTick,
                    20,
                    new LinearRampBehavior(10.0, 2.0),
                    new ModeMatchActivationPolicy(RunMode),
                    modeController,
                    recorder),
            ]);

        scheduler.RunNextTick();

        var expectedTrace = new[]
        {
            "1:run-mid:5",
            "1:run-top:12",
        };

        Assert.Equal(expectedTrace, ToTrace(recorder.Records));
    }

    [Fact]
    public void ChangingControllerModeBetweenTicks_ChangesWhetherDueWorkExecutes()
    {
        var recorder = new BehaviorExecutionRecorder();
        var modeController = new SimulationModeController(IdleMode);
        var scheduler = CreateScheduler(
            [
                new BehaviorScheduledWork(
                    "mode-gated",
                    ExecutionCadence.EveryTick,
                    0,
                    new ConstantBehavior(5.0),
                    new ModeMatchActivationPolicy(RunMode),
                    modeController,
                    recorder),
            ]);

        scheduler.RunNextTick();
        modeController.SetMode(RunMode);
        scheduler.RunNextTick();
        modeController.SetMode(IdleMode);
        scheduler.RunNextTick();

        var expectedTrace = new[]
        {
            "2:mode-gated:5",
        };

        Assert.Equal(expectedTrace, ToTrace(recorder.Records));
    }

    [Fact]
    public void ModeChanges_DoNotAffectCadenceOrDueDetermination()
    {
        var recorder = new BehaviorExecutionRecorder();
        var modeController = new SimulationModeController(IdleMode);
        var activationPolicy = new RecordingModeMatchPolicy(RunMode);
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

        modeController.SetMode(IdleMode);
        scheduler.RunNextTick();
        modeController.SetMode(RunMode);
        scheduler.RunNextTick();
        modeController.SetMode(IdleMode);
        scheduler.RunNextTick();
        modeController.SetMode(IdleMode);
        scheduler.RunNextTick();

        Assert.Equal([2L, 4L], activationPolicy.EvaluatedTicks);

        var expectedTrace = new[]
        {
            "2:mode-gated:5",
        };

        Assert.Equal(expectedTrace, ToTrace(recorder.Records));
    }

    [Fact]
    public void ActivationExceptions_PropagateAndStopLaterWorkInTheTick()
    {
        var recorder = new BehaviorExecutionRecorder();
        var modeController = new SimulationModeController(RunMode);
        var scheduler = CreateScheduler(
            [
                new BehaviorScheduledWork("failing-activation", ExecutionCadence.EveryTick, 0, new ConstantBehavior(1.0), new ThrowingActivationPolicy(), modeController, recorder),
                new BehaviorScheduledWork("constant", ExecutionCadence.EveryTick, 10, new ConstantBehavior(5.0), new AlwaysActivePolicy(), modeController, recorder),
            ]);

        var exception = Assert.Throws<InvalidOperationException>(() => scheduler.RunNextTick());

        Assert.Equal("activation failure", exception.Message);
        Assert.Empty(recorder.Records);
    }

    [Fact]
    public void BehaviorExceptions_PropagateAndStopLaterWorkInTheTick()
    {
        var recorder = new BehaviorExecutionRecorder();
        var modeController = new SimulationModeController(RunMode);
        var scheduler = CreateScheduler(
            [
                new BehaviorScheduledWork("failing", ExecutionCadence.EveryTick, 0, new ThrowingBehavior(), new AlwaysActivePolicy(), modeController, recorder),
                new BehaviorScheduledWork("constant", ExecutionCadence.EveryTick, 10, new ConstantBehavior(5.0), new AlwaysActivePolicy(), modeController, recorder),
            ]);

        var exception = Assert.Throws<InvalidOperationException>(() => scheduler.RunNextTick());

        Assert.Equal("behavior failure", exception.Message);
        Assert.Empty(recorder.Records);
    }

    [Fact]
    public void SinkExceptions_PropagateAndStopLaterWorkInTheTick()
    {
        var recorder = new BehaviorExecutionRecorder();
        var modeController = new SimulationModeController(RunMode);
        var scheduler = CreateScheduler(
            [
                new BehaviorScheduledWork("failing-sink", ExecutionCadence.EveryTick, 0, new ConstantBehavior(1.0), new AlwaysActivePolicy(), modeController, new ThrowingSink()),
                new BehaviorScheduledWork("constant", ExecutionCadence.EveryTick, 10, new ConstantBehavior(5.0), new AlwaysActivePolicy(), modeController, recorder),
            ]);

        var exception = Assert.Throws<InvalidOperationException>(() => scheduler.RunNextTick());

        Assert.Equal("sink failure", exception.Message);
        Assert.Empty(recorder.Records);
    }

    [Fact]
    public void ModeSourceExceptions_PropagateAndStopExecution()
    {
        var recorder = new BehaviorExecutionRecorder();
        var scheduler = CreateScheduler(
            [
                new BehaviorScheduledWork("mode-failing", ExecutionCadence.EveryTick, 0, new ConstantBehavior(1.0), new AlwaysActivePolicy(), new ThrowingModeSource(), recorder),
            ]);

        var exception = Assert.Throws<InvalidOperationException>(() => scheduler.RunNextTick());

        Assert.Equal("mode source failure", exception.Message);
        Assert.Empty(recorder.Records);
    }

    private static DeterministicSimulationScheduler CreateScheduler(IEnumerable<IScheduledWork> workItems)
    {
        var context = SimulationContext.CreateDeterministic(
            new SimulationClockSettings(TimeSpan.FromMilliseconds(10), 8),
            StateKernel.Simulation.Seed.SimulationSeed.FromInt32(100));
        var plan = new SimulationSchedulerPlan(workItems);
        return new DeterministicSimulationScheduler(context, plan);
    }

    private static string[] RunTrace(params BehaviorDefinition[] definitions)
    {
        var recorder = new BehaviorExecutionRecorder();
        var modeController = new SimulationModeController(RunMode);
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

        scheduler.RunTicks(4);
        return ToTrace(recorder.Records);
    }

    private static string[] RunTraceWithModeSequence(
        IReadOnlyList<SimulationMode> modeSequence,
        params BehaviorDefinition[] definitions)
    {
        ArgumentNullException.ThrowIfNull(modeSequence);

        if (modeSequence.Count == 0)
        {
            throw new ArgumentException(
                "Mode sequences must contain at least one step.",
                nameof(modeSequence));
        }

        var recorder = new BehaviorExecutionRecorder();
        var modeController = new SimulationModeController(modeSequence[0]);
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

        foreach (var mode in modeSequence)
        {
            modeController.SetMode(mode);
            scheduler.RunNextTick();
        }

        return ToTrace(recorder.Records);
    }

    private static string[] ToTrace(IEnumerable<BehaviorExecutionRecord> records)
    {
        return records
            .Select(record => $"{record.Tick.SequenceNumber}:{record.BehaviorKey}:{record.Sample.Value:G17}")
            .ToArray();
    }

    private readonly record struct BehaviorDefinition(
        string Key,
        ExecutionCadence Cadence,
        int Order,
        IBehavior Behavior,
        IBehaviorActivationPolicy ActivationPolicy);

    private sealed class CountingSink : IBehaviorOutputSink
    {
        public int CallCount { get; private set; }

        public void Record(BehaviorExecutionRecord record)
        {
            ArgumentNullException.ThrowIfNull(record);
            CallCount++;
        }
    }

    private sealed class ThrowingActivationPolicy : IBehaviorActivationPolicy
    {
        public bool IsActive(BehaviorActivationContext context)
        {
            ArgumentNullException.ThrowIfNull(context);
            throw new InvalidOperationException("activation failure");
        }
    }

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

    private sealed class ThrowingBehavior : IBehavior
    {
        public BehaviorSample Evaluate(BehaviorExecutionContext context)
        {
            ArgumentNullException.ThrowIfNull(context);
            throw new InvalidOperationException("behavior failure");
        }
    }

    private sealed class ThrowingSink : IBehaviorOutputSink
    {
        public void Record(BehaviorExecutionRecord record)
        {
            ArgumentNullException.ThrowIfNull(record);
            throw new InvalidOperationException("sink failure");
        }
    }

    private sealed class ThrowingModeSource : ISimulationModeSource
    {
        public SimulationMode CurrentMode => throw new InvalidOperationException("mode source failure");
    }
}
