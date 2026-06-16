# Signing Provider Package Boundary

This article documents the signing-provider package boundary for AsiBackbone.

In this software project, **ASI** means **Accountable Systems Infrastructure**. AsiBackbone is a governance spine for consequential software decision flow. It is not a signing appliance, key-management system, immutable ledger, blockchain service, legal non-repudiation system, or compliance certification service by itself.

> [!IMPORTANT]
> Signing support does not make records tamper-evident, immutable, legally non-repudiable, or compliance-certified by default. Those claims require concrete signing, verification, durable storage controls, key management, retention, monitoring, and operational procedures supplied by the host or provider environment.

## Purpose

Core exposes provider-neutral signing and verification seams while concrete key-management integrations live outside `CDCavell.AsiBackbone.Core`.

The signing-provider boundary exists so hosts can choose a local-development signer, managed-key provider, HSM-backed provider, or organization-owned signing implementation without forcing every AsiBackbone consumer to take dependencies on Azure, cloud KMS SDKs, certificate stores, hardware security modules, local key files, or blockchain services.

## Current source package posture

Current source includes the following signing-provider package projects:

| Package | Purpose | Production posture |
| --- | --- | --- |
| `CDCavell.AsiBackbone.Signing.LocalDevelopment` | Local-development RSA signing and verification for tests, samples, and wiring proof paths. | Not production. No protected key custody. |
| `CDCavell.AsiBackbone.Signing.ManagedKey` | Provider-neutral managed-key signing adapter that delegates actual signing to a host-owned managed-key client. | Production-capable only when the host supplies a secure managed-key client, credentials, policies, monitoring, and verification path. |

Future provider areas may include Azure Key Vault, Managed HSM, cloud KMS, HSM-backed, or organization-owned signing implementations. Those should remain outside Core and should be reviewed as separate package boundaries.

## Ownership boundaries

| Layer | Owns | Must not own |
| --- | --- | --- |
| Core | `IAsiBackboneSigningService`, `IAsiBackboneSignatureVerificationService`, signing requests/results, signing metadata, provider-neutral canonical payload/hash contracts, and safe metadata fields. | Azure SDKs, HSM SDKs, cloud KMS clients, local key-file production handling, certificate stores, credentials, raw keys, managed identity tokens, immutable storage, blockchain anchoring, legal interpretation, or compliance certification. |
| Signing provider package | Concrete signer/verifier implementation, provider options, DI registration helpers, algorithm mapping, key reference resolution, provider diagnostics, and provider-specific failure handling. | Host policy decisions, host authentication, host authorization, durable audit ownership, database migrations, retention policy, or claims that records are tamper-evident by default. |
| Host application | Configuration source, key identity, provider credentials or managed identity, production failure policy, logging policy, persistence lifecycle, deployment, monitoring, and incident response. | Requiring Core to know provider secrets or making Core responsible for operational key management. |
| Samples/tests | Non-production proof path for wiring, fake/local signatures, and stable metadata assertions. | Production security claims. |

## Required provider responsibilities

A concrete signing provider should:

- accept a `SigningRequest` containing a precomputed artifact hash;
- validate that the requested hash algorithm is supported;
- sign only the precomputed hash, not raw protected content;
- return `SigningResult` containing provider-neutral `SigningMetadata`;
- preserve `SigningHash`, `HashAlgorithm`, `Signature`, `SignatureAlgorithm`, `KeyId`, `KeyVersion`, `Provider`, and `SignedUtc` whenever available;
- include only safe metadata such as provider operation ID, key URI reference, or algorithm descriptor when those values are safe to persist;
- avoid returning private keys, symmetric keys, connection strings, access tokens, credentials, managed identity tokens, or raw secret material to Core;
- honor cancellation tokens;
- avoid logging signing hashes together with sensitive operational metadata unless the host has explicitly accepted that logging posture;
- document whether missing key versions, disabled keys, revoked keys, provider timeouts, unsupported algorithms, and unavailable providers fail closed, throw, or return an unsigned result.

A concrete verifier should:

