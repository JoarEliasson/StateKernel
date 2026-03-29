using StateKernel.Simulation.Signals;

namespace StateKernel.Runtime.Abstractions;

/// <summary>
/// Represents a single projected runtime value update keyed by source simulation signal identity.
/// </summary>
public sealed class RuntimeValueUpdate
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RuntimeValueUpdate" /> type.
    /// </summary>
    /// <param name="sourceSignalId">The source simulation signal identifier.</param>
    /// <param name="value">The projected numeric value.</param>
    /// <param name="sourceTickSequenceNumber">The deterministic source tick sequence number.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="sourceSignalId" /> is null.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="value" /> is non-finite or when
    /// <paramref name="sourceTickSequenceNumber" /> is negative.
    /// </exception>
    public RuntimeValueUpdate(
        SimulationSignalId sourceSignalId,
        double value,
        long sourceTickSequenceNumber)
    {
        ArgumentNullException.ThrowIfNull(sourceSignalId);

        if (!double.IsFinite(value))
        {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                "Runtime value updates must contain finite numeric values.");
        }

        if (sourceTickSequenceNumber < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(sourceTickSequenceNumber),
                "Source tick sequence numbers cannot be negative.");
        }

        SourceSignalId = sourceSignalId;
        Value = value;
        SourceTickSequenceNumber = sourceTickSequenceNumber;
    }

    /// <summary>
    /// Gets the source simulation signal identifier.
    /// </summary>
    public SimulationSignalId SourceSignalId { get; }

    /// <summary>
    /// Gets the projected numeric value.
    /// </summary>
    public double Value { get; }

    /// <summary>
    /// Gets the deterministic source tick sequence number.
    /// </summary>
    public long SourceTickSequenceNumber { get; }
}
