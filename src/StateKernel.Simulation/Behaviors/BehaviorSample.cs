namespace StateKernel.Simulation.Behaviors;

/// <summary>
/// Represents a deterministic numeric behavior output.
/// </summary>
public sealed record BehaviorSample
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BehaviorSample" /> type.
    /// </summary>
    /// <param name="value">The sampled numeric value.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="value" /> is not finite.
    /// </exception>
    public BehaviorSample(double value)
    {
        if (!double.IsFinite(value))
        {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                "Behavior sample values must be finite.");
        }

        Value = value;
    }

    /// <summary>
    /// Gets the sampled numeric value.
    /// </summary>
    public double Value { get; }
}
