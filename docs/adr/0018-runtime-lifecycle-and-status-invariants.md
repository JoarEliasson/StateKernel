# ADR 0018: Runtime Lifecycle and Status Invariants

## Status

Accepted

## Context

By the end of Phases 2.4 and 2.5, StateKernel had:

- runtime lifecycle ownership in `RuntimeHostService`
- run lifecycle ownership in `SimulationExecutionOrchestrator`
- separate runtime and run control APIs

Those seams were already useful, but the lifecycle/status contracts still needed harder guarantees for:

- active vs inactive semantics
- bounded fault visibility
- consistent API readback after failures
- clear separation between runtime lifecycle failure and run lifecycle failure

## Decision

StateKernel now uses explicit invariant-based lifecycle read models:

- `RuntimeHostStatus` is the canonical runtime lifecycle read model
- `SimulationRunStatus` is the canonical run lifecycle read model

Each read model supports exactly three observable states:

1. active
2. clean inactive
3. inactive faulted

Active states never retain fault information.
Clean inactive states clear both active metadata and fault information.
Inactive faulted states clear active metadata and retain one bounded `LastFault`.

StateKernel also now retains only one bounded fault per lifecycle domain:

- `RuntimeHostFaultInfo`
- `SimulationRunFaultInfo`

These are surfaced through status and API read models, but StateKernel does not add fault history,
stack-trace payloads, or an event-stream observability surface in this phase.

Lifecycle ownership remains separate:

- `RuntimeHostService` owns runtime adapter lifecycle
- `SimulationExecutionOrchestrator` owns run lifecycle and deterministic stepping

Both domains may fault on the same path:

- runtime fault explains runtime-lifecycle failure
- run fault explains execution-lifecycle failure

These are not contradictory.

## Consequences

Benefits:

- API status readback is clearer and more trustworthy
- fault cleanup behavior is explicit and testable
- runtime lifecycle and run lifecycle remain separate even when coupled in product flows
- later UI work has stable status semantics to build on

Deferred:

- multi-fault history
- richer diagnostics/event streams
- broader lifecycle timestamps beyond bounded fault timestamps
