# ADR 0004: Baseline Simulation Mode Control Seam

- Status: Accepted
- Date: 2026-03-27

## Context

StateKernel already has a deterministic scheduler, a narrow behavior layer, and activation policies
that gate already-due work. The next control seam needs to let activation depend on the current
operating mode without pushing mode logic into the generic scheduler or widening the behavior result
surface.

The project also needs to preserve naming clarity for the later formal state-machine layer.

## Decision

StateKernel introduces `SimulationMode` as the first operating-mode abstraction in the public API.
The term `State` remains intentionally reserved for the later formal state-machine and transition
layer.

Current mode is exposed through a narrow `ISimulationModeSource` contract and an explicit
`SimulationModeController` implementation. Mode control remains external to the generic scheduler.

Activation may depend on the current mode through `BehaviorActivationContext`, which now carries
the current tick and current mode only. `BehaviorScheduledWork` reads the live current mode at
execution time after the scheduler has already determined that work is due. Mode is not pre-bound
into scheduler planning and does not participate in cadence or due determination.

This current-mode seam is the intended integration point for future state-machine-driven
transition logic.

## Consequences

- Scheduler timing, activation gating, and behavior evaluation remain separate responsibilities.
- Mode changes affect whether already-due work is allowed to execute, but they do not affect
  scheduler cadence or due determination.
- `SimulationModeController` remains intentionally small. It is not a transition engine, a
  scheduler participant, a mode-history store, or a transition validation component.
- Same-tick ordering between mode changes and due work execution is intentionally deferred in this
  baseline slice. Mode changes are expected to occur between scheduler steps.
- These are baseline decisions for the current architecture. They may expand later if a richer
  transition layer requires additional control structures or wider activation context.
