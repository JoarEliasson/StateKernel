using StateKernel.Simulation.Activation;
using StateKernel.Simulation.Clock;
using StateKernel.Simulation.Context;
using StateKernel.Simulation.Modes;
using StateKernel.Simulation.Scheduling;
using StateKernel.Simulation.Signals;

namespace StateKernel.Simulation.Behaviors;

/// <summary>
/// Adapts a behavior into the generic deterministic scheduler work contract.
/// </summary>
/// <remarks>
/// This adapter intentionally preserves fail-fast behavior. The scheduler must first determine
/// that work is due before activation is evaluated. The current simulation mode is read at
/// execution time for already-due work and is not pre-bound into the scheduler plan. Exceptions
/// from reading the mode source, activation, signal snapshot access, behavior evaluation, or
/// output recording propagate directly to the scheduler caller.
/// </remarks>
public sealed class BehaviorScheduledWork : ISignalProducingWork
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BehaviorScheduledWork" /> type.
    /// </summary>
    /// <param name="key">The stable behavior work key.</param>
    /// <param name="cadence">The cadence on which the behavior should execute.</param>
    /// <param name="order">The explicit execution order within the cadence bucket.</param>
    /// <param name="behavior">The behavior to evaluate.</param>
    /// <param name="activationPolicy">The activation policy that gates already-due behavior work.</param>
    /// <param name="modeSource">The source that exposes the current deterministic simulation mode.</param>
    /// <param name="signalValueStore">
    /// The shared signal store that exposes committed upstream values and stages produced signals.
    /// </param>
    /// <param name="outputSink">The sink that records produced behavior samples.</param>
    /// <param name="producedSignalId">
    /// The optional signal identifier published by this work when it produces a sample.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="key" /> is null, empty, or whitespace.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="order" /> is negative.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="cadence" />, <paramref name="behavior" />,
    /// <paramref name="activationPolicy" />, <paramref name="modeSource" />,
    /// <paramref name="signalValueStore" />, or <paramref name="outputSink" /> is null.
    /// </exception>
    public BehaviorScheduledWork(
        string key,
        ExecutionCadence cadence,
        int order,
        IBehavior behavior,
        IBehaviorActivationPolicy activationPolicy,
        ISimulationModeSource modeSource,
        SimulationSignalValueStore signalValueStore,
        IBehaviorOutputSink outputSink,
        SimulationSignalId? producedSignalId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(cadence);
        ArgumentNullException.ThrowIfNull(behavior);
        ArgumentNullException.ThrowIfNull(activationPolicy);
        ArgumentNullException.ThrowIfNull(modeSource);
        ArgumentNullException.ThrowIfNull(signalValueStore);
        ArgumentNullException.ThrowIfNull(outputSink);

        if (order < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(order),
                "Behavior work order cannot be negative.");
        }

        Key = key;
        Cadence = cadence;
        Order = order;
        Behavior = behavior;
        ActivationPolicy = activationPolicy;
        ModeSource = modeSource;
        SignalValueStore = signalValueStore;
        OutputSink = outputSink;
        ProducedSignalId = producedSignalId;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BehaviorScheduledWork" /> type for scenarios
    /// that do not publish or consume shared signals.
    /// </summary>
    /// <param name="key">The stable behavior work key.</param>
    /// <param name="cadence">The cadence on which the behavior should execute.</param>
    /// <param name="order">The explicit execution order within the cadence bucket.</param>
    /// <param name="behavior">The behavior to evaluate.</param>
    /// <param name="activationPolicy">The activation policy that gates already-due behavior work.</param>
    /// <param name="modeSource">The source that exposes the current deterministic simulation mode.</param>
    /// <param name="outputSink">The sink that records produced behavior samples.</param>
    public BehaviorScheduledWork(
        string key,
        ExecutionCadence cadence,
        int order,
        IBehavior behavior,
        IBehaviorActivationPolicy activationPolicy,
        ISimulationModeSource modeSource,
        IBehaviorOutputSink outputSink)
        : this(
            key,
            cadence,
            order,
            behavior,
            activationPolicy,
            modeSource,
            new SimulationSignalValueStore(),
            outputSink,
            null)
    {
    }

    /// <inheritdoc />
    public string Key { get; }

    /// <inheritdoc />
    public ExecutionCadence Cadence { get; }

    /// <inheritdoc />
    public int Order { get; }

    /// <summary>
    /// Gets the adapted behavior.
    /// </summary>
    public IBehavior Behavior { get; }

    /// <summary>
    /// Gets the activation policy that gates already-due behavior work.
    /// </summary>
    public IBehaviorActivationPolicy ActivationPolicy { get; }

    /// <summary>
    /// Gets the source that exposes the current deterministic simulation mode.
    /// </summary>
    public ISimulationModeSource ModeSource { get; }

    /// <summary>
    /// Gets the shared signal store that exposes committed upstream values and stages produced signals.
    /// </summary>
    public SimulationSignalValueStore SignalValueStore { get; }

    /// <summary>
    /// Gets the optional signal identifier published by this work.
    /// </summary>
    public SimulationSignalId? ProducedSignalId { get; }

    /// <summary>
    /// Gets the sink used to capture produced behavior samples.
    /// </summary>
    public IBehaviorOutputSink OutputSink { get; }

    /// <inheritdoc />
    public void Execute(SimulationContext context, SimulationTick tick)
    {
        ArgumentNullException.ThrowIfNull(context);

        var currentMode = ModeSource.CurrentMode;
        ArgumentNullException.ThrowIfNull(currentMode);

        var activationContext = new BehaviorActivationContext(tick, currentMode);

        if (!ActivationPolicy.IsActive(activationContext))
        {
            return;
        }

        var availableSignals = SignalValueStore.GetCommittedSnapshotForTick(tick);
        var executionContext = new BehaviorExecutionContext(tick, availableSignals);
        var sample = Behavior.Evaluate(executionContext);
        OutputSink.Record(new BehaviorExecutionRecord(Key, tick, sample));

        if (ProducedSignalId is not null)
        {
            SignalValueStore.RecordProducedValue(
                new SimulationSignalValue(ProducedSignalId, tick, sample));
        }
    }
}
