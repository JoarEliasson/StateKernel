# Architecture Decision Records

Architecture Decision Records capture durable technical decisions that shape the structure, behavior, or workflow of StateKernel.

## Naming and Numbering

- Use four-digit numeric prefixes such as `0001`, `0002`, and `0003`.
- Follow the numeric prefix with a concise kebab-case title.
- Keep numbers sequential and never reuse a retired number.

Example:

- `0001-establish-runtime-abstraction-boundary.md`

## When to Add an ADR

Add an ADR when a decision has meaningful architectural impact, introduces a tradeoff worth preserving, or defines a convention that future contributors should understand.

## How to Add a New ADR

1. Copy [`0000-adr-template.md`](0000-adr-template.md).
2. Rename the file with the next available number and a short title.
3. Fill in the context, decision, and consequences.
4. Link related ADRs when a newer decision supersedes or refines an older one.

## Status Values

Common statuses include `Proposed`, `Accepted`, `Superseded`, and `Deprecated`.
