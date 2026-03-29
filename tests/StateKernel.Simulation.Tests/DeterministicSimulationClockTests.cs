using StateKernel.Simulation.Clock;
using StateKernel.Simulation.Exceptions;

namespace StateKernel.Simulation.Tests;

public sealed class DeterministicSimulationClockTests
{
    [Fact]
    public void AdvanceBy_ReturnsTheExpectedLogicalTick()
    {
        var settings = new SimulationClockSettings(TimeSpan.FromMilliseconds(25), 4);
        var clock = new DeterministicSimulationClock(settings);

        var currentTick = clock.AdvanceBy(3);

        Assert.Equal(3, currentTick.SequenceNumber);
        Assert.Equal(TimeSpan.FromMilliseconds(75), currentTick.LogicalTime);
        Assert.Equal(currentTick, clock.CurrentTick);
    }

    [Fact]
    public void Reset_ReturnsTheClockToItsOriginTick()
    {
        var settings = new SimulationClockSettings(TimeSpan.FromMilliseconds(10), 8);
        var originTick = new SimulationTick(2, TimeSpan.FromMilliseconds(20));
        var clock = new DeterministicSimulationClock(settings, originTick);

        clock.AdvanceBy(2);
        clock.Reset();

        Assert.Equal(originTick, clock.CurrentTick);
        Assert.Equal(originTick, clock.OriginTick);
    }

    [Fact]
    public void Constructor_RejectsAnOriginTickThatDoesNotMatchTheTickInterval()
    {
        var settings = new SimulationClockSettings(TimeSpan.FromMilliseconds(10), 8);
        var action = () => new DeterministicSimulationClock(
            settings,
            new SimulationTick(2, TimeSpan.FromMilliseconds(25)));

        Assert.Throws<SimulationConfigurationException>(action);
    }
}
