# Signing Provider Package Boundary

This article documents the first concrete signing-provider package boundary for AsiBackbone.

Issue: #220. Parent roadmap: #207. Depends on: #219.

In this software project, **ASI** means **Accountable Systems Infrastructure**. AsiBackbone is a governance spine for consequential software decision flow. It is not a signing appliance, key-management system, immutable ledger, blockchain service, legal non-repudiation system, or compliance certification service by itself.

> [!IMPORTANT]
> This document defines the provider package boundary before implementation proceeds. It does not introduce a production signer, managed-key integration, HSM integration, immutable storage provider, or tamper-evidence guarantee.

## Purpose

Core should expose provider-neutral signing and verification seams, while concrete key-management integrations live outside `CDCavell.AsiBackbone.Core`.

The signing-provider boundary exists so hosts can choose a local-development signer, managed-key provider, HSM-backed provider, or organization-owned signing implementation without forcing every AsiBackbone consumer to take dependencies on Azure, cloud KMS SDKs, certificate stores, hardware security modules, local key files, or blockchain services.

## Package naming direction

Recommended package naming should keep concrete providers visibly outside Core:

| Candidate package | Purpose | Production posture |
| --- | --- | --- |
| `CDCavell.AsiBackbone.Signing.LocalDevelopment` | Local-only development/sample signer used for demos, integration tests, and host wiring proof paths. | Not production. |
| `CDCavell.AsiBackbone.Signing.AzureKeyVault` | Future managed-key provider using Azure Key Vault or Managed HSM key operations. | Production-capable only after dedicated implementation, tests, key-rotation guidance, and operational documentation. |
| `CDCavell.AsiBackbone.Signing.ManagedKey` | Optional generic package name if a provider-neutral managed-key abstraction is later justified. | Future design only. |
| Host-owned package or application assembly | Organization-specific signer or verifier implemented inside the consuming system. | Host-owned. |

The first implementation should favor `CDCavell.AsiBackbone.Signing.LocalDevelopment` or a sample-only host implementation before any managed-key provider is introduced. That lets the signing flow be validated without implying production security.

## Ownership boundaries

| Layer | Owns | Must not own |
| --- | --- | --- |
| Core | `IAsiBackboneSigningService`, `IAsiBackboneSignatureVerificationService`, signing requests/results, signing metadata, provider-neutral canonical payload/hash contracts, and safe metadata fields. | Azure SDKs, HSM SDKs, cloud KMS clients, local key-file production handling, certificate stores, credentials, raw keys, managed identity tokens, immutable storage, blockchain anchoring, legal interpretation, or compliance certification. |
| Signing provider package | Concrete signer/verifier implementation, provider options, DI registration helpers, algorithm mapping, key reference resolution, provider diagnostics, and provider-specific failure handling. | Host policy decisions, host authentication, host authorization, durable audit ownership, database migrations, retention policy, or claims that records are tamper-evident by default. |
| Host application | Configuration source, key identity, provider credentials or managed identity, production failure policy, logging policy, persistence lifecycle, deployment, monitoring, and incident response. | Requiring Core to know provider secrets or making Core responsible for operational key management. |
| Samples/tests | Non-production proof path for wiring, fake/local signatures, and stable metadata assertions. | Production security claims. |

## Required provider responsibilities

A concrete signing provider should:

* accept a `SigningRequest` containing a precomputed artifact hash;
* validate that the requested hash algorithm is supported;
* sign only the precomputed hash, not raw protected content;
* return `SigningResult` containing provider-neutral `SigningMetadata`;
* preserve `SigningHash`, `HashAlgorithm`, `Signature`, `SignatureAlgorithm`, `KeyId`, `KeyVersion`, `Provider`, and `SignedUtc` whenever available;
* include only safe metadata such as provider operation ID, key URI reference, or algorithm descriptor when those values are safe to persist;
* avoid returning private keys, symmetric keys, connection strings, access tokens, credentials, managed identity tokens, or raw secret material to Core;
* honor cancellation tokens;
* avoid logging signing hashes together with sensitive operational metadata unless the host has explicitly accepted that logging posture;
* document whether missing key versions, disabled keys, revoked keys, provider timeouts, unsupported algorithms, and unavailable providers fail closed, throw, or return an unsigned result.

A concrete verifier should:

* verify a supplied signing hash against provider-neutral signing metadata;
* use key ID and key version whenever available;
* distinguish invalid signatures, hash mismatches, missing signatures, unknown key versions, unsupported algorithms, provider unavailability, and revoked or disabled keys where the provider can detect them;
* avoid silently treating unverifiable records as trusted.

## Configuration seams

Provider packages should define options in their own namespace rather than adding provider-specific settings to Core.

Illustrative local-development options:

