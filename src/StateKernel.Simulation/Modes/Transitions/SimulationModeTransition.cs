using StateKernel.Simulation.Clock;

namespace StateKernel.Simulation.Modes.Transitions;

/// <summary>
/// Represents an applied deterministic simulation mode transition.
/// </summary>
public sealed record SimulationModeTransition
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SimulationModeTransition" /> type.
    /// </summary>
    /// <param name="tick">The completed deterministic tick that triggered the transition.</param>
    /// <param name="previousMode">The mode that was active during the completed step.</param>
    /// <param name="nextMode">The mode that became current after the transition was applied.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="previousMode" /> or <paramref name="nextMode" /> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="previousMode" /> and <paramref name="nextMode" /> are equal.
    /// </exception>
    public SimulationModeTransition(
        SimulationTick tick,
        SimulationMode previousMode,
        SimulationMode nextMode)
    {
        ArgumentNullException.ThrowIfNull(previousMode);
        ArgumentNullException.ThrowIfNull(nextMode);

        if (previousMode == nextMode)
        {
            throw new ArgumentException(
                "Applied transitions must change the current mode.",
                nameof(nextMode));
        }

        Tick = tick;
        PreviousMode = previousMode;
        NextMode = nextMode;
    }

    /// <summary>
    /// Gets the completed deterministic tick that triggered the transition.
    /// </summary>
    public SimulationTick Tick { get; }

    /// <summary>
    /// Gets the mode that was active during the completed step.
    /// </summary>
    public SimulationMode PreviousMode { get; }

    /// <summary>
    /// Gets the mode that became current after the transition was applied.
    /// </summary>
    public SimulationMode NextMode { get; }
}
