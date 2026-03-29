# Simulation Foundation

StateKernel's simulation core starts with a deterministic logical clock, an explicit seed boundary, and a minimal context object that keeps runtime-agnostic execution concerns isolated from the rest of the system.

## Scope

This foundation layer intentionally covers only the minimum required primitives for predictable execution:

- a fixed-step deterministic clock
- a stable tick model
- explicit seed handling
- deterministic random stream derivation
- ordered cadence-bucket scheduling
- baseline deterministic behavior evaluation
- baseline deterministic behavior activation gating
- baseline deterministic simulation mode control
- baseline deterministic simulation mode transitions
- baseline deterministic formal state-machine evaluation
- baseline deterministic formal state transition conditions
- baseline deterministic signal and derived-value evaluation
- baseline deterministic declared signal dependency planning
- baseline deterministic timing-aware dependency diagnostics
- a minimal simulation context for composition

State machines, richer transition graphs, dependency graphs, fault overlays, and runtime projection remain future milestones.

## Modules

### Clock

`StateKernel.Simulation.Clock` contains:

- `SimulationTick` for the logical tick number and elapsed logical time
- `SimulationClockSettings` for fixed-step configuration
- `ISimulationClock` as the injectable clock boundary
- `DeterministicSimulationClock` as the first concrete implementation

The clock advances only through explicit calls and never reads wall-clock time.

### Seed

`StateKernel.Simulation.Seed` contains:

- `SimulationSeed` as the root seed value object
- `IRandomSource` as the deterministic random boundary
- `DeterministicRandomSource` as the project-owned pseudo-random implementation
- `SeedContext` for deriving stable named streams from a root seed

Named stream derivation allows later subsystems such as the scheduler, behaviors, and fault injection to use independent deterministic streams without relying on hidden global state.

### Context

`StateKernel.Simulation.Context` contains:

- `SimulationContext` for composing a clock and seed context into one simulation-facing object

This keeps the baseline simulation dependencies explicit while staying small enough to evolve cleanly with the later scheduler and execution model.

### Scheduling

`StateKernel.Simulation.Scheduling` contains:

- `ExecutionCadence` for fixed tick-based execution frequency
- `IScheduledWork` for scheduled work contracts
- `OrderedWorkBucket` for cadence grouping plus explicit ordering
- `SimulationSchedulerPlan` for immutable bucket planning
- `ISimulationScheduler` and `DeterministicSimulationScheduler` for deterministic orchestration
- `SchedulerExecutionFrame` for execution snapshots

The scheduler advances only from the deterministic simulation clock. It uses explicit cadence buckets and stable work ordering rather than registration order, wall-clock timers, or task-per-node execution.

Cadence due rules are explicit: tick zero never executes work, and a cadence is due only when the
current positive tick sequence number is evenly divisible by the cadence interval. Reset returns
the scheduler to the origin tick but does not rewind arbitrary mutable state inside scheduled work
implementations.

### Behaviors

`StateKernel.Simulation.Behaviors` contains:

- `IBehavior` for deterministic behavior contracts
- `BehaviorExecutionContext` for the minimal behavior-facing context
- `BehaviorSample` for deterministic numeric outputs
- `ConstantBehavior` and `LinearRampBehavior` as the first concrete behaviors
- `PassThroughFromSignalBehavior` and `OffsetFromSignalBehavior` as the first derived-value behaviors
- `ISignalDependentBehavior` for planning-only declared upstream signal requirements
- `BehaviorScheduledWork` for adapting behaviors into the generic scheduler
- `BehaviorExecutionRecord`, `IBehaviorOutputSink`, and `BehaviorExecutionRecorder` for lightweight output capture

The first behavior slice is intentionally narrow. Behaviors evaluate against the current logical
tick and a read-only committed signal snapshot, then produce a numeric `double` sample. The
scheduler remains generic, and behaviors enter the execution path through an adapter rather than
behavior-specific scheduler logic.

Linear ramp semantics are explicit. The ramp is anchored to the absolute logical tick and is
defined as `startValue + (currentTick.SequenceNumber * stepPerTick)`. Tick zero is the origin
baseline. The first due execution at tick one therefore produces `startValue + stepPerTick`.
Cadence changes when the ramp is sampled, but it does not change the underlying ramp function.

