using StateKernel.Simulation.Exceptions;

namespace StateKernel.Simulation.Clock;

/// <summary>
/// Implements a deterministic fixed-step logical clock for the simulation core.
/// </summary>
public sealed class DeterministicSimulationClock : ISimulationClock
{
    private readonly SimulationTick originTick;
    private SimulationTick currentTick;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeterministicSimulationClock" /> type.
    /// </summary>
    /// <param name="settings">The fixed-step settings used by the clock.</param>
    public DeterministicSimulationClock(SimulationClockSettings settings)
        : this(settings, SimulationTick.Origin)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DeterministicSimulationClock" /> type.
    /// </summary>
    /// <param name="settings">The fixed-step settings used by the clock.</param>
    /// <param name="originTick">The origin tick used when the clock starts or resets.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="settings" /> is null.
    /// </exception>
    /// <exception cref="SimulationConfigurationException">
    /// Thrown when <paramref name="originTick" /> is not consistent with the configured tick interval.
    /// </exception>
    public DeterministicSimulationClock(SimulationClockSettings settings, SimulationTick originTick)
    {
        ArgumentNullException.ThrowIfNull(settings);

        if (!originTick.IsConsistentWith(settings.TickInterval))
        {
            throw new SimulationConfigurationException(
                "Origin ticks must align with the configured fixed-step interval.");
        }

        Settings = settings;
        this.originTick = originTick;
        currentTick = originTick;
    }

    /// <inheritdoc />
    public SimulationClockMode Mode => SimulationClockMode.Deterministic;

    /// <inheritdoc />
    public SimulationClockSettings Settings { get; }

    /// <summary>
    /// Gets the origin tick used by the clock.
    /// </summary>
    public SimulationTick OriginTick => originTick;

    /// <inheritdoc />
    public SimulationTick CurrentTick => currentTick;

    /// <inheritdoc />
    public SimulationTick Advance()
    {
        return AdvanceBy(1);
    }

    /// <inheritdoc />
    public SimulationTick AdvanceBy(int tickCount)
    {
        if (tickCount <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(tickCount),
                "The clock must advance by at least one tick.");
        }

        if (tickCount > Settings.MaxCatchUpTicks)
        {
            throw new ArgumentOutOfRangeException(
                nameof(tickCount),
                "The requested tick burst exceeds the configured catch-up budget.");
        }

        var nextSequenceNumber = checked(currentTick.SequenceNumber + tickCount);
        var advancedLogicalTimeTicks = checked(Settings.TickInterval.Ticks * tickCount);
        var nextLogicalTimeTicks = checked(currentTick.LogicalTime.Ticks + advancedLogicalTimeTicks);
        currentTick = new SimulationTick(nextSequenceNumber, TimeSpan.FromTicks(nextLogicalTimeTicks));
        return currentTick;
    }

    /// <inheritdoc />
    public void Reset()
    {
        currentTick = originTick;
    }
}
