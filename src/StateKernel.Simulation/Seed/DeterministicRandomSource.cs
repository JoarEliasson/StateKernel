namespace StateKernel.Simulation.Seed;

/// <summary>
/// Implements a deterministic pseudo-random stream using a stable project-owned algorithm.
/// </summary>
public sealed class DeterministicRandomSource : IRandomSource
{
    private const double DoubleUnit = 1.0 / 9007199254740992d;
    private ulong state;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeterministicRandomSource" /> type.
    /// </summary>
    /// <param name="seed">The seed used to initialize the stream.</param>
    public DeterministicRandomSource(SimulationSeed seed)
    {
        Seed = seed;
        state = seed.Value;
    }

    /// <inheritdoc />
    public SimulationSeed Seed { get; }

    /// <inheritdoc />
    public int NextInt32()
    {
        return unchecked((int)(NextUInt64() >> 32));
    }

    /// <inheritdoc />
    public int NextInt32(int maxExclusive)
    {
        return NextInt32(0, maxExclusive);
    }

    /// <inheritdoc />
    public int NextInt32(int minInclusive, int maxExclusive)
    {
        if (minInclusive >= maxExclusive)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxExclusive),
                "The exclusive upper bound must be greater than the inclusive lower bound.");
        }

        var range = (ulong)((long)maxExclusive - minInclusive);
        var sample = NextUInt64() % range;
        return checked((int)(minInclusive + (long)sample));
    }

    /// <inheritdoc />
    public double NextDouble()
    {
        return (NextUInt64() >> 11) * DoubleUnit;
    }

    private ulong NextUInt64()
    {
        state = unchecked(state + 0x9E3779B97F4A7C15UL);

        var mixed = state;
        mixed = unchecked((mixed ^ (mixed >> 30)) * 0xBF58476D1CE4E5B9UL);
        mixed = unchecked((mixed ^ (mixed >> 27)) * 0x94D049BB133111EBUL);
        return mixed ^ (mixed >> 31);
    }
}
