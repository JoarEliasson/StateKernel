# ADR 0011: Baseline Runtime Abstraction and OPC UA Exposure

- Status: Accepted
- Date: 2026-03-28

## Context

StateKernel already has a strong deterministic simulation core with explicit signal identity,
committed signal snapshots, declared dependency planning, and timing diagnostics. The next step is
to make the system externally connectable without collapsing runtime concerns into the simulation
layer.

The project needs a narrow runtime abstraction seam, an explicit signal-to-node projection model, a
small lifecycle-owning runtime host, and a first concrete adapter that can expose selected
simulation values to a real OPC UA client.

## Decision

StateKernel adds a baseline runtime exposure architecture with:

- `IRuntimeAdapter` and `IRuntimeAdapterFactory` as the runtime lifecycle boundary
- `RuntimeStartRequest`, `RuntimeStartResult`, and `RuntimeStopResult` as the narrow lifecycle artifacts
- `RuntimeNodeId`, `SimulationSignalProjection`, `RuntimeProjectionPlan`, `RuntimeNodeBinding`, and `CompiledRuntimePlan` as the runtime exposure model
- `RuntimeValueUpdate` as the pushed value-publication artifact keyed by `SimulationSignalId`
- `RuntimeHostService` as the lifecycle owner for one active adapter instance
- `RuntimeValueUpdateProjector` as the host-side bridge from committed signal snapshots to runtime updates
- `StateKernel.Runtime.UaNet` as the first concrete UA-.NETStandard-based OPC UA adapter

Runtime remains a consumer of simulation outputs, not a simulation owner.

The baseline runtime projection model is one `SimulationSignalId` to one read-only runtime node.
Runtime node identity is stable as a canonical relative identifier. For OPC UA, namespace index
assignment is adapter-local and is not part of the durable public seam.

Runtime updates are pushed after deterministic simulation advancement from already-committed signal
snapshot values. The runtime does not change prior-step signal semantics, same-tick visibility
rules, scheduler timing, activation behavior, or state/mode sequencing.

## Consequences

- StateKernel can now expose deterministic simulation values to a real OPC UA client end to end.
- Runtime hosting, projection, and value publication are explicit and testable without coupling the
  simulation core to OPC UA packages.
- Runtime lifecycle misuse and unprojected signal updates fail fast.
- These are baseline runtime-boundary decisions for the current architecture, not permanent final
  constraints.

## Deferred

The following are intentionally deferred:

- writes from OPC UA clients back into simulation
- methods, events, and historical access
- NodeSet import and broader information-model authoring
- richer security/profile matrices
- runtime-side scenario authoring
- multi-node fan-out from one signal
- broader runtime orchestration workflows
