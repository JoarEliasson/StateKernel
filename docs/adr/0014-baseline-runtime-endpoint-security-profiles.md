# ADR 0014: Baseline Runtime Endpoint and Security Profiles

- Status: Accepted
- Date: 2026-03-30

## Context

StateKernel already had a clean runtime boundary, explicit signal projection, runtime composition,
and a first concrete OPC UA adapter. Runtime startup could decide where to host that adapter, but it
did not yet carry an explicit bounded endpoint/security profile choice.

That made the runtime story weaker than it needed to be for both product credibility and industrial
clarity. The architecture needed an explicit answer to:

- which bounded endpoint/security posture is being requested at runtime startup
- where that choice is modeled
- which layer interprets the profile into OPC UA security behavior

The answer needed to stay bounded. This was not the right time to add a full security matrix,
enterprise PKI workflows, user authorization models, or runtime-wide security configuration DSLs.

## Decision

StateKernel adds a bounded runtime endpoint/security profile seam in
`StateKernel.Runtime.Abstractions`.

The baseline model introduces:

- `RuntimeEndpointProfileId`
- `RuntimeEndpointProfile`
- `RuntimeSecurityMode`
- `RuntimeSecurityPolicy`
- `RuntimeEndpointProfiles`

`RuntimeStartRequest` now carries three separate concerns:

- `CompiledRuntimePlan`: what to expose
- `RuntimeEndpointSettings`: where to host it
- `RuntimeEndpointProfile`: under which bounded endpoint/security profile to host it

The baseline profile catalog contains exactly two accepted profiles:

- `local-dev`
  - insecure by design
  - loopback-oriented
  - intended for low-friction local startup and loopback E2E testing
- `baseline-secure`
  - secure-only by design
  - `SignAndEncrypt`
  - `Basic256Sha256`
  - intended to be the strongest bounded secure profile in this slice

The runtime host validates only whether the selected profile id is supported by the chosen adapter
descriptor. It does not interpret OPC UA security semantics.

Profile interpretation remains adapter-local. For the first concrete path:

- `StateKernel.Runtime.UaNet` maps `local-dev` to an insecure loopback-oriented OPC UA startup
- `StateKernel.Runtime.UaNet` maps `baseline-secure` to a secure-only OPC UA startup with no
  insecure fallback endpoint

## Consequences

StateKernel now has a clearer runtime startup model and a more credible bounded secure runtime
story.

This improves the architecture in several ways:

- runtime composition still decides what to expose
- runtime startup now explicitly decides where and under which bounded profile to expose it
- the host remains generic and adapter-agnostic
- the UA adapter becomes responsible for enforcing the selected secure profile rather than merely
  advertising it

The secure runtime story is stronger, but still intentionally bounded:

- no broad security matrix
- no enterprise PKI workflow
- no trust-store UX
- no user or role authorization model
- no secure client-session E2E coverage requirement in this increment
