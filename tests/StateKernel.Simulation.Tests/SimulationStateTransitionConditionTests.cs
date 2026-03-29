using StateKernel.Simulation.Clock;
using StateKernel.Simulation.StateMachines;

namespace StateKernel.Simulation.Tests;

public sealed class SimulationStateTransitionConditionTests
{
    private static readonly SimulationStateId IdleState = SimulationStateId.From("IdleState");

    [Fact]
    public void CompletedTickMatchCondition_RejectsNonPositiveTicks()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new CompletedTickMatchCondition(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new CompletedTickMatchCondition(-1));
    }

    [Fact]
    public void CompletedTickAtOrAfterCondition_RejectsNonPositiveTicks()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new CompletedTickAtOrAfterCondition(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new CompletedTickAtOrAfterCondition(-1));
    }

    [Fact]
    public void CompletedTickMatchCondition_IsEligibleOnlyOnTheMatchingCompletedTick()
    {
        var condition = new CompletedTickMatchCondition(3);

        Assert.False(condition.IsEligible(CreateContext(2)));
        Assert.True(condition.IsEligible(CreateContext(3)));
        Assert.False(condition.IsEligible(CreateContext(4)));
    }

    [Fact]
    public void CompletedTickAtOrAfterCondition_IsEligibleOnTheConfiguredAndLaterCompletedTicks()
    {
        var condition = new CompletedTickAtOrAfterCondition(3);

        Assert.False(condition.IsEligible(CreateContext(2)));
        Assert.True(condition.IsEligible(CreateContext(3)));
        Assert.True(condition.IsEligible(CreateContext(4)));
    }

    [Fact]
    public void TransitionDefinitions_RejectNullConditions()
    {
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new SimulationStateTransitionDefinition(
                IdleState,
                SimulationStateId.From("RunState"),
                null!));

        Assert.Equal("condition", exception.ParamName);
    }

    private static SimulationStateTransitionConditionContext CreateContext(long sequenceNumber)
    {
        return new SimulationStateTransitionConditionContext(
            new SimulationTick(sequenceNumber, TimeSpan.FromMilliseconds(sequenceNumber * 10)),
            IdleState);
    }
}
