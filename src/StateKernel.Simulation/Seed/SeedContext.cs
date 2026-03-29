namespace StateKernel.Simulation.Seed;

/// <summary>
/// Provides deterministic seed derivation for the simulation core.
/// </summary>
public sealed class SeedContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SeedContext" /> type.
    /// </summary>
    /// <param name="rootSeed">The root simulation seed.</param>
    public SeedContext(SimulationSeed rootSeed)
    {
        RootSeed = rootSeed;
    }

    /// <summary>
    /// Gets the root seed for the simulation.
    /// </summary>
    public SimulationSeed RootSeed { get; }

    /// <summary>
    /// Creates a deterministic random stream for the root simulation seed.
    /// </summary>
    /// <returns>A deterministic random source for the root seed.</returns>
    public IRandomSource CreateRootSource()
    {
        return new DeterministicRandomSource(RootSeed);
    }

    /// <summary>
    /// Derives a stable child seed for a named simulation stream.
    /// </summary>
    /// <param name="streamName">The stable logical stream name.</param>
    /// <returns>A deterministic child seed for the requested stream.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="streamName" /> is null, empty, or whitespace.
    /// </exception>
    public SimulationSeed DeriveStreamSeed(string streamName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(streamName);

        return new SimulationSeed(unchecked(RootSeed.Value ^ ComputeStableHash(streamName)));
    }

    /// <summary>
    /// Creates a deterministic random stream for the specified logical stream name.
    /// </summary>
    /// <param name="streamName">The stable logical stream name.</param>
    /// <returns>A deterministic random source for the requested stream.</returns>
    public IRandomSource CreateStream(string streamName)
    {
        return new DeterministicRandomSource(DeriveStreamSeed(streamName));
    }

    private static ulong ComputeStableHash(string value)
    {
        const ulong offsetBasis = 14695981039346656037UL;
        const ulong prime = 1099511628211UL;

        var hash = offsetBasis;

        foreach (var character in value)
        {
            hash ^= character;
            hash *= prime;
        }

        return hash;
    }
}
