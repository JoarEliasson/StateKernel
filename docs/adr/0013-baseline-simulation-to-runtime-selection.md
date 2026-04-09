# ADR 0013: Baseline Simulation-to-Runtime Selection

- Status: Accepted
- Date: 2026-03-29

## Context

StateKernel already has a runtime composition seam that accepts runtime-facing
`RuntimeSignalSelection` values and turns them into a `RuntimeProjectionPlan` and
`CompiledRuntimePlan`.

That composition seam is intentionally runtime-facing. It applies projection defaults, validates
effective runtime node identities, and prepares adapter-consumable runtime artifacts.

As scenario-driven runtime startup grows, the architecture needs an explicit boundary between:

- upstream approval that a simulation-side signal may be exposed
- runtime-facing selection inputs that composition can consume

Without that boundary, runtime composition becomes the first place where simulation-side approval is
implicitly modeled, which weakens the separation between upstream approval, runtime composition, and
runtime startup.

## Decision

StateKernel adds a separate simulation-to-runtime selection seam in
`StateKernel.Runtime.Abstractions.Selection`.

The baseline selection seam:

- accepts explicit approved `SimulationSignalExposureChoice` inputs
- preserves optional runtime node-id and display-name overrides
- orders output deterministically by canonical `SimulationSignalId`
- produces runtime-facing `RuntimeSignalSelection` values for composition

The initial value of the seam is boundary clarity rather than a broader transformation engine.

Selection intentionally does not:

- discover signals automatically from scheduler plans or runtime artifacts
- apply runtime composition defaults
- validate effective runtime node-id collisions
- build runtime projection or compiled runtime plans
- decide runtime host or endpoint settings

Those concerns remain with upstream approval, runtime composition, and runtime startup
respectively.

`RuntimeSignalSelection` remains in `StateKernel.Runtime.Abstractions.Composition` for now. That is
intentional in this slice to avoid unnecessary API churn while the new selection seam is additive
and narrowly scoped.

## Consequences

StateKernel now has a clearer runtime layering:

`SimulationSignalExposureChoice -> RuntimeSignalSelection -> RuntimeProjectionPlan / CompiledRuntimePlan -> RuntimeStartRequest`

This improves semantic clarity without introducing a broader compile pipeline.

It also preserves the existing lower-level runtime APIs:

- direct `RuntimeSignalSelection` construction still works
- direct `RuntimeCompositionRequest` construction still works
- runtime composition remains the authority for defaults and effective node-id collision validation
- runtime startup remains the authority for hosting concerns
