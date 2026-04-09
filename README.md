# StateKernel

<img src="assets/brand/statekernel-logo-primary.png" alt="StateKernel" width="420" />

StateKernel is a local-first OPC UA scenario studio for deterministic integration testing, security-profile comparison, and benchmark-grade observability.

## Current Baseline

The repository now contains a working deterministic simulation kernel plus a first real runtime exposure path:

- a deterministic clock, seed boundary, scheduler, and behavior execution model
- explicit activation, operating-mode control, and formal state-machine coordination
- committed-snapshot signals, derived-value behaviors, dependency planning, and timing diagnostics
- a runtime abstraction seam, a simulation-to-runtime selection layer, a deterministic runtime composition layer, a runtime host, and a read-only OPC UA server adapter that can expose selected simulation signals to a real OPC UA client
- two bounded runtime endpoint/security profiles: `local-dev` for low-friction loopback work and `baseline-secure` for a verified secure-only OPC UA startup posture
- a baseline run/execution orchestration seam that owns one active deterministic run, explicit run status, and runtime publication from committed snapshots
- tightened runtime/run lifecycle contracts with bounded retained fault readback and explicit active/inactive/faulted status semantics
- a minimal ASP.NET Core control API that can start, stop, and report both runtime lifecycle and run lifecycle through the accepted seams, including bounded fault visibility

The simulation core remains runtime-agnostic. Runtime adapters consume already-computed simulation outputs after deterministic advancement rather than owning simulation execution.

## Repository Layout

- `src/` contains the production .NET projects and runtime boundaries.
- `tests/` contains unit, contract, API, and integration test projects.
- `docs/` contains the public-safe product, brand, architecture, standards, and ADR material.
- `assets/brand/` contains the public brand assets used for repository and product presentation.
- `.github/` contains the baseline CI workflow.

## Build and Test

```bash
dotnet restore StateKernel.sln
dotnet build StateKernel.sln --configuration Release
dotnet test StateKernel.sln --configuration Release
```

Useful local entry points:

```bash
dotnet run --project src/StateKernel.ControlApi
dotnet run --project src/StateKernel.RuntimeHost
```

## Solution Map

- `StateKernel.Simulation` is the deterministic, runtime-agnostic execution core.
- `StateKernel.Runtime.Abstractions` defines runtime lifecycle, selection, composition, projection, and value-update contracts.
- `StateKernel.RuntimeHost` owns runtime adapter lifecycle, explicit run/execution orchestration, and projection of committed signal snapshots into runtime updates.
- `StateKernel.Runtime.UaNet` is the first concrete .NET OPC UA adapter path for read-only signal exposure and bounded endpoint/security profiles.
- `StateKernel.ControlApi` hosts the ASP.NET Core control plane for runtime start/stop/status and run start/stop/status orchestration.
- `StateKernel.Observability`, `StateKernel.Benchmarks`, `StateKernel.NodeSet`, and `StateKernel.ProjectBundles` provide the surrounding product subsystems.

## Documentation

- [Product overview](docs/product/README.md)
- [Brand direction](docs/brand/README.md)
- [Engineering standards](docs/standards/README.md)
- [Architecture overview](docs/architecture/README.md)
- [Simulation foundation](docs/architecture/simulation-foundation.md)
- [Runtime exposure](docs/architecture/runtime-exposure.md)
- [Architecture decision records](docs/adr/README.md)

## Public Repo Conventions

- Public-safe documentation belongs in `docs/`.
- The simulation core must remain runtime-agnostic.
- Runtime-specific implementation belongs behind `StateKernel.Runtime.Abstractions`.
- This public repository keeps the committed artifact set focused on polished code, tests, docs, CI, and presentation assets.
