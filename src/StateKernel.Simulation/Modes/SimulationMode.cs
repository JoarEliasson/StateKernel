namespace StateKernel.Simulation.Modes;

/// <summary>
/// Represents the current deterministic operating mode for a simulation run.
/// </summary>
/// <remarks>
/// This is the first operating-mode abstraction in the public API. Formal state-machine state is
/// intentionally deferred to a later layer and the term <c>State</c> remains reserved for that
/// future boundary. Mode identity is normalized by trimming the supplied name and comparing the
/// stored canonical value with ordinal semantics.
/// </remarks>
public sealed class SimulationMode : IEquatable<SimulationMode>
{
    private SimulationMode(string value)
    {
        Value = value;
    }

    /// <summary>
    /// Gets the canonical trimmed mode name.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Creates a simulation mode from the supplied name.
    /// </summary>
    /// <param name="name">The mode name to normalize.</param>
    /// <returns>A normalized simulation mode.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="name" /> is null, empty, or whitespace.
    /// </exception>
    public static SimulationMode From(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var trimmedName = name.Trim();

        if (trimmedName.Length == 0)
        {
            throw new ArgumentException(
                "Simulation mode names cannot be empty or whitespace.",
                nameof(name));
        }

        return new SimulationMode(trimmedName);
    }

    /// <inheritdoc />
    public bool Equals(SimulationMode? other)
    {
        return other is not null &&
            string.Equals(Value, other.Value, StringComparison.Ordinal);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is SimulationMode other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return StringComparer.Ordinal.GetHashCode(Value);
    }

    /// <summary>
    /// Returns the canonical string representation of the simulation mode.
    /// </summary>
    /// <returns>The canonical trimmed simulation mode name.</returns>
    public override string ToString()
    {
        return Value;
    }

    /// <summary>
    /// Determines whether two simulation modes represent the same canonical mode.
    /// </summary>
    /// <param name="left">The first mode to compare.</param>
    /// <param name="right">The second mode to compare.</param>
    /// <returns>
    /// <see langword="true" /> when both modes are equal by ordinal comparison; otherwise,
    /// <see langword="false" />.
    /// </returns>
    public static bool operator ==(SimulationMode? left, SimulationMode? right)
    {
        return Equals(left, right);
    }

    /// <summary>
    /// Determines whether two simulation modes represent different canonical modes.
    /// </summary>
    /// <param name="left">The first mode to compare.</param>
    /// <param name="right">The second mode to compare.</param>
    /// <returns>
    /// <see langword="true" /> when the modes are not equal by ordinal comparison; otherwise,
    /// <see langword="false" />.
    /// </returns>
    public static bool operator !=(SimulationMode? left, SimulationMode? right)
    {
        return !Equals(left, right);
    }
}
