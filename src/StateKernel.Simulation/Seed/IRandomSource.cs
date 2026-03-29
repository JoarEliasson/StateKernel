namespace StateKernel.Simulation.Seed;

/// <summary>
/// Defines a deterministic pseudo-random stream used by the simulation core.
/// </summary>
public interface IRandomSource
{
    /// <summary>
    /// Gets the seed used to initialize the random stream.
    /// </summary>
    SimulationSeed Seed { get; }

    /// <summary>
    /// Returns the next 32-bit signed integer in the deterministic stream.
    /// </summary>
    /// <returns>The next deterministic 32-bit signed integer.</returns>
    int NextInt32();

    /// <summary>
    /// Returns the next non-negative 32-bit integer that is less than the specified maximum.
    /// </summary>
    /// <param name="maxExclusive">The exclusive upper bound.</param>
    /// <returns>The next deterministic bounded integer.</returns>
    int NextInt32(int maxExclusive);

    /// <summary>
    /// Returns the next 32-bit integer within the specified range.
    /// </summary>
    /// <param name="minInclusive">The inclusive lower bound.</param>
    /// <param name="maxExclusive">The exclusive upper bound.</param>
    /// <returns>The next deterministic bounded integer.</returns>
    int NextInt32(int minInclusive, int maxExclusive);

    /// <summary>
    /// Returns the next deterministic floating-point value in the range [0, 1).
    /// </summary>
    /// <returns>The next deterministic floating-point value.</returns>
    double NextDouble();
}
