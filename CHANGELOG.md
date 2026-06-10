# Changelog

All notable changes to this project are documented in this file.

This project follows the spirit of [Keep a Changelog](https://keepachangelog.com/) and adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

* Added branch-focused unit tests for `AuditLedgerRecord.FromResidue`.
* Added branch-focused unit tests for `DefaultAsiBackbonePolicyEvaluator<TContext>`.
* Added EF Core `ModelBuilder` extension support for host-owned persistence integration.
* Added `ApplyAsiBackboneConfigurations(this ModelBuilder modelBuilder)` for applying AsiBackbone EF Core model contributions from a consuming application's `DbContext`.
* Added tests proving the extension can be called from a host-owned `DbContext`.
* Added argument validation for null `ModelBuilder` usage.
* Added provider-neutral EF Core persistence entities and configurations for audit ledger records, reason codes, metadata, handshake requests, and handshake acknowledgments.
* Added EF Core tests proving host-owned DbContext integration, model metadata, keys, relationships, indexes, enum conversion, and basic persistence behavior.

### Fixed

* Updated EF Core documentation samples to show host applications calling the extension from `OnModelCreating`.

### Notes

* The EF Core integration preserves host ownership of the `DbContext`, database provider, connection string, migrations, deployment process, and schema lifecycle.
* AsiBackbone contributes model configuration; the consuming application remains the persistence composition root.
* Wired the configurations through the existing `ApplyAsiBackboneConfigurations` `ModelBuilder` extension.

## [0.1.0-alpha.2] - 2026-06-09

### Added

* Added a host-neutral Core policy evaluator contract and default policy evaluator implementation.
* Added a decision policy extension point for raising composed decisions to deferred, acknowledgment-required, or escalation-recommended outcomes.
* Added an audit sink contract for writing audit residue without requiring a database or web host.
* Added an in-memory audit ledger project for local validation, samples, and tests.
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
