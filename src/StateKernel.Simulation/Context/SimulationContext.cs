using StateKernel.Simulation.Clock;
using StateKernel.Simulation.Seed;

namespace StateKernel.Simulation.Context;

/// <summary>
/// Captures the deterministic clock and seed boundaries used by a simulation run.
/// </summary>
public sealed class SimulationContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SimulationContext" /> type.
    /// </summary>
    /// <param name="clock">The deterministic simulation clock.</param>
    /// <param name="seedContext">The deterministic seed context.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="clock" /> or <paramref name="seedContext" /> is null.
    /// </exception>
    public SimulationContext(ISimulationClock clock, SeedContext seedContext)
    {
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(seedContext);

        Clock = clock;
        SeedContext = seedContext;
    }

    /// <summary>
    /// Gets the deterministic logical clock.
    /// </summary>
    public ISimulationClock Clock { get; }

    /// <summary>
    /// Gets the deterministic seed context.
    /// </summary>
    public SeedContext SeedContext { get; }

    /// <summary>
    /// Gets the current deterministic simulation tick.
    /// </summary>
    public SimulationTick CurrentTick => Clock.CurrentTick;

    /// <summary>
    /// Creates a deterministic simulation context with the supplied settings and root seed.
    /// </summary>
    /// <param name="clockSettings">The fixed-step clock settings.</param>
    /// <param name="rootSeed">The root seed for deterministic random streams.</param>
    /// <returns>A deterministic simulation context.</returns>
    public static SimulationContext CreateDeterministic(
        SimulationClockSettings clockSettings,
        SimulationSeed rootSeed)
    {
        return new SimulationContext(
            new DeterministicSimulationClock(clockSettings),
            new SeedContext(rootSeed));
    }
}
