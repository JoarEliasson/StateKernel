# ADR 0019: Bounded Secure Profile Hardening

## Status

Accepted

## Context

StateKernel already had a bounded two-profile runtime endpoint/security seam:

- `local-dev`
- `baseline-secure`

The project needed stronger trust guarantees for `baseline-secure` without turning the runtime
platform into a full enterprise PKI or security-matrix product.

The most important requirement was that the secure profile remain real and enforceable, not
merely metadata attached to runtime startup.

## Decision

`baseline-secure` remains a bounded but strong secure profile:

- secure endpoint only
- `SignAndEncrypt`
- `Basic256Sha256`
- no insecure fallback endpoint

Secure interpretation remains adapter-local in `StateKernel.Runtime.UaNet`.
The generic runtime host validates profile support, but it does not interpret OPC UA security
semantics itself.

The UA adapter now performs post-start endpoint-set verification:

- it discovers the actual exposed endpoint set after startup
- it verifies every exposed endpoint matches the bounded secure contract
- it verifies no insecure endpoint remains exposed
- if verification fails, startup is rolled back and the adapter/runtime remain inactive

Certificate/bootstrap handling also remains adapter-local.
This phase improves cleanup and restart behavior for secure startup failures, but does not add:

- enterprise PKI workflows
- trust-store UX
- authorization models
- profile matrices

## Consequences

Benefits:

- the secure profile is stronger and more credible
- secure startup is verified against actual exposed endpoints, not only intended configuration
- failure paths leave the runtime inactive and reusable for a later clean start

Deferred:

- broader secure-policy/profile matrices
- enterprise certificate management
- full secure client-session regression requirements
