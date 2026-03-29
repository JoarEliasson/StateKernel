using StateKernel.Benchmarks;

namespace StateKernel.Benchmarks.Tests;

public sealed class BenchmarkProfileDefinitionTests
{
    [Fact]
    public void Constructor_ComputesTheTotalIterationCount()
    {
        var profile = new BenchmarkProfileDefinition("Baseline", 2, 5, 8);

        Assert.Equal(7, profile.TotalIterations);
    }

    [Fact]
    public void Constructor_RejectsNonPositiveMeasuredIterations()
    {
        var action = () => new BenchmarkProfileDefinition("Baseline", 1, 0, 4);

        Assert.Throws<ArgumentOutOfRangeException>(action);
    }
}
