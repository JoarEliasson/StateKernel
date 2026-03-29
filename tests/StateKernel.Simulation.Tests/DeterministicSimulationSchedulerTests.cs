using StateKernel.Simulation.Clock;
using StateKernel.Simulation.Context;
using StateKernel.Simulation.Exceptions;
using StateKernel.Simulation.Scheduling;

namespace StateKernel.Simulation.Tests;

public sealed class DeterministicSimulationSchedulerTests
{
    [Fact]
    public void RunTicks_ExecutesDueBucketsInStableCadenceAndOrder()
    {
        var executionTrace = new List<string>();
        var scheduler = CreateScheduler(
            executionTrace,
            new WorkDefinition("every-beta", ExecutionCadence.EveryTick, 20),
            new WorkDefinition("every-alpha", ExecutionCadence.EveryTick, 10),
            new WorkDefinition("every-two", new ExecutionCadence(2), 0),
            new WorkDefinition("every-three", new ExecutionCadence(3), 0));

        var frames = scheduler.RunTicks(3);

        var expectedTrace = new[]
        {
            "1:every-alpha",
            "1:every-beta",
            "2:every-alpha",
            "2:every-beta",
            "2:every-two",
            "3:every-alpha",
            "3:every-beta",
            "3:every-three",
        };

        var secondTickKeys = new[] { "every-alpha", "every-beta", "every-two" };

        Assert.Equal(expectedTrace, executionTrace);
        Assert.Equal(secondTickKeys, frames[1].ExecutedWorkKeys);
        Assert.Equal(3, frames[2].Tick.SequenceNumber);
    }

    [Fact]
    public void EquivalentPlans_ProduceTheSameExecutionSequenceRegardlessOfRegistrationOrder()
    {
        var firstTrace = RunTrace(
            new WorkDefinition("every-beta", ExecutionCadence.EveryTick, 20),
            new WorkDefinition("every-alpha", ExecutionCadence.EveryTick, 10),
            new WorkDefinition("every-two", new ExecutionCadence(2), 0));
        var secondTrace = RunTrace(
            new WorkDefinition("every-two", new ExecutionCadence(2), 0),
            new WorkDefinition("every-alpha", ExecutionCadence.EveryTick, 10),
            new WorkDefinition("every-beta", ExecutionCadence.EveryTick, 20));

        Assert.Equal(firstTrace, secondTrace);
    }

    [Fact]
    public void Reset_RestartsTheExecutionSequenceFromTheOriginTick()
    {
        var executionTrace = new List<string>();
        var scheduler = CreateScheduler(
            executionTrace,
            new WorkDefinition("every-alpha", ExecutionCadence.EveryTick, 10),
            new WorkDefinition("every-two", new ExecutionCadence(2), 0));

        scheduler.RunTicks(2);
        var firstRunTrace = executionTrace.ToArray();

        scheduler.Reset();
        executionTrace.Clear();

        Assert.Equal(SimulationTick.Origin, scheduler.Context.CurrentTick);

        scheduler.RunTicks(2);

        Assert.Equal(firstRunTrace, executionTrace);
    }

    [Fact]
    public void Plan_RejectsDuplicateWorkKeys()
    {
        var duplicateWork = new IScheduledWork[]
        {
            new RecordingWork(new WorkDefinition("duplicate", ExecutionCadence.EveryTick, 0), new List<string>()),
            new RecordingWork(new WorkDefinition("duplicate", new ExecutionCadence(2), 1), new List<string>()),
        };

        var action = () => new SimulationSchedulerPlan(duplicateWork);

        Assert.Throws<SimulationConfigurationException>(action);
    }

    [Fact]
    public void Plan_RejectsMissingCadence()
    {
        var workItems = new IScheduledWork[]
        {
            new NullCadenceWork("missing-cadence"),
        };

        var action = () => new SimulationSchedulerPlan(workItems);

        Assert.Throws<SimulationConfigurationException>(action);
    }

    private static DeterministicSimulationScheduler CreateScheduler(
        List<string> executionTrace,
        params WorkDefinition[] definitions)
    {
        var context = SimulationContext.CreateDeterministic(
            new SimulationClockSettings(TimeSpan.FromMilliseconds(10), 8),
            StateKernel.Simulation.Seed.SimulationSeed.FromInt32(100));
        var scheduledWork = definitions
            .Select(definition => (IScheduledWork)new RecordingWork(definition, executionTrace))
            .ToArray();
        var plan = new SimulationSchedulerPlan(scheduledWork);
        return new DeterministicSimulationScheduler(context, plan);
    }

    private static string[] RunTrace(params WorkDefinition[] definitions)
    {
        var executionTrace = new List<string>();
        var scheduler = CreateScheduler(executionTrace, definitions);
        scheduler.RunTicks(4);
        return executionTrace.ToArray();
    }

    private readonly record struct WorkDefinition(string Key, ExecutionCadence Cadence, int Order);

    private sealed class RecordingWork : IScheduledWork
    {
        private readonly List<string> executionTrace;

        public RecordingWork(WorkDefinition definition, List<string> executionTrace)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(definition.Key);
            ArgumentNullException.ThrowIfNull(executionTrace);

            Key = definition.Key;
            Cadence = definition.Cadence;
            Order = definition.Order;
            this.executionTrace = executionTrace;
        }

        public string Key { get; }

        public ExecutionCadence Cadence { get; }

        public int Order { get; }

        public void Execute(SimulationContext context, SimulationTick tick)
        {
            ArgumentNullException.ThrowIfNull(context);
            executionTrace.Add($"{tick.SequenceNumber}:{Key}");
        }
    }

    private sealed class NullCadenceWork : IScheduledWork
    {
        public NullCadenceWork(string key)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);
            Key = key;
        }

        public string Key { get; }

        public ExecutionCadence Cadence => null!;

        public int Order => 0;

        public void Execute(SimulationContext context, SimulationTick tick)
        {
            ArgumentNullException.ThrowIfNull(context);
        }
    }
}
