# ADR 0003: Baseline Behavior Activation Gating

- Status: Accepted
- Date: 2026-03-27

## Context

StateKernel now has a deterministic scheduler and a baseline behavior execution layer. The next
step is to introduce a clean activation seam that can decide whether already-due behavior work is
allowed to execute without pushing activation semantics into the generic scheduler or widening the
behavior result surface.

## Decision

The baseline activation slice will:

- keep scheduler timing, activation gating, and behavior evaluation as separate responsibilities
- model activation outside the generic scheduler
- treat inactive due work as a non-executed behavior evaluation that produces no behavior sample and no output record
- use a baseline activation context that exposes only the current deterministic tick

Activation is evaluated only after the scheduler has already determined that work is due. It is not
a second timing system and does not trigger execution on its own.

This activation layer is the intended seam for future state-machine-driven control.

## Consequences

This keeps the execution model explicit and testable. Scheduler cadence continues to define when
work becomes eligible, activation defines whether eligible work is allowed to execute, and behavior
evaluation defines what value is produced. The resulting architecture stays small and prepares for
future state-machine integration without requiring a full state-machine engine in this slice.

These are baseline decisions for the current slice rather than permanent final constraints. Future
milestones may justify richer activation context or more expressive policies when concrete
deterministic use cases require them.

## Alternatives Considered

- Modeling activation as part of scheduler timing
- Treating inactivity as a recorded status rather than absence of execution
- Widening activation context before a concrete policy required it
