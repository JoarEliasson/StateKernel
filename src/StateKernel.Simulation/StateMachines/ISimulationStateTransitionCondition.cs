namespace StateKernel.Simulation.StateMachines;

/// <summary>
/// Defines the deterministic condition contract used by formal state transitions.
/// </summary>
public interface ISimulationStateTransitionCondition
{
    /// <summary>
    /// Determines whether the transition is eligible for the supplied completed-step context.
    /// </summary>
    /// <param name="context">The completed-step context being evaluated.</param>
    /// <returns>
    /// <see langword="true" /> when the transition is eligible for the supplied context;
    /// otherwise, <see langword="false" />.
    /// </returns>
    bool IsEligible(SimulationStateTransitionConditionContext context);
}
