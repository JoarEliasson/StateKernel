namespace StateKernel.Runtime.Abstractions;

/// <summary>
/// Represents the canonical identifier for a baseline runtime endpoint/security profile.
/// </summary>
public sealed class RuntimeEndpointProfileId : IEquatable<RuntimeEndpointProfileId>
{
    private RuntimeEndpointProfileId(string value)
    {
        Value = value;
    }

    /// <summary>
    /// Gets the canonical trimmed runtime endpoint/profile identifier value.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Creates a runtime endpoint/profile identifier from the supplied value.
    /// </summary>
    /// <param name="value">The profile identifier value to normalize.</param>
    /// <returns>A normalized runtime endpoint/profile identifier.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="value" /> is null, empty, or whitespace.
    /// </exception>
    public static RuntimeEndpointProfileId From(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        var trimmedValue = value.Trim();

        if (trimmedValue.Length == 0)
        {
            throw new ArgumentException(
                "Runtime endpoint/profile identifiers cannot be empty or whitespace.",
                nameof(value));
        }

        return new RuntimeEndpointProfileId(trimmedValue);
    }

    /// <inheritdoc />
    public bool Equals(RuntimeEndpointProfileId? other)
    {
        return other is not null &&
            string.Equals(Value, other.Value, StringComparison.Ordinal);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is RuntimeEndpointProfileId other && Equals(other);
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
    /// Determines whether two runtime endpoint/profile identifiers represent the same canonical value.
    /// </summary>
    public static bool operator ==(RuntimeEndpointProfileId? left, RuntimeEndpointProfileId? right)
    {
        return Equals(left, right);
    }

    /// <summary>
    /// Determines whether two runtime endpoint/profile identifiers represent different canonical values.
    /// </summary>
    public static bool operator !=(RuntimeEndpointProfileId? left, RuntimeEndpointProfileId? right)
    {
        return !Equals(left, right);
    }
}
