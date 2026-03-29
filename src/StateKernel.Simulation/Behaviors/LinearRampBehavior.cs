namespace StateKernel.Simulation.Behaviors;

/// <summary>
/// Represents a deterministic linear ramp anchored to the absolute logical tick number.
/// </summary>
/// <remarks>
/// The ramp value is defined as <c>startValue + (currentTick.SequenceNumber * stepPerTick)</c>.
/// Tick zero is the origin baseline, and cadence affects only when the ramp is sampled through
/// the scheduler rather than changing the underlying ramp function itself.
/// </remarks>
public sealed class LinearRampBehavior : IBehavior
{
    private readonly double startValue;
    private readonly double stepPerTick;

    /// <summary>
    /// Initializes a new instance of the <see cref="LinearRampBehavior" /> type.
    /// </summary>
    /// <param name="startValue">The value at the origin tick.</param>
    /// <param name="stepPerTick">The amount added for each absolute logical tick.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="startValue" /> or <paramref name="stepPerTick" /> is not finite.
    /// </exception>
    public LinearRampBehavior(double startValue, double stepPerTick)
    {
        ValidateFinite(startValue, nameof(startValue));
        ValidateFinite(stepPerTick, nameof(stepPerTick));

        this.startValue = startValue;
        this.stepPerTick = stepPerTick;
    }

    /// <inheritdoc />
    public BehaviorSample Evaluate(BehaviorExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var value = startValue + (context.CurrentTick.SequenceNumber * stepPerTick);
        return new BehaviorSample(value);
    }

    private static void ValidateFinite(double value, string parameterName)
    {
        if (!double.IsFinite(value))
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                "Linear ramp parameters must be finite.");
        }
    }
}
