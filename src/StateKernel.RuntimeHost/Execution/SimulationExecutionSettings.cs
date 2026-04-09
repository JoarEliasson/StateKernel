namespace StateKernel.RuntimeHost.Execution;

/// <summary>
/// Captures the bounded execution settings used by a simulation run.
/// </summary>
public sealed class SimulationExecutionSettings
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SimulationExecutionSettings" /> type.
    /// </summary>
    /// <param name="mode">The execution mode to use.</param>
    /// <param name="loopDelay">
    /// The delay between loop iterations when <paramref name="mode" /> is
    /// <see cref="SimulationExecutionMode.Continuous" />.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown when the supplied loop delay does not match the selected execution mode.
    /// </exception>
    public SimulationExecutionSettings(
        SimulationExecutionMode mode,
        TimeSpan? loopDelay)
    {
        if (mode == SimulationExecutionMode.Manual)
        {
            if (loopDelay is not null)
            {
                throw new ArgumentException(
                    "Manual execution settings cannot define a loop delay.",
                    nameof(loopDelay));
            }
        }
        else if (loopDelay is null || loopDelay <= TimeSpan.Zero)
        {
            throw new ArgumentException(
                "Continuous execution settings require a positive loop delay.",
                nameof(loopDelay));
        }

        Mode = mode;
        LoopDelay = loopDelay;
    }

    /// <summary>
    /// Gets the default manual execution settings.
    /// </summary>
    public static SimulationExecutionSettings Manual { get; } = new(
        SimulationExecutionMode.Manual,
        null);

    /// <summary>
    /// Gets the selected execution mode.
    /// </summary>
    public SimulationExecutionMode Mode { get; }

    /// <summary>
    /// Gets the loop delay used by continuous runs.
    /// </summary>
    public TimeSpan? LoopDelay { get; }

    /// <summary>
    /// Creates continuous execution settings with the supplied loop delay.
    /// </summary>
    /// <param name="loopDelay">The positive loop delay to use between iterations.</param>
    /// <returns>Continuous execution settings.</returns>
    public static SimulationExecutionSettings CreateContinuous(TimeSpan loopDelay)
    {
        return new SimulationExecutionSettings(
            SimulationExecutionMode.Continuous,
            loopDelay);
    }
}
