using StateKernel.Simulation.Activation;
using StateKernel.Simulation.Behaviors;
using StateKernel.Simulation.Clock;
using StateKernel.Simulation.Context;
using StateKernel.Simulation.Modes;
using StateKernel.Simulation.Scheduling;
using StateKernel.Simulation.Signals;

namespace StateKernel.RuntimeHost.Execution;

internal sealed class BaselineConstantSourceSimulationRunDefinition : ISimulationExecutableRunDefinition
{
    private static readonly TimeSpan TickInterval = TimeSpan.FromMilliseconds(10);
    private static readonly SimulationSignalId SourceSignal = SimulationSignalId.From("Source");
    private static readonly SimulationMode RunMode = SimulationMode.From("Run");

    public BaselineConstantSourceSimulationRunDefinition()
    {
        ExposableSignals = Array.AsReadOnly(
        [
            SourceSignal,
        ]);
    }

    public SimulationRunDefinitionId Id { get; } =
        SimulationRunDefinitionId.From("baseline-constant-source");

    public string DisplayName => "Baseline Constant Source";

    public IReadOnlyList<SimulationSignalId> ExposableSignals { get; }

    public SimulationExecutionSettings DefaultExecutionSettings { get; } =
        SimulationExecutionSettings.CreateContinuous(TimeSpan.FromMilliseconds(50));

    public SimulationRunComponents CreateExecutionComponents()
    {
        var signalStore = new SimulationSignalValueStore();
        var outputSink = new BehaviorExecutionRecorder();
        var modeController = new SimulationModeController(RunMode);
        var context = SimulationContext.CreateDeterministic(
            new SimulationClockSettings(TickInterval, 8),
            StateKernel.Simulation.Seed.SimulationSeed.FromInt32(42));
        var plan = new SimulationSchedulerPlan(
        [
            new BehaviorScheduledWork(
                "source",
                ExecutionCadence.EveryTick,
                0,
                new ConstantBehavior(5.0),
                new AlwaysActivePolicy(),
                modeController,
                signalStore,
                outputSink,
                SourceSignal),
        ]);
        var scheduler = new DeterministicSimulationScheduler(context, plan);

        return new SimulationRunComponents(scheduler, signalStore);
    }
}
