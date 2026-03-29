using StateKernel.Simulation.Clock;

namespace StateKernel.Simulation.Behaviors;

/// <summary>
/// Represents a recorded behavior output for a deterministic simulation tick.
/// </summary>
public sealed record BehaviorExecutionRecord
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BehaviorExecutionRecord" /> type.
    /// </summary>
    /// <param name="behaviorKey">The stable behavior key.</param>
    /// <param name="tick">The deterministic simulation tick that produced the sample.</param>
    /// <param name="sample">The sampled behavior output.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="behaviorKey" /> is null, empty, or whitespace.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="sample" /> is null.
    /// </exception>
    public BehaviorExecutionRecord(string behaviorKey, SimulationTick tick, BehaviorSample sample)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(behaviorKey);
        ArgumentNullException.ThrowIfNull(sample);

        BehaviorKey = behaviorKey;
        Tick = tick;
        Sample = sample;
    }

    /// <summary>
    /// Gets the stable behavior key.
    /// </summary>
    public string BehaviorKey { get; }

    /// <summary>
    /// Gets the deterministic simulation tick that produced the sample.
    /// </summary>
    public SimulationTick Tick { get; }

    /// <summary>
    /// Gets the sampled behavior output.
    /// </summary>
    public BehaviorSample Sample { get; }
}
