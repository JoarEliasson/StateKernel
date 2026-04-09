namespace StateKernel.RuntimeHost.Execution;

/// <summary>
/// Represents the canonical identifier for an executable simulation run definition.
/// </summary>
public sealed class SimulationRunDefinitionId : IEquatable<SimulationRunDefinitionId>
{
    private SimulationRunDefinitionId(string value)
    {
        Value = value;
    }

    /// <summary>
    /// Gets the canonical trimmed run-definition identifier value.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Creates a run-definition identifier from the supplied value.
    /// </summary>
    /// <param name="value">The run-definition identifier value to normalize.</param>
    /// <returns>A normalized run-definition identifier.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="value" /> is null, empty, or whitespace.
    /// </exception>
    public static SimulationRunDefinitionId From(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        var trimmedValue = value.Trim();

        if (trimmedValue.Length == 0)
        {
            throw new ArgumentException(
                "Simulation run-definition identifiers cannot be empty or whitespace.",
                nameof(value));
        }

        return new SimulationRunDefinitionId(trimmedValue);
    }

    /// <inheritdoc />
    public bool Equals(SimulationRunDefinitionId? other)
    {
        return other is not null &&
            string.Equals(Value, other.Value, StringComparison.Ordinal);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is SimulationRunDefinitionId other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return StringComparer.Ordinal.GetHashCode(Value);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Value;
    }

    /// <summary>
    /// Determines whether two run-definition identifiers represent the same canonical value.
    /// </summary>
    public static bool operator ==(SimulationRunDefinitionId? left, SimulationRunDefinitionId? right)
    {
        return Equals(left, right);
    }

    /// <summary>
    /// Determines whether two run-definition identifiers represent different canonical values.
    /// </summary>
    public static bool operator !=(SimulationRunDefinitionId? left, SimulationRunDefinitionId? right)
    {
        return !Equals(left, right);
    }
}