- verify a supplied signing hash against provider-neutral signing metadata;
- use key ID and key version whenever available;
- distinguish invalid signatures, hash mismatches, missing signatures, unknown key versions, unsupported algorithms, provider unavailability, and revoked or disabled keys where the provider can detect them;
- avoid silently treating unverifiable records as trusted.

## Dependency direction

The dependency graph must stay one-way:

```text
CDCavell.AsiBackbone.Core
        ^
        |
CDCavell.AsiBackbone.Signing.LocalDevelopment
        ^
        |
Host application, sample, or test
```

```text
CDCavell.AsiBackbone.Core
        ^
        |
CDCavell.AsiBackbone.Signing.ManagedKey
        ^
        |
Host-owned managed-key client
```

Core must never reference the provider package. Provider packages reference Core.

## Recommended signing flow

```text
Governance artifact
  -> build canonical payload
  -> compute canonical payload hash
  -> create SigningRequest from hash metadata
  -> provider signs precomputed hash
  -> provider returns SigningMetadata
  -> host attaches metadata to selected audit/outbox artifact
  -> host persists durable record
  -> optional verifier later checks hash + signature + key reference
```

Hashing and signing should remain separate steps. A hashed record is not signed. A signed record is not verified until verification runs. A signed and verified record is not automatically tamper-evident unless durable storage, chain/anchor strategy, key handling, and operational procedures support that claim.

## Local-development signer boundary

`CDCavell.AsiBackbone.Signing.LocalDevelopment` is useful for:

- proving DI registration and host wiring;
- exercising signing metadata flow in samples;
- running deterministic tests without live cloud resources;
- documenting how a provider maps request fields into `SigningMetadata`.

It must remain clearly marked:

- local only;
- sample/test only;
- not backed by protected key storage;
- not appropriate for production tamper-evidence;
- not proof of non-repudiation, compliance, immutability, or legal effect.

## Managed-key provider boundary

`CDCavell.AsiBackbone.Signing.ManagedKey` provides an adapter boundary. It does not include Azure Key Vault, Managed HSM, cloud KMS, certificate store, or HSM behavior by default.

A production managed-key host should:

- keep private key material inside the key-management boundary;
- prefer managed identity or host-provided credentials over secrets in configuration;
- record returned or configured key ID and key version;
- document algorithm mapping between Core descriptors and provider-specific algorithms;
- document retry and timeout behavior;
- keep live provider integration tests optional and disabled by default;
- document key rotation and retired-key verification behavior;
- document how verification is performed after signing.

## Failure handling guidance

Provider packages should avoid ambiguous failure behavior.

| Condition | Recommended default |
| --- | --- |
| Missing key ID | Throw configuration exception during provider setup or first signing call. |
| Missing key version when required | Fail closed or return explicit unsigned failure metadata according to host policy. |
| Unsupported hash algorithm | Fail the signing operation; do not silently sign under a different algorithm. |
| Unsupported signature algorithm | Fail the signing operation; document supported descriptors. |
| Provider unavailable | Fail closed for production-required signing unless the host explicitly allows optional signing. |
| Local-development signer used in production | Warn in documentation and package naming; hosts remain responsible for environment gating. |
| Raw key material requested by Core | Not allowed. Core should never request it. |

`SigningResult.NoSignature` or unsigned failure metadata should be reserved for intentionally unsigned or optional-signing flows. Production-required signing should not silently downgrade to unsigned behavior.

## Non-goals

This boundary does not provide:

- Azure Key Vault or Managed HSM integration by default;
- automatic key rotation;
- automatic verification policy;
- immutable storage;
- blockchain anchoring;
- legal non-repudiation;
- compliance certification;
- tamper-proof or tamper-evident records by default.

## Related articles

- [Managed-Key Signing Provider](managed-key-signing-provider.md)
- [Signing-Ready Receipts and Key Handling](signing-ready-receipts-and-key-handling.md)
- [Signed Audit and Outbox Records](signed-audit-and-outbox-records.md)
- [Verification Policy and Result Handling](verification-policy-and-result-handling.md)
- [Key Rotation and Retired-Key Verification](key-rotation-and-retired-key-verification.md)
- [Cryptographic Security Posture and Production Guidance](cryptographic-security-posture.md)
