# Schema Versioning

AsiBackbone treats persisted or exported governance artifacts as durable contracts once a host application stores them, exports them, or exposes them to downstream consumers. Those artifacts should carry an explicit schema version so future package releases can add fields, rename shapes through migration adapters, or support parallel readers without guessing which serialized form produced the record.

## Current stable artifact schema

The initial stable artifact schema version is `1.0.0` and is exposed through `AsiBackboneSchemaVersions.StableArtifactsV1`.

The first package-owned stable serialized artifact that carries this version is `AuditLedgerRecord`. It is the persistence-ready audit record shape used by the audit ledger and EF Core storage adapter.

## Default behavior

When an `AuditLedgerRecord` is created without an explicit schema version, the package defaults the record to `AsiBackboneSchemaVersions.StableArtifactsV1`.

Hosts or future package components may provide an explicit schema version when replaying, importing, migrating, or testing alternate artifact shapes. The value is trimmed and preserved.

## Migration expectation

Schema versioning is not the same as provider-specific emission or external telemetry integration. It is the local artifact contract used by hosts to understand persisted or exported records.

When a future release changes a durable artifact shape, the release should:

1. define a new schema version constant,
2. keep readers tolerant of older schema versions where practical,
3. document migration behavior before changing stable payload expectations, and
4. avoid relying on package version alone to infer the serialized shape.

## Host guidance

Host applications that persist or export governance decisions, receipts, audit records, or acknowledgment records should include a schema/version field in any host-defined durable payloads. If the host wraps AsiBackbone records in a larger envelope, the host envelope may have its own version, but it should preserve the underlying artifact schema version as well.

This keeps audit residue, decision receipts, and future governance artifacts migratable after `1.0.0` without pulling provider-specific emission work into the core package line.
