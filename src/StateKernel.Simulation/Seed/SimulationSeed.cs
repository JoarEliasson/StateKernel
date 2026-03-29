namespace StateKernel.Simulation.Seed;

/// <summary>
/// Represents the root deterministic seed for a simulation run or stream.
/// </summary>
public readonly record struct SimulationSeed
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SimulationSeed" /> type.
    /// </summary>
    /// <param name="value">The deterministic seed value.</param>
    public SimulationSeed(ulong value)
    {
        Value = value;
    }

    /// <summary>
    /// Gets the raw deterministic seed value.
    /// </summary>
    public ulong Value { get; }

    /// <summary>
    /// Creates a seed from a non-negative 32-bit integer.
    /// </summary>
    /// <param name="value">The integer seed value.</param>
    /// <returns>A deterministic simulation seed.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="value" /> is negative.
    /// </exception>
    public static SimulationSeed FromInt32(int value)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Seed values cannot be negative.");
        }

        return new SimulationSeed((ulong)value);
    }
}
