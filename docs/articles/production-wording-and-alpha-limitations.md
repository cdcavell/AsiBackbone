# Production Wording and Stable Signing Boundaries

This article defines the documentation wording boundary for current AsiBackbone capabilities, stable `1.1.0` signing-related package surfaces, durable local/outbox behavior, optional provider integrations, historical alpha limitations, and future production-grade tamper-evidence.

Issue: #148, updated for #253.

In this software project, **ASI** means **Accountable Systems Infrastructure**. AsiBackbone is a governance spine for consequential software decision flow. It is not an artificial superintelligence implementation, AI model host, robot controller, signing appliance, key-management system, compliance product, immutable ledger, cloud governance platform, or legal guarantee by itself.

> [!IMPORTANT]
> Documentation should describe only implemented and released behavior as current behavior. `1.1.0` includes stable Core signing-ready and verification primitives, a stable local-development signing provider, and a stable managed-key adapter boundary. Those released surfaces must still be described with their limits: local-development signing is not production key custody, the managed-key package is an adapter rather than a concrete Azure Key Vault/HSM/KMS implementation, and signing alone does not provide tamper-evidence, immutability, legal non-repudiation, or compliance certification.

## Current-stage wording rules

| Topic | Preferred wording | Avoid wording |
| --- | --- | --- |
| Project identity | `Accountable Systems Infrastructure`, governance spine, decision governance infrastructure | artificial superintelligence implementation, AI model, ASI engine |
| Current audit records | audit residue, audit ledger record, durable local/outbox record when configured | immutable ledger, tamper-proof record, legal evidence guarantee |
| Stable Core signing primitives | signing-ready metadata, canonical payload hashing, provider-neutral signing and verification seams, verification-policy primitives | future-only signing model, cryptographically signed by default, automatic verification |
| Local-development signing provider | released local-development signer for tests, samples, local validation, and wiring proof paths | production signer, production key custody, compliance-grade signer |
| Managed-key adapter boundary | released managed-key signing adapter boundary with host-owned managed-key client and operational policy | built-in Azure Key Vault support, built-in HSM/KMS implementation, automatic production trust |
| Tamper-evidence | signing-ready, signed, verified, chained, or externally anchored only when the deployed pieces exist and are described narrowly | tamper-evident, tamper-proof, immutable, or non-repudiable by default |
| Provider integrations | optional provider package, host adapter, downstream emission or enrichment | required platform, Core dependency, authoritative audit store by default |
| Cloud observability | operational search, alerting, dashboards, and diagnostics after local persistence | substitute for durable local accountability |
| Purview | strategy-only governance, catalog, classification, and lineage enrichment unless later released | raw audit ledger by default, released Purview package in `1.1.0` |
| Robotics or gateway work | future or sample gateway pattern unless released | direct ASI-to-robot control implementation |

## Released signing categories

Use these categories when documenting `1.1.0` signing-related behavior.

| Category | Current status | Safe wording |
| --- | --- | --- |
| Core signing-ready and verification primitives | Stable in `CDCavell.AsiBackbone.Core`. | Core provides canonical payload hashing, signing-ready metadata, signing request/result contracts, verification request/result contracts, and verification-policy primitives. |
| Local-development signing provider | Stable package: `CDCavell.AsiBackbone.Signing.LocalDevelopment`. | The local-development signer is for tests, samples, deterministic local validation, and host wiring proof paths. It is not production key custody. |
| Managed-key signing adapter | Stable package: `CDCavell.AsiBackbone.Signing.ManagedKey`. | The managed-key package provides an adapter boundary. The host supplies the actual managed-key client, credentials, key operations, verification path, monitoring, and operational policy. |
| Concrete cloud/HSM/KMS implementation packages | Future or host-owned unless separately released. | Azure Key Vault, Managed HSM, cloud KMS, certificate-store, HSM-backed, or organization-specific clients remain host-owned implementations or future provider packages. |
| Production tamper-evidence, immutability, external anchoring, or legal non-repudiation | Not provided by default. | These claims require deployed signing, verification, protected key management, durable append-only or otherwise controlled storage, retention policy, monitoring, incident response, and any chain/anchor design the claim depends on. |

Do not imply that signing and verification are purely future work. Do not imply that the released signing surfaces provide production tamper-evidence by themselves.

## Required qualifiers for future or provider work

Use explicit qualifiers when behavior is not part of the currently released stable package surface:

- `planned`;
- `optional`;
- `preview`;
- `design-only`;
- `strategy-only`;
- `sample-only`;
- `host-owned`;
- `provider-specific`;
- `future provider package`;
- `when configured`;
- `after durable local persistence`;
- `after signing, verification, key-management, and storage guarantees are implemented`.

Avoid wording that implies future provider work is required for Core consumers.

## Tamper-evidence wording boundary

A record may be described as **signing-ready** when it carries stable identifiers, policy version, policy hash, schema version, timestamps, deterministic canonical payload/hash metadata, or signing metadata fields that can support signing.

A record may be described as **signed** only when a concrete signer has produced a signature over a deterministic payload or hash and the signature metadata is stored with the record or attached signed-artifact projection.

