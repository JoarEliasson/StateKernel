# ADR 0001: Deterministic Fixed-Step Ordered Cadence Buckets

- Status: Accepted
- Date: 2026-03-15

## Context

StateKernel needs a scheduler baseline that can orchestrate deterministic simulation work without drifting toward task-per-node execution, wall-clock coupling, or implicit ordering. This layer sits directly on top of the deterministic logical clock and will become part of the hot path for future behaviors, dependency evaluation, and state-machine execution.

## Decision

StateKernel will use a fixed-step deterministic scheduler that:

- advances from the simulation clock rather than ambient time
- groups scheduled work into cadence buckets keyed by tick interval
- orders cadence buckets by interval
- orders work inside each bucket by explicit order, then stable key
- executes on a single deterministic orchestration path

## Consequences

This keeps execution order explicit and testable, supports predictable repeatability, and avoids speculative concurrency or timer-per-node drift. It also prepares the codebase for later scheduler-adjacent features without prematurely introducing a full behavior engine or runtime loop framework.

The scheduler plan is structurally immutable after creation, and execution snapshots currently
capture only the executed tick and ordered work keys. Work implementations remain responsible for
any internal mutable state and for participating in a future broader run-reset lifecycle.

## Alternatives Considered

- Task-per-node scheduling
- Wall-clock-driven scheduler loops
- Registration-order execution without explicit ordering
