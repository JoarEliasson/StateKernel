using StateKernel.Simulation.Clock;
using StateKernel.Simulation.Scheduling;

namespace StateKernel.Simulation.Tests;

public sealed class ExecutionCadenceTests
{
    [Fact]
    public void IsDue_ReturnsTrueOnlyOnMatchingTicks()
    {
        var cadence = new ExecutionCadence(3);
        var originTick = SimulationTick.Origin;
        var secondTick = new SimulationTick(2, TimeSpan.FromMilliseconds(20));
        var thirdTick = new SimulationTick(3, TimeSpan.FromMilliseconds(30));

        Assert.False(cadence.IsDue(originTick));
        Assert.False(cadence.IsDue(secondTick));
        Assert.True(cadence.IsDue(thirdTick));
    }

    [Fact]
    public void Constructor_RejectsNonPositiveIntervals()
    {
        Action action = () => _ = new ExecutionCadence(0);

        Assert.Throws<ArgumentOutOfRangeException>(action);
    }
}
