# Privacy and Signing Boundaries

This article documents the `1.0.0` release boundary for metadata privacy, identifier handling, signing-ready fields, host responsibilities, and later provider work.

In this software project, **ASI** means **Accountable Systems Infrastructure**. AsiBackbone provides governance-oriented software building blocks. It does not provide legal, compliance, privacy, security, or cryptographic guarantees by itself.

> [!IMPORTANT]
> The `1.0.0` package family is signing-ready in shape, but it does not implement a signing provider, key-management provider, tamper-evident ledger, privacy classifier, or compliance system.

## `1.0.0` boundary summary

The initial stable release focuses on explicit governance records and host-owned integration seams.

| Area | `1.0.0` behavior | Not included in `1.0.0` |
| --- | --- | --- |
| Metadata | Host-provided dictionaries and values can flow into contexts, decisions, audit residue, and ledger records. | Automatic classification, redaction, encryption, tokenization, or privacy scanning. |
| Identifiers | Correlation IDs, trace IDs, event IDs, record IDs, actor IDs, policy versions, and policy hashes are available for linking records. | Automatic pseudonymization, identity-proofing, cross-system identity governance, or secret handling. |
| Signing-ready fields | Records include fields such as policy version, policy hash, schema version, timestamps, correlation IDs, and record IDs that can support later signing. | Built-in cryptographic signing, signature verification, key rotation, certificate policy, or non-repudiation guarantees. |
| Persistence | EF Core integration supports host-owned persistence through the host application database. | Package-owned database lifecycle, retention policy, encryption-at-rest enforcement, backup policy, or immutable storage. |
| Audit | Audit residue and ledger records make decision flow easier to inspect. | Tamper-evidence, regulatory audit certification, legal evidence guarantees, or automated compliance approval. |

## Metadata privacy boundary

AsiBackbone treats metadata as host-owned data. The package does not know whether a metadata value is public, internal, confidential, regulated, personal, or secret.

The host application should classify and minimize metadata before passing it into AsiBackbone APIs.

Recommended metadata shape:

```csharp
metadata: new Dictionary<string, string>(StringComparer.Ordinal)
{
    ["region"] = "US-LA",
    ["risk"] = "routine",
    ["workflow"] = "document-approval"
}
```

Avoid placing secrets, credentials, raw personal data, protected records, or other sensitive values directly in metadata.

Prefer stable but non-sensitive codes:

| Prefer | Avoid |
| --- | --- |
| `region = US-LA` | full address details when not needed |
| `risk = consequential` | free-form incident details containing personal data |
| `workflow = document-approval` | full document body |
| `resourceType = purchase-order` | complete financial details |
| opaque host-owned subject ID | direct personal identifiers when not required |

## Identifier handling boundary

AsiBackbone exposes identifiers so a host can connect decision flow to logs, audit records, and operational records.

Common identifiers include:

- `EventId`
- `RecordId`
- `CorrelationId`
- `TraceId`
- `ActorId`
- `PolicyVersion`
- `PolicyHash`
- `SchemaVersion`

These identifiers are not automatically private. They may become sensitive when they can be linked to a person, tenant, agency, customer, protected workflow, or confidential resource.

Host guidance:

- Use opaque identifiers where possible.
- Do not use raw secrets as identifiers.
- Do not place credentials, security tokens, or confidential key material in any identifier field.
- Treat actor identifiers as potentially personal data when they can be mapped to a person.
- Treat correlation and trace identifiers as operational data that may appear in logs.
- Keep tenant, jurisdiction, and resource identifiers no more specific than the decision requires.

## Signing-ready does not mean signed

The `1.0.0` model includes fields that are useful for future signing and verification workflows. Examples include policy version, policy hash, schema version, event ID, record ID, timestamp, correlation ID, and trace ID.

Those fields make records easier to bind together later, but they do not create cryptographic protection by themselves.

Do not describe a `1.0.0` audit record as:

