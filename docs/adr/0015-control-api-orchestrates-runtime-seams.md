# ADR 0015: Control API Orchestrates Runtime Seams

- Status: Accepted
- Date: 2026-03-30

## Context

StateKernel already had the runtime-side ingredients needed for a usable product entry flow:

- approved simulation-side exposure choices
- a simulation-to-runtime selection seam
- a deterministic runtime composition seam
- a runtime host that owns adapter lifecycle
- a concrete OPC UA adapter path
- a bounded runtime endpoint/security profile seam

What it did not yet have was a product-facing way to compose and start that runtime path through
the accepted architecture.

The next increment needed to make the system feel more like a usable product without collapsing the
existing seams into controllers or a new orchestration engine.

## Decision

StateKernel adds a narrow ASP.NET Core control API that orchestrates the existing runtime seams
rather than owning runtime logic.

The baseline API surface is intentionally small:

- `GET /api/runtime`
- `POST /api/runtime/start`
- `POST /api/runtime/stop`

The control API:

- accepts approved simulation-side exposure choices
- runs the simulation-to-runtime selection seam
- runs runtime composition
- builds `RuntimeStartRequest`
- starts and stops `RuntimeHostService`
- returns the canonical runtime host status read model

`RuntimeHostStatus` is the single source of truth for active runtime state at the host/API boundary.

The control API remains orchestration-only. It does not:

- instantiate adapters directly in endpoints
- reimplement selection or composition logic
- own simulation stepping
- own runtime value publication
- become a broader scenario editing or runtime control platform in this slice

## Consequences

StateKernel now has a demonstrable product-entry flow while preserving the runtime layering:

`SimulationSignalExposureChoice -> RuntimeSignalSelection -> RuntimeCompositionResult -> RuntimeStartRequest -> RuntimeHostService -> Runtime Adapter`

This keeps the control plane honest:

- selection still owns approved runtime-facing signal inputs
- composition still owns runtime projection and compiled runtime artifacts
- runtime startup still owns endpoint settings and profile choice
- the host still owns runtime adapter lifecycle

The API is now a real product seam, but it remains bounded:

- no runtime update endpoints
- no simulation stepping endpoints
- no scenario CRUD
- no broader control-plane orchestration workflows yet
