using StateKernel.Simulation.Behaviors;
using StateKernel.Simulation.Clock;

namespace StateKernel.Simulation.Tests;

public sealed class BehaviorTests
{
    [Fact]
    public void ConstantBehavior_ReturnsTheSameValueForEveryEvaluation()
    {
        var behavior = new ConstantBehavior(42.5);

        var originSample = behavior.Evaluate(CreateContext(0));
        var firstTickSample = behavior.Evaluate(CreateContext(1));
        var thirdTickSample = behavior.Evaluate(CreateContext(3));

        Assert.Equal(42.5, originSample.Value);
        Assert.Equal(42.5, firstTickSample.Value);
        Assert.Equal(42.5, thirdTickSample.Value);
    }

    [Fact]
    public void LinearRampBehavior_UsesAbsoluteLogicalTickProgression()
    {
        var behavior = new LinearRampBehavior(10.0, 1.5);

        var originSample = behavior.Evaluate(CreateContext(0));
        var firstTickSample = behavior.Evaluate(CreateContext(1));
        var thirdTickSample = behavior.Evaluate(CreateContext(3));

        Assert.Equal(10.0, originSample.Value);
        Assert.Equal(11.5, firstTickSample.Value);
        Assert.Equal(14.5, thirdTickSample.Value);
    }

    [Fact]
    public void BehaviorSample_RejectsNonFiniteValues()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new BehaviorSample(double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => new BehaviorSample(double.PositiveInfinity));
        Assert.Throws<ArgumentOutOfRangeException>(() => new BehaviorSample(double.NegativeInfinity));
    }

    [Fact]
    public void ConstantBehavior_RejectsNonFiniteValues()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ConstantBehavior(double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ConstantBehavior(double.PositiveInfinity));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ConstantBehavior(double.NegativeInfinity));
    }

    [Fact]
    public void LinearRampBehavior_RejectsNonFiniteParameters()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new LinearRampBehavior(double.NaN, 1.0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new LinearRampBehavior(1.0, double.PositiveInfinity));
        Assert.Throws<ArgumentOutOfRangeException>(() => new LinearRampBehavior(1.0, double.NegativeInfinity));
    }

    private static BehaviorExecutionContext CreateContext(long sequenceNumber)
    {
        var tick = new SimulationTick(sequenceNumber, TimeSpan.FromMilliseconds(sequenceNumber * 10));
        return new BehaviorExecutionContext(tick);
    }
}
