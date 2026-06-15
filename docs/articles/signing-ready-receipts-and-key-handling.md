# Signing-Ready Receipts and Key Handling

This article documents the Core-neutral signing and verification strategy for the `1.1.0 - Observability, Outbox, and Governance Emission Providers` milestone.

Issue: #147.

In this software project, **ASI** means **Accountable Systems Infrastructure**. AsiBackbone is a governance spine for consequential software decision flow. It is not a signing product, key-management system, immutable ledger, legal certification system, or compliance guarantee by itself.

> [!IMPORTANT]
> The current Core package is **signing-ready**, not a production tamper-evidence implementation. Production tamper evidence requires a concrete signing provider, protected key management, verification policy, durable storage guarantees, retention policy, and operational procedures outside this Core seam.

For production posture, setup guidance, verification-failure behavior, audit-chain wording, capability-token validation, and security non-goals, see [Cryptographic Security Posture and Production Guidance](cryptographic-security-posture.md).

## Purpose

AsiBackbone audit residue, audit ledger records, capability-token references, outbox entries, and downstream governance emission envelopes need a stable way to carry signing metadata without forcing Core to depend on one key provider.

The signing-ready model gives host applications and future provider packages a neutral contract for signing precomputed artifact hashes, recording signature metadata on audit receipts, carrying key identifier and key version references, and verifying signatures downstream.

## Added Core abstractions

| Type | Role |
| --- | --- |
| `SigningMetadata` | Provider-neutral signing metadata containing signing hash, hash algorithm, signature, signature algorithm, key ID, key version, provider descriptor, signed timestamp, and safe metadata. |
| `SigningRequest` | Provider-neutral request to sign a precomputed artifact hash. |
| `SigningResult` | Provider-neutral result containing signing metadata. |
| `SignatureVerificationRequest` | Provider-neutral request to verify signing metadata against a precomputed artifact hash. |
| `SignatureVerificationResult` | Provider-neutral verification result. |
| `IAsiBackboneSigningService` | Async signing boundary implemented by host applications or future provider packages. |
| `IAsiBackboneSignatureVerificationService` | Async verification boundary implemented by host applications or future provider packages. |

## Audit ledger metadata

`AuditLedgerRecord` can carry signing-ready metadata alongside existing audit and capability-token references:

* `SigningHash`
* `SignatureKeyId`
* `SignatureKeyVersion`
* `SignatureAlgorithm`
* `SignatureValue`
* `SignatureProvider`
* `SignedUtc`
* `SigningMetadata`
* `CapabilityTokenId`

`SignatureKeyId` and `SignatureKeyVersion` are references, not secrets. They should identify the key used for verification without exposing raw signing material.

## Key-handling rules

Signing providers should:

* prefer key-based APIs over retrieving raw signing secrets;
* avoid returning private keys, symmetric keys, connection strings, credentials, or managed identity tokens to Core;
* return only key references, provider descriptors, and signature metadata;
* preserve key version metadata whenever available;
* treat key rotation as provider or host policy;
* avoid claims of tamper evidence unless signing, verification, durable storage, and operational controls are all implemented.

## Verification strategy

`IAsiBackboneSignatureVerificationService` verifies a `SignatureVerificationRequest` containing the expected signing hash and signing metadata.

Core does not require a particular algorithm such as RSA, ECDSA, HMAC, EdDSA, or a provider-specific managed key operation.

## Test seams

The Core tests use fake signer and verifier implementations to prove the seam without live key-management resources. Live Azure Key Vault, HSM, or cloud signing tests should be optional, explicitly configured, and excluded from default CI.

## Related documentation

- [Cryptographic Security Posture and Production Guidance](cryptographic-security-posture.md)
- [Privacy and Signing Boundaries](privacy-and-signing-boundaries.md)
- [Governance Emission Contract](governance-emission-contract.md)
- [Durable Audit and Outbox Persistence](durable-audit-outbox-persistence.md)
- [Audit Residue Observability Schema](audit-residue-observability-schema.md)
- [Observability and Governance Emission Architecture](observability-and-governance-emission-architecture.md)
