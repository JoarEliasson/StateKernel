using StateKernel.Simulation.Modes;

namespace StateKernel.Simulation.Activation;

/// <summary>
/// Represents an activation policy that permits already-due behavior work only for one simulation mode.
/// </summary>
public sealed class ModeMatchActivationPolicy : IBehaviorActivationPolicy
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ModeMatchActivationPolicy" /> type.
    /// </summary>
    /// <param name="requiredMode">The mode required for activation.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="requiredMode" /> is null.
    /// </exception>
    public ModeMatchActivationPolicy(SimulationMode requiredMode)
    {
        ArgumentNullException.ThrowIfNull(requiredMode);
        RequiredMode = requiredMode;
    }

    /// <summary>
    /// Gets the mode required for activation.
    /// </summary>
    public SimulationMode RequiredMode { get; }

    /// <inheritdoc />
    public bool IsActive(BehaviorActivationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.CurrentMode == RequiredMode;
    }
}