- cryptographically signed;
- tamper-proof;
- tamper-evident;
- immutable;
- non-repudiable;
- legally certified;
- compliance-approved.

Accurate `1.0.0` wording:

- signing-ready fields are present;
- records are structured for future signing providers;
- durable records can carry schema and policy version information;
- hosts may apply their own signing, hashing, immutable storage, or retention controls outside the package.

## Host responsibilities

The consuming application remains responsible for privacy, security, compliance, and operational policy.

Before using AsiBackbone in a production or regulated environment, the host should decide:

- which metadata fields are allowed;
- which metadata fields must be redacted, hashed, tokenized, or omitted;
- whether actor IDs are personal data;
- how long audit records are retained;
- who can read audit records;
- whether durable records require encryption at rest;
- whether logs include correlation IDs or decision metadata;
- whether metadata needs privacy review before persistence;
- whether records must be copied to immutable storage;
- whether signing, verification, or timestamp authority is required;
- how keys, certificates, secrets, and rotation are managed;
- how data deletion, discovery, and retention obligations are handled.

AsiBackbone should sit inside the host's existing security, privacy, and compliance program. It does not replace that program.

## EF Core and persistence boundary

`CDCavell.AsiBackbone.EntityFrameworkCore` provides model configuration and storage helpers. The host application owns the `DbContext`, provider, connection string, database permissions, migrations, schema deployment, backups, retention, and encryption choices.

For production persistence, the host should review:

- database access controls;
- encryption at rest;
- encryption in transit;
- backup and restore behavior;
- tenant isolation;
- retention and purge processes;
- administrative access;
- operational logging;
- migration review;
- incident response requirements.

See [EF Core Host Ownership and Migrations](ef-core-host-ownership-and-migrations.md) for host-owned persistence expectations.

## ASP.NET Core boundary

`CDCavell.AsiBackbone.AspNetCore` provides thin host adapters for request correlation, actor context, HTTP result mapping, and acknowledgment challenge helpers.

The ASP.NET Core package does not own:

- authentication;
- authorization;
- claims transformation;
- consent notices;
- privacy notices;
- session management;
- endpoint exposure;
- route protection;
- UI rendering;
- external execution.

The host must decide which requests are allowed to reach AsiBackbone and which actors may view or act on decisions and audit records.

## Later provider work

Later `1.1.0` or preview provider packages may add specialized implementations, such as:

- signing providers;
- key-management adapters;
- external immutable ledger adapters;
- outbox and external emission patterns;
- cloud observability enrichment;
- governance catalog or lineage enrichment;
- privacy review hooks;
- gateway-specific providers.

Those providers are outside the `1.0.0` stable boundary unless they complete their own stable API review and are explicitly released as stable packages.

Future provider documentation should state whether a provider is:

- stable;
- preview;
- experimental;
- sample-only;
- host-owned integration guidance.

## Release wording checklist

Use this checklist when preparing `1.0.0` release notes or docs:

- State that AsiBackbone provides Accountable Systems Infrastructure, not artificial superintelligence.
- State that metadata is host-owned.
- State that hosts must classify, minimize, redact, or omit sensitive metadata before passing it into package APIs.
- State that signing-ready fields are present, but built-in signing is future provider work.
- Avoid claims of tamper-evidence unless signing or immutable storage is actually implemented and documented.
- Avoid claims of regulatory compliance or legal non-repudiation.
- Keep provider, cloud, signing, outbox, and gateway behavior separate from the `1.0.0` stable package boundary unless explicitly released as stable.

## Related documentation

- [1.0.0 Quickstart](quickstart-100.md)
- [API Compatibility and SemVer](api-compatibility-and-semver.md)
- [Schema Versioning](schema-versioning.md)
- [EF Core Host Ownership and Migrations](ef-core-host-ownership-and-migrations.md)
- [ASP.NET Core Integration Boundary](aspnetcore-integration-boundary.md)
