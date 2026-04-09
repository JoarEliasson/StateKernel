# ADR 0016: Baseline Run / Execution Orchestration

## Status

Accepted

## Context

StateKernel already had:

- a deterministic simulation core
- runtime selection and composition seams
- runtime startup through `RuntimeHostService`
- runtime endpoint/security profiles
- a product-facing runtime control API

The remaining gap was execution ownership. Runtime could be started and read, but there was no
explicit seam that owned:

- one active run
- deterministic stepping
- publication from committed snapshots into the active runtime
- run lifecycle/status as a concept distinct from runtime lifecycle/status

## Decision

StateKernel introduces a narrow execution seam under `StateKernel.RuntimeHost.Execution`.

That seam includes:

- `SimulationRunId`
- `SimulationRunDefinitionId`
- executable run definitions resolved through a bounded catalog
- `SimulationExecutionSettings`
- `SimulationRunStatus`
- `SimulationExecutionOrchestrator`

The baseline execution model is:

- exactly one active run
- runtime publication remains downstream of deterministic stepping
- committed snapshot visibility is read explicitly at the next tick boundary after each completed step
- the orchestrator starts and stops the attached runtime through `RuntimeHostService`
- continuous runs perform one immediate step before returning, then continue on a bounded timer loop
- manual stepping remains available only as an internal/test seam

The executable-definition contract is intentionally executable rather than a pure immutable scenario
document in this slice. It exists to create fresh run-scoped scheduler and signal-store
collaborators without introducing a broader project/scenario authoring system yet.

The baseline catalog contains one approved built-in definition:

- `baseline-constant-source`

## Consequences

Positive:

- execution ownership is now explicit
- run lifecycle is distinct from runtime lifecycle
- deterministic stepping can drive real OPC UA publication through the accepted seams
- the control plane can expose a usable run product flow without collapsing runtime and simulation logic

Deferred:

- multi-run orchestration
- attach-to-existing-runtime flows
- richer fault reporting
- broader run-definition catalogs and authoring
- benchmark scheduling and observability expansion
