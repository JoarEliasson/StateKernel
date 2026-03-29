# ADR 0009: Baseline Declared Signal Dependency Planning

- Status: Accepted
- Date: 2026-03-28

## Context

StateKernel already has explicit signal identities, committed signal snapshots, derived-value
behaviors, and duplicate published-signal validation. The next step is to strengthen authoring-time
safety without changing the accepted runtime execution model.

The project needs a narrow way for selected behaviors to declare which upstream signals they
require, and a narrow planning seam that can validate those declarations before execution without
turning the simulation core into a graph planner, propagation engine, or scheduler-level dependency
system.

## Decision

StateKernel adds a baseline declared dependency-planning seam with:

- `ISignalDependentBehavior` on behaviors for stable, duplicate-free required signal declarations
- `SimulationDependencyPlanner` as an external validator and planner that consumes
  `SimulationSchedulerPlan`
- `SimulationDependencyPlan` as an explicit immutable artifact for discovered published signals and
  resolved declared dependencies
- `SimulationPublishedSignal` and `SimulationSignalDependencyBinding` as the baseline inspection
  records

Declared signal dependencies live on behaviors, not on generic scheduler contracts. Dependency
planning remains external to `SimulationSchedulerPlan`.

The planner discovers published signals through `ISignalProducingWork` and declared dependencies
only through `BehaviorScheduledWork` when its adapted behavior implements
`ISignalDependentBehavior`.

Duplicate declared dependencies from one behavior are invalid. Declared dependencies that reference
unknown published signals fail fast during planning.

Execution semantics remain unchanged. The dependency plan does not drive runtime execution order,
does not alter prior-step committed-snapshot timing, and does not make same-tick reads available.

## Consequences

- Dependency metadata becomes explicit, deterministic, and testable.
- Authoring-time validation improves without widening scheduler contracts.
- The planning seam is inspectable and extensible without becoming a runtime dependency engine.
- These are baseline decisions for the current architecture, not permanent final constraints.

## Deferred

The following are intentionally deferred:

- graph planning and topological sorting
- cycle analysis
- first-tick availability checks
- cadence compatibility and timing validation
- same-tick propagation
- dependency history or observability stores
- formula or expression DSLs
