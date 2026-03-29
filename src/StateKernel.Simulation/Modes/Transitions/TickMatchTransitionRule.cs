namespace StateKernel.Simulation.Modes.Transitions;

/// <summary>
/// Represents a transition rule that requests a target mode on one exact completed tick.
/// </summary>
/// <remarks>
/// This rule evaluates only the completed tick sequence number. It does not inspect the current
/// mode and does not cause transitions on its own.
/// </remarks>
public sealed class TickMatchTransitionRule : ISimulationModeTransitionRule
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TickMatchTransitionRule" /> type.
    /// </summary>
    /// <param name="transitionTick">The positive completed tick that should request a transition.</param>
    /// <param name="targetMode">The target mode requested when the completed tick matches.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="transitionTick" /> is not positive.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="targetMode" /> is null.
    /// </exception>
    public TickMatchTransitionRule(long transitionTick, SimulationMode targetMode)
    {
        if (transitionTick <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(transitionTick),
                "Transition ticks must be positive.");
        }

        ArgumentNullException.ThrowIfNull(targetMode);

        TransitionTick = transitionTick;
        TargetMode = targetMode;
    }

    /// <summary>
    /// Gets the positive completed tick that requests a mode change.
    /// </summary>
    public long TransitionTick { get; }

    /// <summary>
    /// Gets the target mode requested when the completed tick matches.
    /// </summary>
    public SimulationMode TargetMode { get; }

    /// <inheritdoc />
    public bool TrySelectTargetMode(SimulationModeTransitionContext context, out SimulationMode targetMode)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.CompletedTick.SequenceNumber != TransitionTick)
        {
            targetMode = null!;
            return false;
        }

        targetMode = TargetMode;
        return true;
    }
}
