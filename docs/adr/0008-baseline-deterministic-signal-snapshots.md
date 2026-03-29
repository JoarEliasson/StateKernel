# ADR 0008: Baseline Deterministic Signal Snapshots

- Status: Accepted
- Date: 2026-03-28

## Context

StateKernel already has deterministic behaviors, activation, mode control, and formal state-machine
control. The next step is to allow selected simulated outputs to be derived from previously
produced outputs without turning the simulation core into a graph engine, propagation system, or
runtime projection layer.

The project needs a narrow produced-value identity seam and a deterministic read model that keeps
signal reads separate from scheduler timing, activation gating, and behavior evaluation.

## Decision

StateKernel adds a baseline `StateKernel.Simulation.Signals` module with:

- explicit `SimulationSignalId` value objects
- produced `SimulationSignalValue` records
- read-only committed `SimulationSignalSnapshot` objects
- a narrow `SimulationSignalValueStore`
- an optional `ISignalProducingWork` capability seam for duplicate-producer validation

Signal identity is explicit and separate from scheduler work keys, formal states, operating modes,
and runtime node identity.

Derived reads use the latest committed prior-step snapshot only. All behaviors executing on tick
`N` read the same committed snapshot. Values produced during tick `N` are staged and never become
visible during tick `N`.

The committed snapshot uses hold-last-committed-value semantics. If no later produced tick replaces
a signal value, the latest committed value remains readable on later ticks.

Duplicate producers are rejected by published signal id during scheduler-plan validation. Same-tick
reads are intentionally unsupported in the baseline slice.

## Consequences

- Derived-value behavior can exist without widening scheduler contracts or behavior result surfaces.
- Signal reads remain deterministic and independent of same-tick scheduler ordering.
- The baseline signal store is committed-snapshot-based rather than history-based.
- These are baseline decisions for the current architecture, not permanent final constraints.

## Deferred

The following are intentionally deferred:

- dependency graphs
- topological sorting and propagation engines
- cycle resolution
- formula DSLs
- history stores
- runtime-node projection
