# Project Status

## Current status

StateKernel currently has two major completed foundations:

### 1. Deterministic simulation core

StateKernel includes a deterministic simulation core with:

- fixed-step scheduling
- explicit behaviors
- activation and control seams
- formal state/mode separation
- signal/value semantics
- derived-value support
- dependency planning and diagnostics

### 2. Baseline runtime platform

StateKernel also includes a baseline runtime architecture with:

- explicit runtime adapter abstractions
- runtime composition
- simulation-to-runtime selection
- a runtime host
- a baseline run/execution orchestration seam
- a first UA-.NETStandard OPC UA adapter
- runtime endpoint/security profiles
- control APIs for runtime start/stop/status and run start/stop/status
- explicit runtime/run status invariants with bounded retained fault visibility
- verified secure-profile startup checks with no insecure fallback path
- real end-to-end OPC UA exposure of simulation values through both runtime and run product flows

## What is currently being built next

The next major step is to add the first baseline Studio/UI entry that can:

- present runtime and run lifecycle/status clearly
- drive the existing profile/exposure/start flows through the accepted APIs
- make the hardened runtime/run platform easier to demonstrate and operate

This is the next highest-value move because the runtime and execution platform is now contract-hardened enough to support a product-facing entry point without inventing new backend seams first.

## What remains after that

Major later steps include:

- runtime observability and benchmarking
- NodeSet/model workflows
- public polish and demo packaging
- a later hardened runtime path

## Status interpretation

StateKernel is already beyond the prototype stage.

It now demonstrates:

- deterministic system design
- simulation/runtime separation
- formal control modeling
- explicit runtime abstraction discipline
- real OPC UA runtime exposure
- profile-driven runtime startup
- product-facing runtime lifecycle orchestration
- one-active-run deterministic execution orchestration
- run-driven OPC UA publication through a real product-facing control API
- explicit active/inactive/faulted lifecycle contracts for runtime and run status
- bounded fault-state visibility through the product-facing APIs
- verified secure-profile startup and discovery guarantees for the bounded secure baseline

The remaining work is no longer "prove the architecture exists."

The remaining work is increasingly about:

- usability
- observability
- benchmarking
- product polish
