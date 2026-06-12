# Changelog

All notable changes to this project are documented in this file.

This project follows the spirit of [Keep a Changelog](https://keepachangelog.com/) and adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.4.0-alpha.2] - 2026-06-12

### Added

* Added Stryker.NET as a local .NET tool for mutation-analysis validation.
* Added an initial Core test-project Stryker configuration for evaluator and policy-pipeline mutation testing.
* Added an ASP.NET Core test-project Stryker configuration for acknowledgment challenge mutation testing.
* Added mutation-focused ASP.NET Core acknowledgment challenge tests covering safe-default challenge shaping and response conversion.
* Added a Quality Reports landing page for coverage and mutation-analysis reports.
* Added Quality to the DocFX top navigation.

### Changed

* Updated the documentation publishing workflow to generate and publish the Core mutation report alongside the existing coverage report.
* Updated the release/manual quality workflow to generate separate Core and ASP.NET Core mutation reports.
* Updated DocFX content configuration so the Quality Reports landing page is included in the documentation site.

### Documentation

* Clarified that ASI means **Accountable Systems Infrastructure** within the AsiBackbone software project.
* Updated the README, documentation index, Getting Started guide, Why AsiBackbone article, and Core domain language article to frame AsiBackbone as governance infrastructure rather than artificial superintelligence.
* Added Accountable Systems Infrastructure to the core domain language and alignment guidance.

### Boundary Notes

* Reinforced that AsiBackbone does not implement artificial superintelligence, host or train AI models, control robots, or prove the Eden/Backbone framework.
* Clarified that broader Eden/Backbone concepts may inspire the package while implementation claims remain limited to practical software governance.

## [0.4.0-alpha.1] - 2026-06-11

### Samples and Host Validation

### Added

* Added `samples/PlainAspNetCoreHost` as the canonical in-repository ASP.NET Core validation sample.
* Added a plain ASP.NET Core sample project demonstrating:

  * `AddAsiBackboneAspNetCore()` registration.
  * host-defined constraint evaluation.
  * host-defined decision policy behavior.
  * acknowledgment-required decision flow.
  * in-memory audit residue capture.
  * EF Core audit ledger persistence through a host-owned `DbContext`.
  * SQLite-based local validation.
* Added sample documentation for the plain ASP.NET Core host.
* Added DocFX article for the plain ASP.NET Core host sample.
* Added DocFX article documenting `NetCoreApplicationTemplate` as an optional external local validation host.
* Added package-reference and local project-reference guidance for validating AsiBackbone against a `NetCoreApplicationTemplate`-generated host.
* Added host-owned EF Core integration guidance showing `ApplyAsiBackboneConfigurations()`.
* Added temporary validation endpoint sketch for external host validation.
* Added targeted branch coverage tests for:

  * ASP.NET Core options.
  * request correlation resolution.
  * acknowledgment challenge handling.
  * EF Core audit ledger edge paths.

### Changed

* Updated documentation navigation to include sample and host-validation guidance.
* Updated README links to reference the new sample and host-validation documentation.
* Clarified that the plain ASP.NET Core host sample is the canonical in-repository validation baseline.
* Clarified that `NetCoreApplicationTemplate` is a preferred external validation host, not a required dependency or parent framework.

### Validation

* Confirmed the solution builds successfully in Release configuration.
* Confirmed the full test suite passes in Release configuration.
* Regenerated local coverage after targeted branch coverage additions.

### Boundary Notes

* No AsiBackbone project references `NetCoreApplicationTemplate`.
* No in-repository `NetCoreApplicationTemplate` sample was added.
* `NetCoreApplicationTemplate` remains optional and external.
* AsiBackbone remains governance infrastructure, not an intelligence engine, AI model host, or ASI implementation.

## [0.3.0-alpha.1] - 2026-06-11

### Added

* Added the initial `CDCavell.AsiBackbone.AspNetCore` alpha integration package.
* Added ASP.NET Core service registration extensions through `AddAsiBackboneAspNetCore(...)`.
* Added configurable ASP.NET Core integration options with startup validation.
* Added an HTTP actor context adapter for resolving Core-compatible actor context from `HttpContext.User`.
* Added configurable claim mapping for actor identifiers, display names, and actor type.
* Added safe unauthenticated actor handling without throwing during normal request flow.
* Added ASP.NET Core request correlation support for resolving correlation identifiers, trace identifiers, and safe request metadata.
* Added audit enrichment helpers for creating Core audit residue from HTTP request correlation data.
* Added HTTP result mapping helpers for Core `GovernanceDecision` and `OperationResult` values.
* Added host-overridable HTTP result mapping options for allowed, warning, denied, deferred, acknowledgment-required, escalation-recommended, and failed operation outcomes.
* Added Problem Details-style responses for non-success governance and operation outcomes.
* Added safe default response behavior that preserves reason codes and correlation identifiers while hiding reason messages, trace identifiers, policy versions, and policy hashes unless explicitly enabled.
* Added ASP.NET Core acknowledgment challenge models and service support for Core `AcknowledgmentRequired` governance decisions.
* Added acknowledgment challenge response handling that round-trips accepted or rejected responses into Core `LiabilityHandshakeAcknowledgment` values.
* Added tests for service registration, actor context resolution, request correlation, audit enrichment, HTTP result mapping, and acknowledgment challenge handling.

### Documentation

* Added ASP.NET Core integration boundary documentation.
* Added ASP.NET Core package README guidance for service registration, request correlation, audit enrichment, HTTP result mapping, and acknowledgment challenge usage.
* Documented the package as a thin web-host adapter around Core governance primitives.
* Documented that hosts remain responsible for authentication, authorization, persistence, routing, UI rendering, endpoint exposure, and operational execution.

### Boundaries

* The ASP.NET Core package keeps Core framework-neutral and free of ASP.NET Core dependencies.
* The ASP.NET Core package does not register EF Core, persistence stores, authentication handlers, MVC, Razor Pages, Minimal API endpoints, middleware enforcement, policy evaluators, or NetCoreApplicationTemplate dependencies by default.
* HTTP result mapping and acknowledgment challenge helpers are explicit host adapters and do not enforce decisions automatically.
* Hosts choose how to render, store, protect, and round-trip acknowledgment challenge state.

### Notes

* This alpha release establishes the first web-host integration layer for the AsiBackbone package family.
* The implementation is intentionally adapter-focused: it translates ASP.NET Core request context into Core governance language and translates Core outcomes into HTTP-friendly shapes when explicitly used by the host.

## [0.2.0-alpha.1] - 2026-06-10

### Added

* Added EF Core `ModelBuilder` extension support for host-owned persistence integration.
* Added `ApplyAsiBackboneConfigurations(this ModelBuilder modelBuilder)` for applying AsiBackbone EF Core model contributions from a consuming application's `DbContext`.
* Added tests proving the extension can be called from a host-owned `DbContext`.
* Added argument validation for null `ModelBuilder` usage.
* Added provider-neutral EF Core persistence entities and configurations for audit ledger records, reason codes, metadata, handshake requests, and handshake acknowledgments.
* Added an EF Core-backed audit ledger store for append-oriented accountability persistence through a host-owned `DbContext`.
* Added EF Core tests proving host-owned DbContext integration, model metadata, keys, relationships, indexes, enum conversion, and basic persistence behavior.
* Added SQLite-backed EF Core integration tests proving relational schema creation, persistence/readback, and query behavior without package-owned migrations.
* Added EF Core host ownership and migration guidance documentation.
* Added package-specific README files for the EF Core and in-memory storage packages.

### Fixed

* Updated EF Core documentation samples to show host applications calling the extension from `OnModelCreating`.
* Aligned EF Core documentation with the implemented `CDCavell.AsiBackbone.EntityFrameworkCore` package name.
* Normalized EF Core configuration folder and file paths.
* Updated the root README to describe the current 0.2 persistence package status.
* Cleared the EF Core change tracker after audit ledger append failures so failed append entities do not remain tracked in the host-owned context.

### Notes

* The EF Core integration preserves host ownership of the `DbContext`, database provider, connection string, migrations, deployment process, and schema lifecycle.
* AsiBackbone contributes model configuration; the consuming application remains the persistence composition root.
* Wired the configurations through the existing `ApplyAsiBackboneConfigurations` `ModelBuilder` extension.
* The in-memory storage package remains non-durable and intended only for tests, samples, and local validation hosts.

## [0.1.0-alpha.2] - 2026-06-09

### Added

* Added a host-neutral Core policy evaluator contract and default policy evaluator implementation.
* Added a decision policy extension point for raising composed decisions to deferred, acknowledgment-required, or escalation-recommended outcomes.
* Added an audit sink contract for writing audit residue without requiring a database or web host.
* Added an in-memory audit ledger project for local validation, samples, and tests.
* Added branch-focused unit tests for `AuditLedgerRecord.FromResidue`.
* Added branch-focused unit tests for `DefaultAsiBackbonePolicyEvaluator<TContext>`.
* Added end-to-end policy evaluator tests covering allow, deny, warning, acknowledgment-required, escalation-recommended, deferred, and not-applicable constraint scenarios.
* Added policy evaluator pipeline documentation with a minimal in-memory usage example.

### Fixed

* Aligned policy evaluator tests with the intended constraint-versus-decision-policy boundary.
* Preserved elevated-risk warnings as constraint-layer results instead of replacing them in the decision policy layer.
* Updated test expectations to match current `AsiBackbone` assembly casing.
* Added explicit switch handling for low-risk and elevated-risk document policy scenarios.

### Boundaries

* The evaluator remains framework-neutral and does not depend on ASP.NET Core, Entity Framework Core, robotics packages, database providers, or AI model hosting.
* The in-memory ledger is non-durable and intended only for tests, samples, and local validation hosts.

## [0.1.0-alpha.1] - 2026-06-04

### Added

* Introduced the initial `CDCavell.AsiBackbone.Core` alpha package boundary.
* Added framework-neutral domain primitives for governance-oriented decision flow.
* Added actor context primitives for describing who or what is requesting an operation.
* Added entity identity and optimistic-concurrency abstractions.
* Added operation result primitives for package execution outcomes.
* Added reason code primitives for explainable result and decision handling.
* Added constraint evaluation primitives for allow, deny, warning, and not-applicable outcomes.
* Added governance decision primitives for allowed, warning, denied, deferred, acknowledgment-required, and escalation-recommended outcomes.
* Added audit residue primitives for capturing decision traces, reason codes, policy version/hash, correlation data, timestamps, actor data, and metadata.
* Added persistent audit ledger record shape and framework-neutral storage contract.
* Added liability/responsibility handshake primitives for acknowledgment before consequential execution.
* Added capability-token abstractions for scoped, time-bound, traceable permission grants.
* Added assembly marker support for discovery-friendly package references.
* Added XML documentation coverage for public Core types and members.
* Added unit tests for introduced Core primitives.

### Documentation

* Added README documentation describing AsiBackbone as a governance spine rather than an intelligence engine.
* Added Core domain language documentation for the initial alpha boundary.
* Added package boundary documentation clarifying what belongs in Core versus future integration packages.
* Added EF Core integration boundary documentation for future host-owned persistence support.
* Added alpha readiness review documentation.

### Boundaries

* Core does not implement artificial superintelligence.
* Core does not host, train, or run AI models.
* Core does not prove the ASI Backbone concept or the Eden Hypothesis.
* Core does not depend on ASP.NET Core, Entity Framework Core, NetCoreApplicationTemplate, robotics packages, or AI model dependencies.
* Core does not provide middleware, endpoint mapping, database storage, signing implementation, robotics control, or provider-specific persistence behavior.

### Notes

* This alpha release establishes the foundational language and primitives for the AsiBackbone package family.
* Future packages may provide ASP.NET Core integration, EF Core persistence integration, in-memory storage, signing support, samples, and later gateway or robotics examples.
