# Documentation

This section contains detailed documentation for the ASI Backbone.

Start with [Getting Started](getting-started.md), then use the navigation menu to browse the major documentation areas.

## Application documentation

* [Core Domain Language](core-domain-language.md)
  Defines the Core terminology for governance spine, constraints, collapse boundary, audit residue, actor context, decision results, operation results, acknowledgment, capability tokens, and gateway boundaries.

* [1.0.0 Release Notes](release-notes-100.md)
  Describes the first stable release identity, stable package list, known limitations, and upgrade guidance.

* [Governance Tool Comparisons](governance-tool-comparisons.md)
  Compares Azure Policy, Open Policy Agent (OPA), Microsoft Agent Governance Toolkit, and AsiBackbone as complementary governance layers without positioning any tool as a replacement for the others.

* [Equations and Toy Models](equations-and-toy-models.md)
  Explains the conceptual progression from `Λ(t)` to `Λ(τ)` to `ΛS(x, τ)` and maps the Eden/ASI collapse notation into practical AsiBackbone software terms: active policy structure, allowed decision states, acknowledgment, audit residue, and gateway-safe execution.

* [Historical Alpha Package Boundary](alpha-package-boundary.md)
  Documents the original `0.1.0-alpha.1` boundary for `CDCavell.AsiBackbone.Core`, including what belongs in Core and what belongs in integration packages.

* [EF Core Integration Boundary](ef-core-integration-boundary.md)  
  Defines the implemented boundary for `CDCavell.AsiBackbone.EntityFrameworkCore`, including host-owned `DbContext`, migration ownership, provider-neutral configuration, and `ModelBuilder` extension guidance.

* [EF Core Host Ownership and Migration Guidance](ef-core-host-ownership-and-migrations.md)  
  Explains how host applications own the EF Core `DbContext`, provider, connection string, migrations, schema deployment, and operational lifecycle while applying ASI Backbone model configurations.

* [ASP.NET Core Integration Boundary](aspnetcore-integration-boundary.md)  
  Defines the implemented web-host adapter boundary for `CDCavell.AsiBackbone.AspNetCore`, including service registration, request correlation, audit enrichment, HTTP outcome mapping, acknowledgment challenge helpers, and compatibility with both plain ASP.NET Core and NetCoreApplicationTemplate hosts.