```csharp
public sealed class AsiBackboneLocalDevelopmentSigningOptions
{
    public string ProviderName { get; set; } = "local-development";
    public string KeyId { get; set; } = "local-dev-key";
    public string KeyVersion { get; set; } = "dev";
    public string SignatureAlgorithm { get; set; } = "LOCAL-DEV-SIGNATURE-V1";
    public bool AllowUnsignedOnFailure { get; set; }
}
```

Illustrative managed-key options:

```csharp
public sealed class AsiBackboneManagedKeySigningOptions
{
    public string ProviderName { get; set; } = "managed-key";
    public string KeyId { get; set; } = string.Empty;
    public string? KeyVersion { get; set; }
    public string SignatureAlgorithm { get; set; } = string.Empty;
    public bool RequireKeyVersion { get; set; } = true;
    public bool FailClosedOnProviderUnavailable { get; set; } = true;
}
```

These examples describe the configuration seam only. They should not be treated as stable API until a provider package is implemented and reviewed.

## Dependency direction

The dependency graph should stay one-way:

```text
CDCavell.AsiBackbone.Core
        ^
        |
CDCavell.AsiBackbone.Signing.LocalDevelopment
        ^
        |
Host application or sample
```

A managed-key provider should follow the same rule:

```text
CDCavell.AsiBackbone.Core
        ^
        |
CDCavell.AsiBackbone.Signing.AzureKeyVault
        ^
        |
Host application
```

Core must never reference the provider package. Provider packages reference Core.

## Recommended signing flow

The recommended flow after canonical hashing is available is:

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

A local-development signer may be useful for:

* proving DI registration and host wiring;
* exercising signing metadata flow in samples;
* running deterministic tests without live cloud resources;
* documenting how a provider should map request fields into `SigningMetadata`.

A local-development signer must be clearly marked:

* local only;
* sample/test only;
* not backed by protected key storage;
* not appropriate for production tamper-evidence;
* not proof of non-repudiation, compliance, immutability, or legal effect.

The package README and XML comments should include this warning prominently if the package is implemented.

## Managed-key provider boundary

A future managed-key provider, such as Azure Key Vault or Managed HSM, should:

* use provider APIs that keep private key material inside the key-management boundary;
* prefer managed identity or host-provided credentials over secrets in configuration;
* record the returned or configured key ID and key version;
* document algorithm mapping between Core descriptors and provider-specific algorithms;
* document retry behavior and timeout behavior;
* include no-live-cloud unit tests with fake provider clients;
* keep live provider integration tests optional and disabled by default;
* document key rotation and retired-key verification behavior in the provider README.

## Failure handling guidance

Provider packages should avoid ambiguous failure behavior. At minimum, document how these cases behave:

| Condition | Recommended default |
| --- | --- |
| Missing key ID | Throw configuration exception during provider setup or first signing call. |
| Missing key version when required | Fail closed and preserve a safe diagnostic. |
| Unsupported hash algorithm | Fail the signing operation; do not silently sign under a different algorithm. |
| Unsupported signature algorithm | Fail the signing operation; document supported descriptors. |
| Provider unavailable | Fail closed for production providers unless host explicitly chooses optional signing. |
| Local-development signer used in production | Warn in documentation and package naming; hosts remain responsible for environment gating. |
| Raw key material requested by Core | Not allowed. Core should never request it. |

The existing `SigningResult.NoSignature` path should be reserved for intentionally unsigned or optional-signing flows. Production-required signing should not silently downgrade to unsigned behavior.

## Test and sample expectations

If a provider implementation is included in a later issue or PR, it should include:

* unit tests proving a valid `SigningRequest` returns complete provider-neutral `SigningMetadata`;
* tests proving key ID, key version, provider, hash algorithm, and signature algorithm are preserved;
* tests proving cancellation is honored;
* tests for unsupported algorithms and missing configuration;
* tests proving no raw key material is surfaced in metadata;
* sample host wiring showing registration and a clearly marked local-only signer when applicable.

No concrete provider implementation is included by this boundary document.

## Non-goals

This boundary does not provide:

* production signing by default;
* Azure Key Vault, HSM, local key-file, certificate-store, cloud KMS, blockchain, or immutable-storage integration;
* automatic key rotation;
* automatic verification policy;
* append-only audit-chain verification;
* legal non-repudiation;
* compliance certification;
* tamper-proof or tamper-evident records by default.

## Implementation readiness checklist

Before creating a concrete provider package, confirm:

- [ ] Core remains provider-neutral.
- [ ] Provider package references Core, not the reverse.
- [ ] Package name clearly communicates provider scope.
- [ ] README warns when a provider is local-development/sample only.
- [ ] Options keep key identifiers, versions, provider descriptors, and algorithm choices explicit.
- [ ] Provider never returns raw key material to Core.
- [ ] Failure behavior is documented and tested.
- [ ] Live cloud or HSM tests are optional and excluded from default CI.
- [ ] Documentation avoids tamper-evidence, immutability, legal, or compliance claims unless supported by a concrete implemented design.
