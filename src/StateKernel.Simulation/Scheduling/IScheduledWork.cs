using StateKernel.Simulation.Clock;
using StateKernel.Simulation.Context;

namespace StateKernel.Simulation.Scheduling;

/// <summary>
/// Defines a unit of deterministic work that can be scheduled into cadence buckets.
/// </summary>
/// <remarks>
/// This contract intentionally stays generic. It does not encode behavior-engine concepts,
/// state-machine transitions, dependency evaluation, fault semantics, or work reset behavior.
/// </remarks>
public interface IScheduledWork
{
    /// <summary>
    /// Gets the stable key for the scheduled work item.
    /// </summary>
    string Key { get; }

    /// <summary>
    /// Gets the cadence on which the work item should execute.
    /// </summary>
    ExecutionCadence Cadence { get; }

    /// <summary>
    /// Gets the explicit order used within the cadence bucket.
    /// </summary>
    int Order { get; }

    /// <summary>
    /// Executes the scheduled work for the supplied simulation tick.
    /// </summary>
    /// <param name="context">The deterministic simulation context.</param>
    /// <param name="tick">The deterministic simulation tick being executed.</param>
    void Execute(SimulationContext context, SimulationTick tick);
}
