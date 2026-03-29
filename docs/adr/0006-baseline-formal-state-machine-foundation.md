# ADR 0006: Baseline Formal State-Machine Foundation

- Status: Accepted
- Date: 2026-03-28

## Context

StateKernel already has a deterministic scheduler, a baseline behavior layer, activation policies,
an explicit `SimulationMode` seam, and a post-step mode-transition layer. The next step is to add
formal `State` vocabulary without collapsing state and mode into one concept or pushing
state-machine logic into the generic scheduler.

The project needs a baseline formal control model that can evaluate deterministic post-step state
changes and drive the activation-facing mode seam through an explicit mapping.

## Decision

StateKernel adds a baseline `StateKernel.Simulation.StateMachines` module with:

- canonical `SimulationStateId` value objects
- minimal state definitions
- exact completed-tick transition definitions
- an immutable state-machine definition
- a separate explicit total `State -> Mode` map
- a narrow post-step state-machine coordinator
- a narrow applied formal state transition result

Formal state-machine evaluation happens after a completed scheduler step. Transition definitions
evaluate against the completed tick and the pre-transition formal state that was active during that
completed step. If a matching transition is found, the coordinator updates the current formal
state immediately after evaluation.

The coordinator then derives the target state's mapped mode through the explicit `State -> Mode`
map. If the mapped mode differs from the current `SimulationModeController.CurrentMode`, the
coordinator updates the controller immediately after the formal state change. The updated mode then
becomes visible to activation on the next scheduler step.

Formal state transitions and operating mode changes are intentionally distinct. Two different
formal states may map to the same operating mode. In that case a real formal state transition still
occurs, but no mode mutation is applied.

The coordinator requires the initial state's mapped mode to match the current mode controller at
construction time. Construction fails fast when that alignment is missing.

## Consequences

- Formal state remains distinct from operating mode.
- Post-step semantics remain consistent across scheduler timing, formal state evaluation, mode
  control, activation, and behavior execution.
- The coordinator returns a narrow applied formal state transition result without introducing a
  history store in this slice.
- The baseline API makes the `State -> Mode` relationship explicit and testable.
- These are baseline decisions for the current architecture, not permanent final constraints.

## Deferred

The following are intentionally deferred:

- graph-style transition DSLs
- guard expression systems
- history stores
- multi-machine orchestration
- richer state transition semantics beyond exact completed-tick triggers
