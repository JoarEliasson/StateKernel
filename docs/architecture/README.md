# Architecture

StateKernel is structured as a local-first control plane plus runtime architecture with a deliberately isolated simulation core.

## Core Subsystems

- `StateKernel.ControlApi`: ASP.NET Core control plane for orchestration and workflow integrity.
- `StateKernel.RuntimeHost`: dedicated runtime process boundary for execution lifecycle concerns.
- `StateKernel.Simulation`: deterministic, runtime-agnostic execution core.
- `StateKernel.Runtime.Abstractions`: contracts for runtime lifecycle, signal projection, and pushed value publication.
- `StateKernel.Runtime.UaNet`: first .NET OPC UA adapter path for read-only signal exposure.
- `StateKernel.Observability`: logs, metrics, summaries, and run artifacts.
- `StateKernel.Benchmarks`: benchmark profile and result orchestration logic.
- `StateKernel.ProjectModel`, `StateKernel.NodeSet`, and `StateKernel.ProjectBundles`: persisted project structure and import/export tooling.

## Dependency Rules

- The control plane may orchestrate domain, project, runtime abstraction, benchmark, bundle, and observability concerns.
- The runtime host may coordinate simulation, runtime abstractions, and observability concerns.
- The simulation core must not depend on runtime-specific OPC UA packages.
- Runtime adapters must remain isolated from control-plane workflow concerns.
- Persisted project state must remain separate from live runtime state.

## Current Scope

The current public baseline includes the deterministic clock, explicit seed handling, minimal simulation context, the first ordered cadence-bucket scheduler, a baseline deterministic behavior layer, an activation seam that gates already-due behavior work, an explicit `SimulationMode` control seam that activation can read at execution time, a baseline post-step transition layer that can update mode between scheduler steps, the first formal state-machine foundation that evaluates post-step formal state transitions through an explicit `State -> Mode` map, a narrow deterministic transition-condition seam for those formal state transitions, a baseline deterministic signal and derived-value foundation that allows selected behaviors to read committed upstream values through a signal snapshot seam, a narrow declared signal dependency-planning seam that validates behavior-declared upstream requirements without changing execution semantics, a first timing-aware dependency diagnostics seam that reports first-need timing issues without altering runtime behavior as described in [simulation-foundation.md](simulation-foundation.md), and a first runtime exposure slice that projects selected signals into a read-only OPC UA server through explicit runtime abstractions as described in [runtime-exposure.md](runtime-exposure.md). Richer transition graphs, multi-rule transition arbitration, composed step-runner APIs, dependency propagation, bidirectional runtime control, and broader OPC UA information-model support remain future milestones.