Behavior execution is fail-fast in this baseline slice. Exceptions from behavior evaluation or
output recording propagate directly to the scheduler caller without retries, isolation, or rollback.

### Signals

`StateKernel.Simulation.Signals` contains:

- `SimulationSignalId` as the canonical identifier for deterministic produced values
- `SimulationSignalValue` as the produced value record keyed by signal id
- `SimulationSignalSnapshot` as the read-only committed signal-read surface
- `SimulationSignalValueStore` as the narrow run-scoped committed-snapshot store
- `ISignalProducingWork` as the optional scheduler-validation seam for published signals

Signals are runtime-agnostic produced-value identities. They are intentionally separate from
scheduler work keys, formal states, operating modes, and OPC UA node identity.

Derived-value reads use the latest committed prior-step snapshot only. All behaviors executing on
tick `N` read the same committed snapshot, and values produced during tick `N` are staged but never
become visible during tick `N`.

The committed snapshot uses hold-last-committed-value semantics. If no new value is produced for a
signal on later ticks, the latest committed value for that signal remains available until a later
produced tick replaces it. When multiple signals are produced on one tick, they become visible
together on the next later tick.

Duplicate signal producers are rejected by published signal id during scheduler-plan validation.
That validation uses a narrow optional capability seam and does not widen the generic scheduler
contracts with signal-specific members.

### Signal Dependency Planning

`StateKernel.Simulation.Signals.Dependencies` contains:

- `SimulationDependencyPlanner` as the narrow declared-dependency discovery and validation seam
- `SimulationDependencyPlan` as the immutable planning and inspection artifact
- `SimulationPublishedSignal` as the discovered published-signal record
- `SimulationSignalDependencyBinding` as the resolved declared dependency record

Declared signal dependencies live on behaviors through `ISignalDependentBehavior`, not on generic
scheduler contracts. The baseline planner consumes an already validated `SimulationSchedulerPlan`,
discovers published signals through `ISignalProducingWork`, and discovers declared dependencies only
from `BehaviorScheduledWork` instances whose adapted behavior implements
`ISignalDependentBehavior`.

Baseline validation is intentionally narrow. It checks that declared required signal identifiers are
stable metadata and that each declared dependency references a known published signal somewhere in
the scheduler plan. It does not drive execution ordering and does not change prior-step committed
snapshot timing, same-tick read unavailability, activation timing, or state/mode semantics.

Graph analysis, cycle handling, first-tick availability checks, cadence compatibility checks,
timing validation, and propagation planning are intentionally deferred.

### Dependency Diagnostics

`StateKernel.Simulation.Signals.Dependencies.Diagnostics` contains:

- `SimulationDependencyDiagnosticCode` as the closed baseline timing-diagnostic code set
- `SimulationDependencyDiagnostic` as the immutable advisory timing-diagnostic record
- `SimulationDependencyDiagnosticsReport` as the immutable report artifact
- `SimulationDependencyDiagnosticsAnalyzer` as the external first-need timing analyzer

The diagnostics seam is layered on top of `SimulationSchedulerPlan` and `SimulationDependencyPlan`.
It remains external to scheduler execution, signal-store behavior, activation, and state/mode
control.

The baseline analyzer reasons only about the first consumer due tick under the current scheduler
cadence rules: tick zero is never due, and a cadence is due only when the positive tick sequence
number is evenly divisible by the cadence interval. Under that baseline model, the first due tick
for a work item equals its cadence interval.

Diagnostics are advisory only. They report suspicious or unsatisfied dependencies at the first
consumer need under the prior-step committed-snapshot model. A diagnostic does not necessarily mean
that the dependency can never be satisfied later; later recovery may still be possible on later
consumer ticks.

The current analyzer intentionally checks only two clearly justified timing cases:

- the producer first publishes on the same tick as the consumer's first need
- the producer's first publish occurs after the consumer's first need

Graph analysis, cycle detection, activation reachability analysis, state-machine reachability
analysis, broader timing simulation, and propagation planning remain deferred.

### Activation

`StateKernel.Simulation.Activation` contains:

