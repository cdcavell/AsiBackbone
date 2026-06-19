# CDCavell.AsiBackbone.Storage.InMemory

Non-durable in-memory storage helpers for Accountable Systems Infrastructure local validation, samples, and tests.

This package provides non-durable storage implementations that make it easy to exercise ASI Backbone governance flows without requiring a database, EF Core provider, or host infrastructure.

> **New to AsiBackbone?** Start with the concept, not this package: [Intent to Execution: An Accountability Pattern](https://cdcavell.github.io/AsiBackbone/articles/intent-to-execution-pattern.html) and the [documentation site](https://cdcavell.github.io/AsiBackbone/). This README covers one package in the family.

> **Important:**
> This package is not durable storage. Do not use it as a production audit ledger, compliance archive, tamper-evident store, or long-term accountability store.

## What this package provides

- In-memory audit ledger behavior for local validation and tests.
- Storage implementations that depend on `CDCavell.AsiBackbone.Core` only.
- A simple bridge for samples that need audit records without introducing EF Core or a database.

## Intended usage

Use this package when:

- writing unit tests or integration tests around policy evaluation;
- building sample applications;
- validating audit residue and audit ledger behavior locally;
- demonstrating host-neutral ASI Backbone flows before adding durable storage.

For production persistence, use a host-owned durable storage strategy such as `CDCavell.AsiBackbone.EntityFrameworkCore` or a custom implementation of the Core storage contracts.

## Boundary

The in-memory package should remain lightweight and host-neutral. It should not select a database provider, own migrations, expose ASP.NET Core middleware, or imply durable compliance-grade audit storage.
