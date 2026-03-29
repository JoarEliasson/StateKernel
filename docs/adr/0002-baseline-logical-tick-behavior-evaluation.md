# ADR 0002: Baseline Logical-Tick Behavior Evaluation

- Status: Accepted
- Date: 2026-03-21

## Context

StateKernel needs the first real behavior execution slice on top of the deterministic scheduler.
This slice must prove that the simulation core can produce deterministic values through scheduled
work without prematurely committing to a full behavior graph, state-machine orchestration model, or
runtime projection layer.

## Decision

The baseline behavior slice will:

- evaluate behaviors against the current logical simulation tick
- use a narrow numeric `double` result surface for produced samples
- integrate behaviors into the scheduler through generic scheduled-work adapters

The first linear ramp behavior is anchored to the absolute logical tick and is defined as
`startValue + (currentTick.SequenceNumber * stepPerTick)`. Scheduler cadence affects when the ramp
is sampled, but it does not redefine the ramp function itself.

## Consequences

This keeps the first behavior layer small, deterministic, and easy to test. It also preserves the
generic scheduler boundary so later state transitions, dependency work, and fault overlays can use
similar adapter-style integration rather than forcing behavior-specific logic into the scheduler.

These are baseline decisions for the current slice rather than permanent final constraints. Future
milestones may justify richer behavior outputs or a wider execution context when real use cases
require them.

## Alternatives Considered

- Behavior progression based on per-behavior execution count
- Behavior progression based on elapsed logical time as the primary contract
- Behavior-specific scheduler logic instead of adapter-based integration
