namespace StateKernel.Simulation.Behaviors;

/// <summary>
/// Represents a behavior that always produces the same deterministic value.
/// </summary>
public sealed class ConstantBehavior : IBehavior
{
    private readonly BehaviorSample sample;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConstantBehavior" /> type.
    /// </summary>
    /// <param name="value">The constant sampled value.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="value" /> is not finite.
    /// </exception>
    public ConstantBehavior(double value)
    {
        sample = new BehaviorSample(value);
    }

    /// <inheritdoc />
    public BehaviorSample Evaluate(BehaviorExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return sample;
    }
}
