# Runtime Exposure

StateKernel now includes a baseline runtime and execution platform that projects selected
deterministic simulation signals into a real read-only OPC UA server, applies bounded runtime
endpoint/security profiles, owns one active deterministic run, and exposes minimal control APIs
without moving simulation semantics into runtime-specific code.

## Runtime Boundary

The runtime side is intentionally narrow:

- `StateKernel.Runtime.Abstractions` defines the lifecycle, selection, composition, projection, and value-update contracts
- `StateKernel.Runtime.Abstractions` also defines the bounded runtime endpoint/security profile model
- `StateKernel.RuntimeHost` owns adapter lifecycle, one-active-run execution orchestration, and projection of committed signal snapshots into runtime value updates
- `StateKernel.Runtime.UaNet` is the first concrete OPC UA runtime adapter
- `StateKernel.ControlApi` orchestrates runtime lifecycle and run lifecycle across those seams

The runtime remains a consumer of simulation outputs. It does not own scheduler timing, activation,
state-machine evaluation, dependency planning, dependency diagnostics, or signal-store semantics.

## Runtime Selection

StateKernel now includes a narrow simulation-to-runtime selection seam that sits before runtime
composition.

Selection is responsible only for transforming approved simulation-side exposure choices into
runtime-facing `RuntimeSignalSelection` values:

- it accepts explicit approved `SimulationSignalExposureChoice` inputs
- it preserves explicit runtime node-id and display-name overrides when they are supplied
- it returns deterministic runtime-facing selections ordered by canonical signal id
- it remains separate from runtime composition and runtime startup

This seam is primarily a boundary/type distinction seam. `SimulationSignalExposureChoice` records
upstream approval to expose a simulation signal, while `RuntimeSignalSelection` remains the
runtime-facing input consumed by composition.

Selection intentionally does not:

- discover available signals automatically
- apply runtime composition defaults
- validate effective runtime node-id collisions
- decide how or where a runtime adapter is hosted

Effective runtime node-id collision validation remains in runtime composition.

Selection is additive rather than mandatory. Lower-level callers may still construct
`RuntimeSignalSelection` values directly when they need to work at the runtime-composition layer.

## Runtime Composition

StateKernel now includes a narrow runtime composition seam that sits before runtime startup.

Composition is responsible only for deciding what to expose:

- it accepts runtime-facing `RuntimeSignalSelection` inputs
- it applies the baseline projection defaults
- it produces a validated `RuntimeProjectionPlan`
- it produces a `CompiledRuntimePlan` ready for runtime startup

Composition remains separate from full project compilation and separate from hosting concerns.
`RuntimeStartRequest` still decides how and where a runtime adapter is hosted, and under which
bounded runtime endpoint/security profile it is hosted.

`RuntimeSignalSelection` currently remains in the composition namespace intentionally to avoid
unnecessary runtime API churn while the selection seam is still narrow and additive.

## Projection Model

The baseline projection model is explicit and signal-based:

- one `SimulationSignalId` maps to one `RuntimeNodeId`
- composition can derive that mapping deterministically from selected signals
- projection is described by `SimulationSignalProjection` and `RuntimeProjectionPlan`
- runtime startup consumes a `CompiledRuntimePlan`
- runtime node identity is a stable canonical relative identifier such as `Signals/Source`

Baseline composition defaults are intentionally small:

- node-id prefix: `Signals`
- default node id: `Signals/<signal-id>`
- default display name: canonical signal id string

The composition result is a convenience artifact only. It returns both the projection plan and the
compiled runtime plan together, but it does not become a start request, orchestration artifact, or
execution authority.

For the UA adapter, namespace index assignment is adapter-local. The durable identity seam is the
relative runtime node identifier plus the adapter's namespace URI, not a hard-coded namespace index.

## Runtime Profiles

Runtime startup now carries a bounded endpoint/security profile selection separately from runtime
composition.

The current profile set is intentionally small:

- `local-dev`: insecure by design, loopback-oriented, and intended for low-friction local startup and loopback E2E testing
- `baseline-secure`: the strongest bounded secure profile in this slice, enforced by the adapter as a secure-only OPC UA startup using `SignAndEncrypt` plus `Basic256Sha256`

The secure profile is adapter-enforced rather than a metadata label:

- the UA adapter applies the selected mode/policy internally
- `baseline-secure` does not fall back to an insecure endpoint
- secure startup now includes post-start endpoint-set verification against the actual exposed endpoint set
- local application certificate handling remains internal to the adapter path

This is still a bounded baseline, not a full enterprise security product:

- no broad security matrix
- no enterprise PKI workflow
- no trust-store UX
- no user or role authorization model yet

## Lifecycle Status and Fault Semantics

Runtime lifecycle state and run lifecycle state remain separate and now follow explicit invariant rules.

`RuntimeHostStatus` is the canonical runtime lifecycle read model:

- active runtime state: `IsRunning == true`, adapter/profile/endpoint/node-count metadata populated, `LastFault == null`
- clean inactive state: `IsRunning == false`, active metadata cleared, `LastFault == null`
- inactive faulted state: `IsRunning == false`, active metadata cleared, `LastFault != null`

`SimulationRunStatus` is the canonical run lifecycle read model:

- active run state: `IsActive == true`, run/runtime attachment metadata populated, `RuntimeAttached == true`, `LastFault == null`
- clean inactive state: `IsActive == false`, active metadata cleared, `RuntimeAttached == false`, `LastFault == null`
- inactive faulted state: `IsActive == false`, active metadata cleared, `RuntimeAttached == false`, `LastFault != null`

