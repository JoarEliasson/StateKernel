# ADR 0017: Run Control API Orchestrates Execution Seams

## Status

Accepted

## Context

StateKernel already exposed a minimal runtime control API:

- `GET /api/runtime`
- `POST /api/runtime/start`
- `POST /api/runtime/stop`

After adding explicit execution orchestration, the product needed a first run lifecycle API without
moving simulation semantics, runtime adapter behavior, or runtime host ownership into controllers.

## Decision

StateKernel adds a separate run lifecycle API:

- `GET /api/run`
- `POST /api/run/start`
- `POST /api/run/stop`

The run API is orchestration-only.

It:

- resolves an explicit `RunDefinitionId`
- validates exposure choices against the selected executable definition
- invokes the existing selection seam
- invokes the existing composition seam
- builds a `RuntimeStartRequest`
- starts and stops the execution orchestrator
- returns canonical run status from `SimulationRunStatus`

It does not:

- own scheduler semantics
- own manual stepping endpoints
- own runtime adapter logic
- own runtime update push flows
- introduce scenario CRUD or broader job orchestration

The baseline product flow is self-contained:

- `/api/run/start` starts the runtime if needed and then starts the run
- `/api/run/stop` stops the run and then stops the attached runtime

Runtime lifecycle APIs remain separate and continue to map only from `RuntimeHostStatus`.

## Consequences

Positive:

- the product now has a clear run/start/stop/status entry point
- runtime lifecycle and run lifecycle remain distinct at the API boundary
- no controller bypasses selection, composition, runtime host, or execution orchestration seams

Deferred:

- public step endpoints
- attach-to-existing-runtime flows
- broader control-plane orchestration
- richer run-definition discovery and authoring endpoints
