namespace StateKernel.Simulation.StateMachines;

/// <summary>
/// Represents the canonical identifier for a formal simulation state.
/// </summary>
/// <remarks>
/// <c>State</c> is the formal state-machine concept in the simulation core. It remains distinct
/// from <c>Mode</c>, which is the operating concept already used by activation. State identity is
/// normalized by trimming the supplied name and comparing the stored canonical value with ordinal
/// semantics.
/// </remarks>
public sealed class SimulationStateId : IEquatable<SimulationStateId>
{
    private SimulationStateId(string value)
    {
        Value = value;
    }

    /// <summary>
    /// Gets the canonical trimmed state identifier value.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Creates a formal simulation state identifier from the supplied value.
    /// </summary>
    /// <param name="value">The state identifier value to normalize.</param>
    /// <returns>A normalized formal state identifier.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="value" /> is null, empty, or whitespace.
    /// </exception>
    public static SimulationStateId From(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        var trimmedValue = value.Trim();

        if (trimmedValue.Length == 0)
        {
            throw new ArgumentException(
                "Simulation state identifiers cannot be empty or whitespace.",
                nameof(value));
        }

        return new SimulationStateId(trimmedValue);
    }

    /// <inheritdoc />
    public bool Equals(SimulationStateId? other)
    {
        return other is not null &&
            string.Equals(Value, other.Value, StringComparison.Ordinal);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is SimulationStateId other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return StringComparer.Ordinal.GetHashCode(Value);
    }

    /// <summary>
    /// Returns the canonical string representation of the formal state identifier.
    /// </summary>
    /// <returns>The canonical trimmed formal state identifier value.</returns>
    public override string ToString()
    {
        return Value;
    }

    /// <summary>
    /// Determines whether two formal state identifiers represent the same canonical value.
    /// </summary>
    /// <param name="left">The first state identifier to compare.</param>
    /// <param name="right">The second state identifier to compare.</param>
    /// <returns>
    /// <see langword="true" /> when both identifiers are equal by ordinal comparison; otherwise,
    /// <see langword="false" />.
    /// </returns>
    public static bool operator ==(SimulationStateId? left, SimulationStateId? right)
    {
        return Equals(left, right);
    }

    /// <summary>
    /// Determines whether two formal state identifiers represent different canonical values.
    /// </summary>
    /// <param name="left">The first state identifier to compare.</param>
    /// <param name="right">The second state identifier to compare.</param>
    /// <returns>
    /// <see langword="true" /> when the identifiers are not equal by ordinal comparison;
    /// otherwise, <see langword="false" />.
    /// </returns>
    public static bool operator !=(SimulationStateId? left, SimulationStateId? right)
    {
        return !Equals(left, right);
    }
}
