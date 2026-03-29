namespace StateKernel.Simulation.Activation;

/// <summary>
/// Defines a deterministic policy that decides whether already-due behavior work is active.
/// </summary>
/// <remarks>
/// Activation is evaluated only after the scheduler has already decided that work is due. It is
/// not a second timing system and does not cause execution on its own.
/// </remarks>
public interface IBehaviorActivationPolicy
{
    /// <summary>
    /// Determines whether already-due behavior work is active for the supplied execution point.
    /// </summary>
    /// <param name="context">The deterministic activation context.</param>
    /// <returns>
    /// <see langword="true" /> when the already-due behavior work is active; otherwise,
    /// <see langword="false" />.
    /// </returns>
    bool IsActive(BehaviorActivationContext context);
}
