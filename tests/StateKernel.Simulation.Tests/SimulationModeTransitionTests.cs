using StateKernel.Simulation.Clock;
using StateKernel.Simulation.Modes;
using StateKernel.Simulation.Modes.Transitions;

namespace StateKernel.Simulation.Tests;

public sealed class SimulationModeTransitionTests
{
    private static readonly SimulationMode IdleMode = SimulationMode.From("Idle");
    private static readonly SimulationMode RunMode = SimulationMode.From("Run");

    [Fact]
    public void NeverTransitionRule_NeverSelectsATargetMode()
    {
        var rule = new NeverTransitionRule();

        var selected = rule.TrySelectTargetMode(CreateContext(1, RunMode), out _);

        Assert.False(selected);
    }

    [Fact]
    public void TickMatchTransitionRule_SelectsTheConfiguredTargetModeOnlyOnTheMatchingCompletedTick()
    {
        var rule = new TickMatchTransitionRule(2, IdleMode);

        Assert.False(rule.TrySelectTargetMode(CreateContext(1, RunMode), out _));
        Assert.True(rule.TrySelectTargetMode(CreateContext(2, RunMode), out var targetMode));
        Assert.False(rule.TrySelectTargetMode(CreateContext(3, IdleMode), out _));
        Assert.Equal(IdleMode, targetMode);
    }

    [Fact]
    public void TickMatchTransitionRule_RejectsInvalidConfiguration()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new TickMatchTransitionRule(0, RunMode));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TickMatchTransitionRule(-1, RunMode));
        Assert.Throws<ArgumentNullException>(() => new TickMatchTransitionRule(1, null!));
    }

    [Fact]
    public void TransitionCoordinator_EvaluatesTheCompletedTickAndThePreTransitionCurrentMode()
    {
        var recordingRule = new RecordingTransitionRule(2, IdleMode);
        var controller = new SimulationModeController(RunMode);
        var coordinator = new SimulationModeTransitionCoordinator(controller, recordingRule);

        var afterTickOne = coordinator.EvaluateAndApply(CreateTick(1));
        var afterTickTwo = coordinator.EvaluateAndApply(CreateTick(2));
        var appliedTransition = Assert.IsType<SimulationModeTransition>(afterTickTwo);

        Assert.Null(afterTickOne);
        Assert.Equal(["1:Run", "2:Run"], recordingRule.SeenContexts);
        Assert.Equal(2, appliedTransition.Tick.SequenceNumber);
        Assert.Equal(RunMode, appliedTransition.PreviousMode);
        Assert.Equal(IdleMode, appliedTransition.NextMode);
        Assert.Equal(IdleMode, controller.CurrentMode);
    }

    [Fact]
    public void TransitionCoordinator_TreatsSameModeSelectionsAsNoTransition()
    {
        var controller = new SimulationModeController(RunMode);
        var coordinator = new SimulationModeTransitionCoordinator(
            controller,
            new TickMatchTransitionRule(1, RunMode));

        var transition = coordinator.EvaluateAndApply(CreateTick(1));

        Assert.Null(transition);
        Assert.Equal(RunMode, controller.CurrentMode);
    }

    [Fact]
    public void TransitionCoordinator_RejectsNullDependencies()
    {
        Assert.Throws<ArgumentNullException>(() => new SimulationModeTransitionCoordinator(null!, new NeverTransitionRule()));
        Assert.Throws<ArgumentNullException>(() => new SimulationModeTransitionCoordinator(new SimulationModeController(RunMode), null!));
    }

    [Fact]
    public void TransitionRecord_RejectsEquivalentModes()
    {
        var tick = CreateTick(1);

        Assert.Throws<ArgumentException>(() => new SimulationModeTransition(tick, RunMode, RunMode));
    }

    private static SimulationModeTransitionContext CreateContext(long sequenceNumber, SimulationMode currentMode)
    {
        return new SimulationModeTransitionContext(CreateTick(sequenceNumber), currentMode);
    }

    private static SimulationTick CreateTick(long sequenceNumber)
    {
        return new SimulationTick(sequenceNumber, TimeSpan.FromMilliseconds(sequenceNumber * 10));
    }

    private sealed class RecordingTransitionRule : ISimulationModeTransitionRule
    {
        public RecordingTransitionRule(long transitionTick, SimulationMode targetMode)
        {
            if (transitionTick <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(transitionTick),
                    "Transition ticks must be positive.");
            }

            ArgumentNullException.ThrowIfNull(targetMode);

            TransitionTick = transitionTick;
            TargetMode = targetMode;
        }

        public List<string> SeenContexts { get; } = [];

        public long TransitionTick { get; }

        public SimulationMode TargetMode { get; }

        public bool TrySelectTargetMode(SimulationModeTransitionContext context, out SimulationMode targetMode)
        {
            ArgumentNullException.ThrowIfNull(context);

            SeenContexts.Add($"{context.CompletedTick.SequenceNumber}:{context.CurrentMode}");

            if (context.CompletedTick.SequenceNumber != TransitionTick)
            {
                targetMode = null!;
                return false;
            }

            targetMode = TargetMode;
            return true;
        }
    }
}
