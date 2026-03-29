using StateKernel.Simulation.Signals;

namespace StateKernel.Simulation.Behaviors;

/// <summary>
/// Represents a derived behavior that adds a deterministic offset to a committed upstream signal.
/// </summary>
public sealed class OffsetFromSignalBehavior : IBehavior, ISignalDependentBehavior
{
    private readonly double offset;
    private readonly IReadOnlyCollection<SimulationSignalId> requiredSignalIds;

    /// <summary>
    /// Initializes a new instance of the <see cref="OffsetFromSignalBehavior" /> type.
    /// </summary>
    /// <param name="sourceSignalId">The required upstream signal identifier.</param>
    /// <param name="offset">The deterministic offset added to the upstream value.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="sourceSignalId" /> is null.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="offset" /> is not finite.
    /// </exception>
    public OffsetFromSignalBehavior(SimulationSignalId sourceSignalId, double offset)
    {
        ArgumentNullException.ThrowIfNull(sourceSignalId);

        if (!double.IsFinite(offset))
        {
            throw new ArgumentOutOfRangeException(
                nameof(offset),
                "Derived signal offsets must be finite.");
        }

        SourceSignalId = sourceSignalId;
        this.offset = offset;
        requiredSignalIds = Array.AsReadOnly([sourceSignalId]);
    }

    /// <summary>
    /// Gets the required upstream signal identifier.
    /// </summary>
    public SimulationSignalId SourceSignalId { get; }

    /// <inheritdoc />
    public IReadOnlyCollection<SimulationSignalId> RequiredSignalIds => requiredSignalIds;

    /// <inheritdoc />
    public BehaviorSample Evaluate(BehaviorExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var upstreamValue = context.AvailableSignals.GetRequiredValue(SourceSignalId);
        return new BehaviorSample(upstreamValue.Sample.Value + offset);
    }
}
