using StateKernel.Simulation.Clock;
using StateKernel.Simulation.Context;

namespace StateKernel.Simulation.Scheduling;

/// <summary>
/// Implements the first deterministic cadence-bucket scheduler for the simulation core.
/// </summary>
public sealed class DeterministicSimulationScheduler : ISimulationScheduler
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DeterministicSimulationScheduler" /> type.
    /// </summary>
    /// <param name="context">The deterministic simulation context used by the scheduler.</param>
    /// <param name="plan">The immutable scheduler plan used by the scheduler.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="context" /> or <paramref name="plan" /> is null.
    /// </exception>
    public DeterministicSimulationScheduler(SimulationContext context, SimulationSchedulerPlan plan)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(plan);

        Context = context;
        Plan = plan;
    }

    /// <inheritdoc />
    public SimulationContext Context { get; }

    /// <inheritdoc />
    public SimulationSchedulerPlan Plan { get; }

    /// <inheritdoc />
    public SchedulerExecutionFrame RunNextTick()
    {
        var tick = Context.Clock.Advance();
        return ExecuteDueBuckets(tick);
    }

    /// <inheritdoc />
    public IReadOnlyList<SchedulerExecutionFrame> RunTicks(int tickCount)
    {
        if (tickCount <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(tickCount),
                "The scheduler must run at least one tick.");
        }

        var executionFrames = new SchedulerExecutionFrame[tickCount];

        for (var index = 0; index < tickCount; index++)
        {
            executionFrames[index] = RunNextTick();
        }

        return Array.AsReadOnly(executionFrames);
    }

    /// <inheritdoc />
    public void Reset()
    {
        Context.Clock.Reset();
    }

    private SchedulerExecutionFrame ExecuteDueBuckets(SimulationTick tick)
    {
        var executedWorkKeys = new List<string>();

        foreach (var bucket in Plan.Buckets)
        {
            if (!bucket.IsDue(tick))
            {
                continue;
            }

            executedWorkKeys.AddRange(bucket.Execute(Context, tick));
        }

        return new SchedulerExecutionFrame(tick, executedWorkKeys);
    }
}
