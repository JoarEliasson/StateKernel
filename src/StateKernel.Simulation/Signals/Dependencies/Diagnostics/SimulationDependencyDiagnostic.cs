using StateKernel.Simulation.Signals;

namespace StateKernel.Simulation.Signals.Dependencies.Diagnostics;

/// <summary>
/// Represents one immutable advisory timing diagnostic for a declared signal dependency.
/// </summary>
/// <remarks>
/// Diagnostics are inspection artifacts only. They do not alter runtime execution ordering,
/// signal snapshot timing, activation behavior, or state/mode sequencing.
/// </remarks>
public sealed record SimulationDependencyDiagnostic
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SimulationDependencyDiagnostic" /> type.
    /// </summary>
    /// <param name="code">The closed diagnostic code.</param>
    /// <param name="consumerWorkKey">The stable key of the consuming scheduled work.</param>
    /// <param name="requiredSignalId">The required upstream signal identifier.</param>
    /// <param name="producerWorkKey">The stable key of the producing scheduled work.</param>
    /// <param name="consumerFirstDueTick">The first due tick of the consuming scheduled work.</param>
    /// <param name="producerFirstDueTick">The first due tick of the producing scheduled work.</param>
    /// <param name="message">The stable human-readable diagnostic message.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="requiredSignalId" /> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="consumerWorkKey" />, <paramref name="producerWorkKey" />, or
    /// <paramref name="message" /> is null, empty, or whitespace.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="consumerFirstDueTick" /> or
    /// <paramref name="producerFirstDueTick" /> is not greater than zero.
    /// </exception>
    public SimulationDependencyDiagnostic(
        SimulationDependencyDiagnosticCode code,
        string consumerWorkKey,
        SimulationSignalId requiredSignalId,
        string producerWorkKey,
        long consumerFirstDueTick,
        long producerFirstDueTick,
        string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(consumerWorkKey);
        ArgumentNullException.ThrowIfNull(requiredSignalId);
        ArgumentException.ThrowIfNullOrWhiteSpace(producerWorkKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        if (consumerFirstDueTick <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(consumerFirstDueTick),
                "Consumer first due ticks must be greater than zero.");
        }

        if (producerFirstDueTick <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(producerFirstDueTick),
                "Producer first due ticks must be greater than zero.");
        }

        Code = code;
        ConsumerWorkKey = consumerWorkKey;
        RequiredSignalId = requiredSignalId;
        ProducerWorkKey = producerWorkKey;
        ConsumerFirstDueTick = consumerFirstDueTick;
        ProducerFirstDueTick = producerFirstDueTick;
        Message = message;
    }

    /// <summary>
    /// Gets the closed diagnostic code.
    /// </summary>
    public SimulationDependencyDiagnosticCode Code { get; }

    /// <summary>
    /// Gets the stable key of the consuming scheduled work.
    /// </summary>
    public string ConsumerWorkKey { get; }

    /// <summary>
    /// Gets the required upstream signal identifier.
    /// </summary>
    public SimulationSignalId RequiredSignalId { get; }

    /// <summary>
    /// Gets the stable key of the producing scheduled work.
    /// </summary>
    public string ProducerWorkKey { get; }

    /// <summary>
    /// Gets the first due tick of the consuming scheduled work.
    /// </summary>
    public long ConsumerFirstDueTick { get; }

    /// <summary>
    /// Gets the first due tick of the producing scheduled work.
    /// </summary>
    public long ProducerFirstDueTick { get; }

    /// <summary>
    /// Gets the stable human-readable diagnostic message.
    /// </summary>
    public string Message { get; }
}
