namespace StateKernel.Simulation.Behaviors;

/// <summary>
/// Defines a deterministic behavior that produces a sampled value for the current execution point.
/// </summary>
public interface IBehavior
{
    /// <summary>
    /// Evaluates the behavior for the supplied deterministic execution context.
    /// </summary>
    /// <param name="context">The behavior execution context.</param>
    /// <returns>The sampled behavior output for the current execution point.</returns>
    BehaviorSample Evaluate(BehaviorExecutionContext context);
}
