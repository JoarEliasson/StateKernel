# ADR 0012: Baseline Runtime Composition

- Status: Accepted
- Date: 2026-03-29

## Context

StateKernel already has explicit runtime lifecycle boundaries, signal-to-node projection artifacts,
and a first concrete OPC UA runtime adapter. The next step is to make runtime startup more
scenario-driven by composing runtime exposure artifacts from selected simulation signals without
turning runtime composition into a full project-wide compilation system.

The project needs a narrow seam that accepts runtime-facing signal selections, applies deterministic
defaults, and produces runtime exposure artifacts that the existing runtime host and adapter layers
already know how to consume.

## Decision

StateKernel adds a baseline runtime composition seam with:

- `RuntimeSignalSelection` as the runtime-facing signal-selection input
- `RuntimeCompositionDefaults` as the intentionally tiny defaults object
- `RuntimeCompositionRequest` as the validated composition request
- `RuntimeCompositionResult` as a convenience artifact that returns both the projection plan and the compiled runtime plan together
- `RuntimeCompositionService` as the deterministic composer

Runtime composition remains separate from full project compilation.

The baseline composition rules are:

- ordering is based only on canonical `SimulationSignalId` using ordinal semantics
- explicit `TargetNodeId` and `DisplayNameOverride` values do not affect ordering
- default node ids use `Signals/<signal-id>`
- default display names use the canonical signal-id string
- duplicate effective runtime node ids after applying defaults and overrides are invalid

`AdapterKey` is carried through composition so the result remains ready for later runtime startup,
but the baseline composition rules themselves remain adapter-agnostic.

Composition produces runtime exposure artifacts only. It does not own host startup settings, runtime
execution, pushed update behavior, scheduler timing, activation behavior, or signal-store semantics.

## Consequences

- Runtime startup can now be driven from explicit signal selections without widening runtime-host or
  adapter contracts.
- The composition seam remains testable and deterministic without turning into a broader compilation
  pipeline.
- Lower-level direct construction of `RuntimeProjectionPlan` and `CompiledRuntimePlan` remains
  supported for focused tests and lower-level scenarios.

## Deferred

The following are intentionally deferred:

- rediscovering available signals from scheduler plans
- initial-value composition
- adapter-specific composition branching
- host or endpoint settings inside composition
- project-wide runtime compilation
- fan-out, writes, methods, events, history, and broader runtime modeling
