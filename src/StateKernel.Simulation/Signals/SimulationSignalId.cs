namespace StateKernel.Simulation.Signals;

/// <summary>
/// Represents the canonical identifier for a deterministic simulation signal.
/// </summary>
/// <remarks>
/// Signals identify produced simulation values independently of scheduler work keys, formal
/// states, operating modes, and runtime node identity. Signal identity is normalized by trimming
/// the supplied name and comparing the stored canonical value with ordinal semantics.
/// </remarks>
public sealed class SimulationSignalId : IEquatable<SimulationSignalId>
{
    private SimulationSignalId(string value)
    {
        Value = value;
    }

    /// <summary>
    /// Gets the canonical trimmed signal identifier value.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Creates a simulation signal identifier from the supplied value.
    /// </summary>
    /// <param name="value">The signal identifier value to normalize.</param>
    /// <returns>A normalized simulation signal identifier.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="value" /> is null, empty, or whitespace.
    /// </exception>
    public static SimulationSignalId From(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        var trimmedValue = value.Trim();

        if (trimmedValue.Length == 0)
        {
            throw new ArgumentException(
                "Simulation signal identifiers cannot be empty or whitespace.",
                nameof(value));
        }

        return new SimulationSignalId(trimmedValue);
    }

    /// <inheritdoc />
    public bool Equals(SimulationSignalId? other)
    {
        return other is not null &&
            string.Equals(Value, other.Value, StringComparison.Ordinal);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is SimulationSignalId other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return StringComparer.Ordinal.GetHashCode(Value);
    }

    /// <summary>
    /// Returns the canonical string representation of the simulation signal identifier.
    /// </summary>
    /// <returns>The canonical trimmed signal identifier value.</returns>
    public override string ToString()
    {
        return Value;
    }

    /// <summary>
    /// Determines whether two simulation signal identifiers represent the same canonical value.
    /// </summary>
    /// <param name="left">The first signal identifier to compare.</param>
    /// <param name="right">The second signal identifier to compare.</param>
    /// <returns>
    /// <see langword="true" /> when both identifiers are equal by ordinal comparison; otherwise,
    /// <see langword="false" />.
    /// </returns>
    public static bool operator ==(SimulationSignalId? left, SimulationSignalId? right)
    {
        return Equals(left, right);
    }

    /// <summary>
    /// Determines whether two simulation signal identifiers represent different canonical values.
    /// </summary>
    /// <param name="left">The first signal identifier to compare.</param>
    /// <param name="right">The second signal identifier to compare.</param>
    /// <returns>
    /// <see langword="true" /> when the identifiers are not equal by ordinal comparison;
    /// otherwise, <see langword="false" />.
    /// </returns>
    public static bool operator !=(SimulationSignalId? left, SimulationSignalId? right)
    {
        return !Equals(left, right);
    }
}
