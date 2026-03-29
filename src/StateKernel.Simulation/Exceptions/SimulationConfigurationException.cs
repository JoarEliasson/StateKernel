namespace StateKernel.Simulation.Exceptions;

/// <summary>
/// Represents an invalid configuration for a simulation runtime component.
/// </summary>
public sealed class SimulationConfigurationException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SimulationConfigurationException" /> type.
    /// </summary>
    /// <param name="message">The exception message.</param>
    public SimulationConfigurationException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SimulationConfigurationException" /> type.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="innerException">The inner exception.</param>
    public SimulationConfigurationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
