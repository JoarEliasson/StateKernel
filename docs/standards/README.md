# Standards

This repository is intended to read like a serious, maintainable .NET product.

## Engineering Rules

- Prefer explicit, domain-centered names over generic helper or utility buckets.
- Keep runtime-specific code out of the simulation core.
- Keep orchestration concerns separate from execution concerns.
- Treat observability and documentation as first-class parts of the product.

## C# Documentation

- Avoid inline and narrative `//` comments in production code.
- Use structured XML documentation comments for public types, interfaces, and important contracts.
- Prefer refactoring over comment-heavy method bodies.

## Testing Stack

- xUnit is the default testing framework.
- Test projects should stay focused and avoid unnecessary dependencies.
- Integration tests should exercise public behavior rather than internal implementation details.

## Documentation Boundary

- `docs/` is reserved for public-safe project documentation.
- `docs-internal/` is reserved for internal planning, prompts, and deeper working material.