- `BehaviorActivationContext` for the minimal activation-facing context
- `IBehaviorActivationPolicy` for activation contracts
- `AlwaysActivePolicy` as the baseline policy
- `TickRangeActivationPolicy` for inclusive tick-range activation
- `ModeMatchActivationPolicy` for current-mode activation

Activation is evaluated only after work is already due. It is not a second timing system and does
not cause execution on its own. The scheduler decides whether work is due, activation decides
whether already-due work is allowed to execute, and behavior evaluation produces a value only when
activation allows execution.

Inactive due work is treated as a non-executed behavior evaluation. It produces no behavior sample
and no output record.

`TickRangeActivationPolicy` is inclusive. A range such as `0..0` can treat tick zero as active by
policy, but activation alone still does not trigger origin execution because the scheduler does not
invoke work at tick zero.

`BehaviorActivationContext` intentionally exposes only the current tick and current simulation mode
in this slice. It should widen later only when a concrete deterministic activation policy requires
additional state.

### Modes

`StateKernel.Simulation.Modes` contains:

- `SimulationMode` as the first deterministic operating-mode abstraction
- `ISimulationModeSource` as the read-only current-mode contract
- `SimulationModeController` as the explicit baseline control surface

`SimulationMode` is a canonical value object. Input is trimmed before storage and equality uses
ordinal semantics on the stored value.

Current mode is a deterministic first-class concept, but it is not part of scheduler timing or
scheduler planning. `BehaviorScheduledWork` reads the live current mode only at execution time for
already-due work and passes that value into activation. Activation may therefore depend on the mode
that is current at the moment the due work is evaluated.

Reading `CurrentMode` is part of the fail-fast activation path. If the mode source throws, the
exception propagates directly to the scheduler caller. There is no fallback mode or recovery logic
in this baseline slice.

`SimulationModeController` is intentionally narrow and run-scoped. In this baseline slice it is a
deterministic single-threaded control seam, not a concurrent shared-state abstraction. It is not a
transition engine, a scheduler participant, a mode-history store, or a transition validation
component. It exists only to expose and update the current mode deterministically.

### Mode Transitions

`StateKernel.Simulation.Modes.Transitions` contains:

- `SimulationModeTransitionContext` for the minimal transition-facing context
- `ISimulationModeTransitionRule` for deterministic transition selection
- `NeverTransitionRule` as the baseline no-op rule
- `TickMatchTransitionRule` for one exact completed-tick transition
- `SimulationModeTransition` for applied transition records only
- `SimulationModeTransitionCoordinator` for post-step rule evaluation and mode updates

The transition coordinator is intentionally narrow, run-scoped, and single-threaded. It is not a
scheduler, a transition graph engine, a multi-rule arbiter, a history store, or a reset or
lifecycle manager.

Transition evaluation happens after a completed scheduler step. Rules are evaluated against the
completed tick and the pre-transition current mode that was active during that completed step. If a
rule selects a different target mode, the coordinator updates the current mode immediately after
evaluation, and that updated mode becomes visible to activation on the next scheduler step.

Same-mode target selections are treated as no transition. No transition record is produced unless
an actual mode change occurs.

`RunTicks()` remains intentionally transition-unaware in this baseline slice. Callers that need
transition-aware sequencing must step explicitly with `RunNextTick()` and then call
`EvaluateAndApply(frame.Tick)`. A composed step runner, scheduled mode-change work, and richer
transition graphs are intentionally deferred.

### State Machines

`StateKernel.Simulation.StateMachines` contains:

- `SimulationStateId` as the canonical identifier for formal state-machine state
- `SimulationStateDefinition` for minimal state definitions
- `ISimulationStateTransitionCondition` and `SimulationStateTransitionConditionContext` for the narrow deterministic transition-condition seam
- `CompletedTickMatchCondition` and `CompletedTickAtOrAfterCondition` as the first concrete condition types
- `SimulationStateTransitionDefinition` for source state, target state, and one explicit transition condition
- `SimulationStateMachineDefinition` for immutable state and transition definitions
- `SimulationStateModeMap` for the explicit total `State -> Mode` relationship
- `SimulationStateMachineContext` for post-step formal state evaluation
- `SimulationStateTransition` for applied formal state changes
- `SimulationStateMachineCoordinator` for deterministic post-step state evaluation and mapped mode updates

