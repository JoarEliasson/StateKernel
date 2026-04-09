namespace StateKernel.RuntimeHost.Execution;

/// <summary>
/// Represents the canonical identifier for a simulation run instance.
/// </summary>
public sealed class SimulationRunId : IEquatable<SimulationRunId>
{
    private SimulationRunId(string value)
    {
        Value = value;
    }

    /// <summary>
    /// Gets the canonical trimmed run identifier value.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Creates a simulation run identifier from the supplied value.
    /// </summary>
    /// <param name="value">The run identifier value to normalize.</param>
    /// <returns>A normalized simulation run identifier.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="value" /> is null, empty, or whitespace.
    /// </exception>
    public static SimulationRunId From(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        var trimmedValue = value.Trim();

        if (trimmedValue.Length == 0)
        {
            throw new ArgumentException(
                "Simulation run identifiers cannot be empty or whitespace.",
                nameof(value));
        }

        return new SimulationRunId(trimmedValue);
    }

    /// <summary>
    /// Creates a new simulation run identifier suitable for a fresh active run.
    /// </summary>
    /// <returns>A new simulation run identifier.</returns>
    public static SimulationRunId CreateNew()
    {
        return new SimulationRunId(Guid.NewGuid().ToString("N"));
    }

    /// <inheritdoc />
    public bool Equals(SimulationRunId? other)
    {
        return other is not null &&
            string.Equals(Value, other.Value, StringComparison.Ordinal);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is SimulationRunId other && Equals(other);
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
    /// Determines whether two simulation run identifiers represent the same canonical value.
    /// </summary>
    public static bool operator ==(SimulationRunId? left, SimulationRunId? right)
    {
        return Equals(left, right);
    }

    /// <summary>
    /// Determines whether two simulation run identifiers represent different canonical values.
    /// </summary>
    public static bool operator !=(SimulationRunId? left, SimulationRunId? right)
    {
        return !Equals(left, right);
    }
}
