using StateKernel.Simulation.Signals;

namespace StateKernel.Runtime.Abstractions;

/// <summary>
/// Represents the canonical runtime-facing relative identifier for one exposed runtime node.
/// </summary>
public sealed class RuntimeNodeId : IEquatable<RuntimeNodeId>
{
    private RuntimeNodeId(string value)
    {
        Value = value;
    }

    /// <summary>
    /// Gets the canonical trimmed runtime node identifier value.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Creates a runtime node identifier from the supplied canonical relative identifier.
    /// </summary>
    /// <param name="value">The runtime node identifier value to normalize.</param>
    /// <returns>A normalized runtime node identifier.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="value" /> is null, empty, or whitespace.
    /// </exception>
    public static RuntimeNodeId From(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        var trimmedValue = value.Trim();

        if (trimmedValue.Length == 0)
        {
            throw new ArgumentException(
                "Runtime node identifiers cannot be empty or whitespace.",
                nameof(value));
        }

        return new RuntimeNodeId(trimmedValue);
    }

    /// <summary>
    /// Creates the baseline runtime node identifier for the supplied simulation signal.
    /// </summary>
    /// <param name="signalId">The source simulation signal identifier.</param>
    /// <returns>The baseline runtime node identifier for the signal.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="signalId" /> is null.
    /// </exception>
    public static RuntimeNodeId ForSignal(SimulationSignalId signalId)
    {
        ArgumentNullException.ThrowIfNull(signalId);
        return From($"Signals/{signalId.Value}");
    }

    /// <inheritdoc />
    public bool Equals(RuntimeNodeId? other)
    {
        return other is not null &&
            string.Equals(Value, other.Value, StringComparison.Ordinal);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is RuntimeNodeId other && Equals(other);
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
    /// Determines whether two runtime node identifiers represent the same canonical value.
    /// </summary>
    public static bool operator ==(RuntimeNodeId? left, RuntimeNodeId? right)
    {
        return Equals(left, right);
    }

    /// <summary>
    /// Determines whether two runtime node identifiers represent different canonical values.
    /// </summary>
    public static bool operator !=(RuntimeNodeId? left, RuntimeNodeId? right)
    {
        return !Equals(left, right);
    }
}
