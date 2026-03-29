using StateKernel.Simulation.Signals;

namespace StateKernel.Simulation.Behaviors;

/// <summary>
/// Represents a derived behavior that returns the committed value of another signal unchanged.
/// </summary>
public sealed class PassThroughFromSignalBehavior : IBehavior, ISignalDependentBehavior
{
    private readonly IReadOnlyCollection<SimulationSignalId> requiredSignalIds;

    /// <summary>
    /// Initializes a new instance of the <see cref="PassThroughFromSignalBehavior" /> type.
    /// </summary>
    /// <param name="sourceSignalId">The required upstream signal identifier.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="sourceSignalId" /> is null.
    /// </exception>
    public PassThroughFromSignalBehavior(SimulationSignalId sourceSignalId)
    {
        ArgumentNullException.ThrowIfNull(sourceSignalId);
        SourceSignalId = sourceSignalId;
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
        return upstreamValue.Sample;
    }
}
