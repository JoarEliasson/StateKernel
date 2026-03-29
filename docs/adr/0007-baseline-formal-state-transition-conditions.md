# ADR 0007: Baseline Formal State Transition Conditions

- Status: Accepted
- Date: 2026-03-28

## Context

StateKernel already has a baseline formal state-machine foundation with post-step evaluation and an
explicit `State -> Mode` map. The next step is to allow formal state transitions to be triggered by
more than one exact completed-tick match without introducing a broad guard system, general rule
engine, or scheduler-level transition logic.

## Decision

StateKernel adds a narrow transition-condition seam inside
`StateKernel.Simulation.StateMachines` with:

- `ISimulationStateTransitionCondition`
- `SimulationStateTransitionConditionContext`
- `CompletedTickMatchCondition`
- `CompletedTickAtOrAfterCondition`

`SimulationStateTransitionDefinition` now owns a source state, a target state, and one explicit
transition condition.

Condition evaluation remains post-step. Conditions are evaluated against the completed tick and the
pre-transition current state that was active during that completed step.

To preserve deterministic selection without adding conflict handling in this slice, the baseline
condition-driven model allows at most one transition definition per source state.

## Consequences

- Formal transition eligibility is now extensible without widening scheduler or behavior contracts.
- Condition evaluation stays inside the formal state-machine layer.
- Exact-match transition behavior remains supported after the refactor.
- Same-mode formal state transitions remain valid formal state changes.
- These are baseline decisions for the current architecture, not permanent final constraints.

## Deferred

The following are intentionally deferred:

- guard DSLs
- arbitrary predicates or delegate-based transition logic
- composite condition trees
- multi-rule conflict handling
- dependency-driven transitions
- transition history stores