A record may be described as **verified** only when a verification service checks the signature against the expected payload or hash, algorithm, key identity, key version, and verification policy.

A record should not be described as **tamper-evident** unless documentation also identifies:

- deterministic payload hashing or canonicalization;
- signing provider and key protection model;
- verification behavior;
- key rotation and retired-key verification policy;
- durable storage expectations;
- chain, anchoring, continuity, object-lock, or append-only behavior if deletion, reordering, or external proof is claimed;
- host operational controls.

When those pieces are not implemented, use **signing-ready**, **signed**, **verified**, **hash-linked**, or **tamper-evident-ready** only when that narrower phrase is accurate.

## Durable local accountability

External providers can enrich, search, alert, stream, classify, catalog, or display governance events. They should not be described as replacing durable local accountability unless the host explicitly designs and documents them as the authoritative audit store.

Default wording should preserve this sequence:

```text
Decision or acknowledgment
  -> audit residue / governance record
  -> durable local store or outbox when configured
  -> optional signing or verification boundary when configured
  -> optional provider emission
  -> provider-specific enrichment, search, alerting, or cataloging
```

Recommended language:

- preserve local audit/outbox records before provider emission;
- emit minimized envelopes to external systems;
- treat provider failures as retry, defer, quarantine, or escalate conditions;
- keep provider packages optional and downstream of Core;
- describe signed artifacts as signed only after a configured signing provider returns signature metadata;
- describe verified artifacts as verified only after verification policy has run.

## Package dependency language

Core remains provider-neutral. Provider packages depend on Core. Core must not depend on OpenTelemetry, Azure Monitor, Event Hubs, Purview, SIEM, signing providers, cloud SDKs, robotics SDKs, HSM SDKs, KMS SDKs, certificate stores, or immutable storage systems.

Appropriate wording:

- `CDCavell.AsiBackbone.Core` defines provider-neutral governance primitives and seams.
- `CDCavell.AsiBackbone.Signing.LocalDevelopment` is a released local-development provider package for tests, samples, and wiring proof paths.
- `CDCavell.AsiBackbone.Signing.ManagedKey` is a released adapter boundary for host-owned managed-key clients.
- Provider packages or host adapters translate Core records into provider-specific systems.
- Provider packages are optional integration surfaces.
- Host applications choose whether to install and configure a provider package.

Avoid wording that implies:

- Core requires a cloud provider;
- Core requires a certificate, managed key, HSM, or KMS;
- the local-development signer is production-safe;
- the managed-key adapter includes a live Azure Key Vault, Managed HSM, cloud KMS, certificate-store, or HSM implementation by default;
- OpenTelemetry, Azure Monitor, Event Hubs, Purview, or SIEM replaces local accountability;
- provider-specific packages define Core semantics.

## Alpha, preview, and sample language

Use alpha, preview, design-only, strategy-only, or sample wording for APIs, provider pages, or packages that are not part of the stable package family.

Do not imply that an alpha, preview, strategy-only, design-only, or sample package:

- is production-hardened;
- is covered by the same compatibility promise as stable packages;
- provides legal, compliance, cryptographic, or operational guarantees;
- is required to use Core;
- makes AsiBackbone a completed ASI implementation.

Stable documentation may discuss alpha history, but should keep historical alpha limitations separate from current stable behavior and future provider planning.

## Release and PR checklist

Before merging documentation for provider or security work, confirm:

- current behavior is separated from planned or future behavior;
- stable Core signing-ready and verification primitives are not described as future-only;
- the local-development signing provider remains clearly non-production;
- the managed-key signing provider remains clearly an adapter boundary with host-owned client implementation;
- tamper-evidence claims are tied to implemented and deployed features;
- provider dependencies remain outside Core;
- durable local/outbox persistence remains the reliability baseline before provider emission;
- host responsibilities for security, privacy, retention, key management, verification, monitoring, and compliance are explicit;
- no wording claims AsiBackbone implements artificial superintelligence, controls robots, or replaces governance, legal, security, or operational review.

## Related documentation

- [Privacy and Signing Boundaries](privacy-and-signing-boundaries.md)
- [Signing-Ready Receipts and Key Handling](signing-ready-receipts-and-key-handling.md)
- [Signing Provider Package Boundary](signing-provider-package-boundary.md)
- [Managed-Key Signing Provider](managed-key-signing-provider.md)
- [Signed Audit and Outbox Records](signed-audit-and-outbox-records.md)
- [Cryptographic Security Posture and Production Guidance](cryptographic-security-posture.md)
- [Verification Policy and Result Handling](verification-policy-and-result-handling.md)
- [Key Rotation and Retired-Key Verification](key-rotation-and-retired-key-verification.md)
- [Observability and Governance Emission Architecture](observability-and-governance-emission-architecture.md)
- [Governance Emission Contract](governance-emission-contract.md)
- [Durable Audit and Outbox Persistence](durable-audit-outbox-persistence.md)
- [API Compatibility and SemVer](api-compatibility-and-semver.md)
