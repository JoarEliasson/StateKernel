namespace StateKernel.Runtime.Abstractions.Composition;

/// <summary>
/// Represents the intentionally small baseline defaults used during runtime composition.
/// </summary>
/// <remarks>
/// This defaults object exists only to support deterministic runtime composition rules. It is not
/// the start of a broad runtime configuration DSL. Node-id prefixes are normalized by trimming
/// surrounding whitespace and surrounding '/' characters before composition uses them.
/// </remarks>
public sealed class RuntimeCompositionDefaults
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RuntimeCompositionDefaults" /> type.
    /// </summary>
    /// <param name="nodeIdPrefix">
    /// The prefix used when default runtime node identifiers are derived. The supplied prefix is
    /// normalized by trimming surrounding whitespace and surrounding '/' characters.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="nodeIdPrefix" /> is null, empty, whitespace, or normalizes to an
    /// empty prefix.
    /// </exception>
    public RuntimeCompositionDefaults(string nodeIdPrefix)
    {
        NodeIdPrefix = NormalizeNodeIdPrefix(nodeIdPrefix);
    }

    /// <summary>
    /// Gets the baseline runtime-composition defaults.
    /// </summary>
    public static RuntimeCompositionDefaults Baseline { get; } = new("Signals");

    /// <summary>
    /// Gets the normalized prefix used for default runtime node identifiers.
    /// </summary>
    public string NodeIdPrefix { get; }

    private static string NormalizeNodeIdPrefix(string nodeIdPrefix)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeIdPrefix);

        var normalizedPrefix = nodeIdPrefix.Trim().Trim('/');

        if (normalizedPrefix.Length == 0)
        {
            throw new ArgumentException(
                "Runtime composition node-id prefixes cannot normalize to an empty value.",
                nameof(nodeIdPrefix));
        }

        return normalizedPrefix;
    }
}
