# StateKernel

![StateKernel](assets/brand/statekernel-logo-primary.png)

StateKernel is a local-first OPC UA scenario studio for deterministic integration testing, security-profile comparison, and benchmark-grade observability.

## Current Baseline

The repository now contains a working deterministic simulation kernel plus a first real runtime exposure path:

- a deterministic clock, seed boundary, scheduler, and behavior execution model
- explicit activation, operating-mode control, and formal state-machine coordination
- committed-snapshot signals, derived-value behaviors, dependency planning, and timing diagnostics
- a runtime abstraction seam, runtime host, and a read-only OPC UA server adapter that can expose selected simulation signals to a real OPC UA client

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
- `StateKernel.Runtime.Abstractions` defines runtime lifecycle, projection, and value-update contracts.
- `StateKernel.RuntimeHost` owns runtime adapter lifecycle and projects committed signal snapshots into runtime updates.
- `StateKernel.Runtime.UaNet` is the first concrete .NET OPC UA adapter path for read-only signal exposure.
- `StateKernel.ControlApi` hosts the ASP.NET Core control plane.
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