`State` is now the formal state-machine concept. `Mode` remains the operating concept used by
activation. The two concepts are intentionally separate, and the baseline relationship between them
is owned by an explicit total `State -> Mode` map rather than by embedding mode inside the state
identifier itself.

Formal state-machine evaluation happens after a completed scheduler step. Transition definitions are
evaluated against the completed tick and the pre-transition formal state that was active during that
completed step. Tick `N` therefore executes under the state and mode that were current when step
`N` began, and any resulting formal state change affects tick `N + 1`.

The formal state-machine layer now includes a narrow deterministic transition-condition seam.
Conditions are evaluated only inside the formal state-machine layer and remain outside the scheduler,
behavior, and activation layers. The baseline condition set is intentionally small: exact
completed-tick matching and completed-tick-at-or-after matching.

The baseline condition-driven model supports at most one transition definition per source state.
That keeps transition selection deterministic without introducing multi-rule conflict handling or a
broader guard/DSL system in this slice.

The state-machine coordinator is intentionally narrow, run-scoped, and single-threaded. It is not a
scheduler, a history store, a multi-machine orchestrator, a graph editor model, or a general
transition DSL engine.

Entering a target formal state causes the coordinator to derive the target state's mapped mode and
update the mode controller only when the mapped mode actually differs. Formal state changes may
therefore occur without a mode change when two distinct states map to the same mode. In that case
the formal state still changes, no mode mutation occurs, and activation behavior remains unchanged
because activation continues to read the same current mode on the next scheduler step.

## Runtime Boundary

The simulation core remains runtime-agnostic even though StateKernel now has a first externally
connectable runtime path.

Runtime exposure happens outside `StateKernel.Simulation` through:

- explicit `SimulationSignalId -> RuntimeNodeId` projection in `StateKernel.Runtime.Abstractions`
- compiled runtime plans that describe what to expose, not how the simulation executes
- `RuntimeValueUpdate` batches pushed after deterministic simulation advancement
- `StateKernel.RuntimeHost` as the lifecycle owner for runtime adapters
- `StateKernel.Runtime.UaNet` as the first concrete read-only OPC UA adapter

The accepted signal semantics remain unchanged:

- runtime updates are derived from already-committed signal snapshot values
- same-tick produced values are still unavailable during the producing tick
- runtime publication reflects completed simulation work and never triggers simulation execution
- scheduler, activation, state-machine, dependency-planning, and dependency-diagnostics logic stay in the simulation layer rather than moving into runtime-specific code

## Determinism Rules

- The clock advances in discrete fixed steps.
- Logical time is derived from tick count, not the system clock.
- Seeds are explicit and stable.
- Random streams are derived by name from a root seed.
- Scheduler execution order is explicit and stable.
- Activation is evaluated only for already-due work.
- Signal reads use the latest committed prior-step snapshot only.
- Same-tick produced signal values are never visible during the same tick.
- The latest committed signal values remain current across later non-producing ticks.
- Declared signal dependency planning validates only known published-signal existence.
- Declared dependency planning does not change runtime execution order or signal snapshot timing.
- Timing-aware dependency diagnostics reason only about the first consumer due tick.
- Dependency diagnostics are advisory artifacts and do not alter runtime execution behavior.
- Current mode is read only at execution time for already-due work.
- Transition rules evaluate against the completed tick and the pre-transition current mode.
- Applied transitions become visible to activation on the next scheduler step.
- Formal state-machine transitions evaluate against the completed tick and the pre-transition current state.
- Formal transition conditions are evaluated only inside the state-machine layer.
- The baseline condition-driven model allows at most one transition definition per source state.
- State-machine-driven mode changes become visible to activation on the next scheduler step.
- Behavior evaluation is anchored to the current logical tick.
- The simulation project remains runtime-agnostic and contains no OPC UA dependencies.

## Why This Matters

This foundation gives later simulation work a clean base for repeatable tests, benchmark reproducibility, and clear separation between deterministic execution logic and runtime adapter concerns.