The current bounded fault model retains only one last fault per status domain:

- host/runtime faults describe runtime lifecycle failures
- run faults describe execution lifecycle failures
- both may exist at the same time on one failure path without contradiction

Successful restart semantics are independent:

- a successful runtime start clears retained runtime fault state
- a successful run start clears retained run fault state

Request/configuration errors and lifecycle conflicts do not populate retained fault state.

## Update Flow

The baseline update flow is push-based and post-step:

1. deterministic simulation advances
2. the signal store exposes a committed prior-step snapshot
3. approved simulation-side exposure choices can be transformed into runtime-facing selections
4. runtime composition turns those selections into validated projection and compiled runtime artifacts
5. `RuntimeValueUpdateProjector` creates ordered `RuntimeValueUpdate` items for projected signals present in that snapshot
6. `RuntimeHostService` forwards the batch to the active runtime adapter
7. the runtime adapter updates externally exposed node values

This preserves the accepted signal semantics:

- runtime updates reflect already-computed committed values
- same-tick signal writes remain unavailable during the producing tick
- runtime exposure never drives simulation execution

## Run Orchestration

StateKernel now includes a narrow run/execution seam that sits after runtime startup and before any
future broader product orchestration.

Run orchestration is responsible for:

- owning one active deterministic run
- resolving one approved executable run definition
- advancing deterministic simulation steps
- reading the committed snapshot visible after each completed step
- projecting runtime value updates from that committed snapshot
- forwarding those updates through `RuntimeHostService`
- exposing canonical run status separately from runtime host status

This seam stays intentionally narrow:

- it does not reimplement selection or composition
- it does not own runtime adapter behavior
- it does not change scheduler, activation, state/mode, or signal-store semantics
- it does not introduce multi-run orchestration or benchmark scheduling

The baseline executable definition seam is explicit:

- `RunDefinitionId` selects one approved executable simulation shape
- the current baseline catalog contains one built-in definition: `baseline-constant-source`
- that definition can expose `Source` and creates fresh run-scoped scheduler and signal-store collaborators for each run

The baseline run lifecycle supports exactly one active run:

- starting while a run is active fails clearly
- runtime startup is performed as part of run start in the product-facing flow
- run stop tears down the attached runtime in this baseline
- inactive run and runtime status snapshots clear endpoint, profile, and projected-node metadata consistently
- richer attach/detach semantics are intentionally deferred

Stepping remains explicit and deterministic:

- the execution seam supports both `Manual` and `Continuous` modes
- the control API uses the definition's continuous default
- `StepOnceAsync(...)` remains an internal/test seam for manual runs only
- continuous runs perform one immediate step before startup returns, then continue on a bounded timer loop
- the orchestrator serializes all stepping so loop-driven iterations and explicit manual steps cannot overlap

Committed snapshot visibility is kept explicit:

- the execution seam reads the snapshot visible at the next tick boundary after a completed step
- this preserves the accepted prior-step committed-snapshot rule rather than inventing a new publication semantic
- runtime publication remains a consequence of deterministic advancement, never the cause of it

Failure handling is now explicit:

- request-time runtime or run start/stop failures can return bounded `500` responses
- later asynchronous continuous-loop failures do not retroactively fail old requests
- instead, later background failures transition the system to inactive/faulted status and are observed through `GET /api/run` and `GET /api/runtime`

## Control API

StateKernel now includes a minimal ASP.NET Core control API for the product-entry runtime flow.

The runtime lifecycle API supports only:

- `GET /api/runtime`
- `POST /api/runtime/start`
- `POST /api/runtime/stop`

StateKernel now also exposes a separate run lifecycle surface:

- `GET /api/run`
- `POST /api/run/start`
- `POST /api/run/stop`

The control API remains orchestration-only:

- it transforms approved simulation-side exposure choices into runtime-facing selections
- it runs runtime composition
- it starts and stops the runtime host when required by the selected flow
- it starts and stops the execution orchestrator
- it reports canonical runtime and run statuses separately

The existing route surface remains intentionally fixed in this hardening phase:

- `GET /api/runtime`
- `POST /api/runtime/start`
- `POST /api/runtime/stop`
- `GET /api/run`
- `POST /api/run/start`
- `POST /api/run/stop`

Status payloads now include bounded flattened fault visibility:

- `FaultCode`
- `FaultMessage`
- `FaultOccurredAtUtc`

These remain product-facing status fields rather than debugging dumps:

- no stack traces
- no nested exception graphs
- no raw OPC UA internal exception serialization

The control API does not own:

- scheduler semantics
- manual stepping endpoints
- runtime update push endpoints
- runtime update batches
- scenario editing or broader control-plane CRUD

## First UA Adapter

The first concrete adapter is `StateKernel.Runtime.UaNet` on the OPC Foundation UA-.NETStandard
stack. In this baseline slice it supports:

- runtime startup from a compiled runtime plan
- runtime shutdown
- bounded endpoint/security profile application
- one projected node per signal
- read-only `double` value exposure
- pushed value updates from the host
- real OPC UA client connectivity over loopback

## Deferred

The following are intentionally deferred:

- broader project-wide compilation pipelines
- scheduler-plan mining and automatic runtime signal discovery
- writes from OPC UA clients back into simulation
- methods, events, and historical access
- NodeSet import and broader information-model authoring
- security profile matrices beyond the bounded two-profile baseline
- secure client-session E2E coverage beyond startup and endpoint-discovery assertions
- richer fault reporting and longer fault history beyond one retained bounded `LastFault`
- attach-to-existing-runtime execution flows
- runtime-side scenario authoring
- multi-node fan-out from one signal
