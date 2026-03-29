namespace StateKernel.Simulation.Activation;

/// <summary>
/// Represents an activation policy that always permits already-due behavior work to execute.
/// </summary>
public sealed class AlwaysActivePolicy : IBehaviorActivationPolicy
{
    /// <inheritdoc />
    public bool IsActive(BehaviorActivationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return true;
    }
}
