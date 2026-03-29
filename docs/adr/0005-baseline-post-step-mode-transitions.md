# ADR 0005: Baseline Post-Step Mode Transitions

- Status: Accepted
- Date: 2026-03-27

## Context

StateKernel already has a deterministic scheduler, a baseline behavior layer, activation policies,
and a narrow `SimulationMode` control seam. The next step is to formalize when mode changes occur
without pushing transition logic into the generic scheduler or into behaviors.

The project also needs to preserve the naming distinction between the operating-mode seam and the
later formal state-machine layer.

## Decision

StateKernel adds a baseline `StateKernel.Simulation.Modes.Transitions` module with:

- a minimal transition context containing only the completed tick and the pre-transition current mode
- a single-rule transition coordinator outside the generic scheduler
- a baseline `NeverTransitionRule`
- a baseline `TickMatchTransitionRule`
- an applied transition record used only when an actual mode change occurs

Transition evaluation happens after a completed scheduler step. Rules evaluate against the
completed tick and the pre-transition current mode that was active during that step. When a rule
selects a different target mode, the coordinator applies the change immediately after evaluation,
and the updated mode becomes visible to activation on the next scheduler step.

The initial mode governs the first executed tick. Same-mode target selections are treated as no
transition and do not produce transition records.

`RunTicks()` remains intentionally transition-unaware. Callers that need transition-aware
sequencing must step explicitly with `RunNextTick()` followed by transition evaluation.

`Mode` remains distinct from the future formal `State` layer.

## Consequences

- Scheduler timing, transition evaluation, activation gating, and behavior evaluation remain
  separate responsibilities.
- Transition coordination stays deterministic, single-threaded, and external to the generic
  scheduler.
- The baseline API commits to one rule only. There is no multi-rule ordering or conflict policy in
  this slice.
- These are baseline decisions for the current architecture, not permanent final constraints.

## Deferred

The following are intentionally deferred:

- multi-rule conflict handling
- scheduled mode-change work
- richer transition graphs
- composed step-runner APIs
- fuller state-machine semantics
