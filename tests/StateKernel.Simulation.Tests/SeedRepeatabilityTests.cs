using StateKernel.Simulation.Clock;
using StateKernel.Simulation.Context;
using StateKernel.Simulation.Seed;

namespace StateKernel.Simulation.Tests;

public sealed class SeedRepeatabilityTests
{
    [Fact]
    public void CreateStream_ReturnsRepeatableSequencesForTheSameRootSeedAndStreamName()
    {
        var seedContext = new SeedContext(SimulationSeed.FromInt32(4242));
        var firstSource = seedContext.CreateStream("scheduler");
        var secondSource = seedContext.CreateStream("scheduler");

        var firstSequence = new[]
        {
            firstSource.NextInt32(),
            firstSource.NextInt32(),
            firstSource.NextInt32(1000),
            firstSource.NextInt32(-50, 50),
        };

        var secondSequence = new[]
        {
            secondSource.NextInt32(),
            secondSource.NextInt32(),
            secondSource.NextInt32(1000),
            secondSource.NextInt32(-50, 50),
        };

        Assert.Equal(firstSequence, secondSequence);
    }

    [Fact]
    public void CreateStream_DerivesIndependentStreamsForDifferentNames()
    {
        var seedContext = new SeedContext(SimulationSeed.FromInt32(4242));
        var schedulerSource = seedContext.CreateStream("scheduler");
        var behaviorSource = seedContext.CreateStream("behavior");

        var schedulerSequence = new[]
        {
            schedulerSource.NextInt32(),
            schedulerSource.NextInt32(),
            schedulerSource.NextInt32(),
        };

        var behaviorSequence = new[]
        {
            behaviorSource.NextInt32(),
            behaviorSource.NextInt32(),
            behaviorSource.NextInt32(),
        };

        Assert.NotEqual(schedulerSequence, behaviorSequence);
    }

    [Fact]
    public void CreateDeterministic_ProducesAnOriginTickAndRepeatableRootStreams()
    {
        var firstContext = SimulationContext.CreateDeterministic(
            SimulationClockSettings.Default,
            SimulationSeed.FromInt32(9001));
        var secondContext = SimulationContext.CreateDeterministic(
            SimulationClockSettings.Default,
            SimulationSeed.FromInt32(9001));

        var firstValue = firstContext.SeedContext.CreateRootSource().NextInt32();
        var secondValue = secondContext.SeedContext.CreateRootSource().NextInt32();

        Assert.Equal(SimulationTick.Origin, firstContext.CurrentTick);
        Assert.Equal(firstValue, secondValue);
    }
}
