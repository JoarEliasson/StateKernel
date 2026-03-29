namespace StateKernel.Domain.Projects;

/// <summary>
/// Represents the stable identifier assigned to a persisted StateKernel project.
/// </summary>
public sealed record ProjectId
{
    private ProjectId(Guid value)
    {
        Value = value;
    }

    /// <summary>
    /// Gets the underlying GUID value for the project identifier.
    /// </summary>
    public Guid Value { get; }

    /// <summary>
    /// Creates a new project identifier.
    /// </summary>
    /// <returns>A new non-empty project identifier.</returns>
    public static ProjectId New()
    {
        return new ProjectId(Guid.NewGuid());
    }

    /// <summary>
    /// Creates a project identifier from an existing GUID value.
    /// </summary>
    /// <param name="value">The GUID value to wrap.</param>
    /// <returns>A project identifier for the supplied GUID.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="value" /> is the empty GUID.
    /// </exception>
    public static ProjectId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Project identifiers cannot be empty.", nameof(value));
        }

        return new ProjectId(value);
    }

    /// <summary>
    /// Parses a project identifier from its string representation.
    /// </summary>
    /// <param name="value">The string representation of the project identifier.</param>
    /// <returns>A parsed project identifier.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="value" /> is null, empty, or whitespace.
    /// </exception>
    /// <exception cref="FormatException">
    /// Thrown when <paramref name="value" /> is not a valid non-empty GUID.
    /// </exception>
    public static ProjectId Parse(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        if (!Guid.TryParse(value, out var parsed) || parsed == Guid.Empty)
        {
            throw new FormatException("Project identifiers must be valid non-empty GUID values.");
        }

        return new ProjectId(parsed);
    }

    /// <summary>
    /// Returns the canonical string representation of the project identifier.
    /// </summary>
    /// <returns>The canonical string representation of the project identifier.</returns>
    public override string ToString()
    {
        return Value.ToString("D");
    }
}
