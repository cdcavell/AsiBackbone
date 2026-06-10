# Documentation

This section contains detailed documentation for the ASI Backbone.

Start with [Getting Started](getting-started.md), then use the navigation menu to browse the major documentation areas.

## Application documentation

* [Core Domain Language and Alpha Boundary](core-domain-language.md)
  Defines the initial Core terminology for governance spine, constraints, collapse boundary, audit residue, actor context, decision results, operation results, acknowledgment, capability tokens, and gateway boundaries.

* [Equations and Toy Models](equations-and-toy-models.md)
  Explains the conceptual progression from `Λ(t)` to `Λ(τ)` to `ΛS(x, τ)` and maps the Eden/ASI collapse notation into practical AsiBackbone software terms: active policy structure, allowed decision states, acknowledgment, audit residue, and gateway-safe execution.

* [Alpha Package Boundary](alpha-package-boundary.md)
  Documents the intended `0.1.0-alpha.1` boundary for `CDCavell.AsiBackbone.Core`, including what belongs in Core and what belongs in later integration packages.

* [EF Core Integration Boundary](ef-core-integration-boundary.md)  
  Defines the intended boundary for the future EF Core storage package, including host-owned `DbContext`, migration ownership, provider-neutral configuration, and `ModelBuilder` extension expectations.

* [EF Core Host Ownership and Migration Guidance](ef-core-host-ownership-and-migrations.md)  
  Explains how host applications own the EF Core `DbContext`, provider, connection string, migrations, schema deployment, and operational lifecycle while applying ASI Backbone model configurations.
