# Runtime Exposure

StateKernel now includes a first runtime exposure slice that projects selected deterministic
simulation signals into a real read-only OPC UA server without moving simulation semantics into
runtime-specific code.

## Runtime Boundary

The runtime side is intentionally narrow:

- `StateKernel.Runtime.Abstractions` defines the lifecycle, projection, and value-update contracts
- `StateKernel.RuntimeHost` owns adapter lifecycle and projects committed signal snapshots into runtime value updates
- `StateKernel.Runtime.UaNet` is the first concrete OPC UA runtime adapter

The runtime remains a consumer of simulation outputs. It does not own scheduler timing, activation,
state-machine evaluation, dependency planning, dependency diagnostics, or signal-store semantics.

## Projection Model

The baseline projection model is explicit and signal-based:

- one `SimulationSignalId` maps to one `RuntimeNodeId`
- projection is described by `SimulationSignalProjection` and `RuntimeProjectionPlan`
- runtime startup consumes a `CompiledRuntimePlan`
- runtime node identity is a stable canonical relative identifier such as `Signals/Source`

For the UA adapter, namespace index assignment is adapter-local. The durable identity seam is the
relative runtime node identifier plus the adapter's namespace URI, not a hard-coded namespace index.

## Update Flow

The baseline update flow is push-based and post-step:

1. deterministic simulation advances
2. the signal store exposes a committed prior-step snapshot
3. `RuntimeValueUpdateProjector` creates ordered `RuntimeValueUpdate` items for projected signals present in that snapshot
4. `RuntimeHostService` forwards the batch to the active runtime adapter
5. the runtime adapter updates externally exposed node values

This preserves the accepted signal semantics:

- runtime updates reflect already-computed committed values
- same-tick signal writes remain unavailable during the producing tick
- runtime exposure never drives simulation execution

## First UA Adapter

The first concrete adapter is `StateKernel.Runtime.UaNet` on the OPC Foundation UA-.NETStandard
stack. In this baseline slice it supports:

- runtime startup from a compiled runtime plan
- runtime shutdown
- one projected node per signal
- read-only `double` value exposure
- pushed value updates from the host
- real OPC UA client connectivity over loopback

## Deferred

The following are intentionally deferred:

- writes from OPC UA clients back into simulation
- methods, events, and historical access
- NodeSet import and broader information-model authoring
- security profile matrices beyond the minimal local test baseline
- runtime-side scenario authoring
- multi-node fan-out from one signal
