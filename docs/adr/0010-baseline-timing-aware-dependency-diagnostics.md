# ADR 0010: Baseline Timing-Aware Dependency Diagnostics

- Status: Accepted
- Date: 2026-03-28

## Context

StateKernel already has explicit signal identities, committed signal snapshots, declared signal
dependencies, and an immutable dependency-planning seam. The next step is to improve authoring
safety by detecting dependency setups that are suspicious at the first consumer need under the
accepted prior-step committed-snapshot model.

The project needs a narrow advisory diagnostics layer that can analyze validated scheduler and
dependency-plan artifacts without changing runtime execution, scheduler behavior, or the accepted
dependency planning boundaries.

## Decision

StateKernel adds a baseline timing-aware dependency diagnostics seam with:

- `SimulationDependencyDiagnosticCode` as the closed timing-diagnostic code set
- `SimulationDependencyDiagnostic` as the immutable advisory timing-diagnostic record
- `SimulationDependencyDiagnosticsReport` as the immutable report artifact
- `SimulationDependencyDiagnosticsAnalyzer` as the external analyzer

Diagnostics remain external to scheduler execution and external to `SimulationDependencyPlan`.

The analyzer consumes validated `SimulationSchedulerPlan` and `SimulationDependencyPlan` artifacts.
It depends on the current scheduler cadence semantics: tick zero is never due, and a cadence is
due only when a positive tick sequence number is evenly divisible by the cadence interval.
Therefore, the first due tick of a work item equals its cadence interval in this baseline model.

The analyzer reasons only about the first consumer due tick. It emits at most one diagnostic per
dependency binding and uses the earliest and most specific first-need timing issue under the
baseline model. Diagnostics are advisory only and are not global satisfiability proofs; later
consumer ticks may still recover even when the first need is unsatisfied.

Runtime semantics remain unchanged. Diagnostics do not alter execution ordering, signal snapshot
timing, same-tick visibility, activation behavior, or state/mode sequencing.

## Consequences

- First-need dependency timing issues become explicit, deterministic, and testable.
- The simulation core gains stronger authoring feedback without becoming a graph engine.
- The analyzer has a clear dependency on current scheduler cadence semantics that future changes
  must respect.
- These are baseline decisions for the current architecture, not permanent final constraints.

## Deferred

The following are intentionally deferred:

- graph planning and topological sorting
- cycle analysis
- activation reachability analysis
- state-machine reachability analysis
- broader timing simulation and forecasting
- same-tick propagation
- UI concerns and severity systems
