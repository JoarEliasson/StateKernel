namespace StateKernel.Benchmarks;

/// <summary>
/// Defines the iteration counts and concurrency used by a benchmark profile.
/// </summary>
public sealed record BenchmarkProfileDefinition
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BenchmarkProfileDefinition" /> type.
    /// </summary>
    /// <param name="name">The benchmark profile name.</param>
    /// <param name="warmupIterations">The number of warm-up iterations executed before measurement begins.</param>
    /// <param name="measuredIterations">The number of measured iterations to execute.</param>
    /// <param name="concurrentSessions">The number of concurrent sessions represented by the profile.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="name" /> is null, empty, or whitespace.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when any numeric setting is outside its supported range.
    /// </exception>
    public BenchmarkProfileDefinition(
        string name,
        int warmupIterations,
        int measuredIterations,
        int concurrentSessions)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (warmupIterations < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(warmupIterations),
                "Warm-up iterations cannot be negative.");
        }

        if (measuredIterations <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(measuredIterations),
                "Measured iterations must be greater than zero.");
        }

        if (concurrentSessions <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(concurrentSessions),
                "Concurrent sessions must be greater than zero.");
        }

        Name = name;
        WarmupIterations = warmupIterations;
        MeasuredIterations = measuredIterations;
        ConcurrentSessions = concurrentSessions;
    }

    /// <summary>
    /// Gets the benchmark profile name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the number of warm-up iterations executed before measurement begins.
    /// </summary>
    public int WarmupIterations { get; }

    /// <summary>
    /// Gets the number of measured iterations executed by the profile.
    /// </summary>
    public int MeasuredIterations { get; }

    /// <summary>
    /// Gets the number of concurrent sessions represented by the profile.
    /// </summary>
    public int ConcurrentSessions { get; }

    /// <summary>
    /// Gets the total number of iterations executed by the profile.
    /// </summary>
    public int TotalIterations => WarmupIterations + MeasuredIterations;
}
