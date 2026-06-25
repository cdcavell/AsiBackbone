# Managed-Key Signing Provider

Issue: #210, updated for #253.

This article documents the released `AsiBackbone.Signing.ManagedKey` provider package.

In this software project, **ASI** means **Accountable Systems Infrastructure**. AsiBackbone provides provider-neutral signing, verification, and audit metadata seams. It does not provide immutable storage, external anchoring, blockchain, legal evidence guarantees, compliance certification, or tamper-evidence by itself.

> [!IMPORTANT]
> The managed-key package is a stable `1.1.0` provider adapter and client boundary. It does not include a live Azure Key Vault, Managed HSM, cloud KMS, certificate store, or HSM implementation by default. Host applications supply the actual managed-key client, credentials, verification path, monitoring, and operational policy.

## Purpose

`AsiBackbone.Signing.ManagedKey` lets a host application wire AsiBackbone signing to a managed-key system without forcing `AsiBackbone.Core` to reference cloud SDKs or handle private key material.

The dependency direction remains:

```text
AsiBackbone.Core
        ^
        |
AsiBackbone.Signing.ManagedKey
        ^
        |
Host application managed-key client
```

Core remains provider-neutral. Provider packages reference Core, not the reverse.

## Released boundary

| Boundary | Status | Notes |
| --- | --- | --- |
| Managed-key adapter package | Released stable package in `1.1.0`. | Provides options, DI registration, signing service, and client abstraction. |
| `IManagedKeySigningClient` | Host-owned implementation boundary. | The host implementation may call Azure Key Vault, Managed HSM, cloud KMS, an HSM appliance, or an organization-owned signing service. |
| Concrete cloud/HSM/KMS client | Not shipped by default. | Host-owned or future provider-specific package work. |
| Verification service | Separate host/provider responsibility. | Signed metadata is preserved for later verification, but hosts must provide a matching verification path when trust is required. |
| Production tamper-evidence | Not provided by default. | Requires signing, verification, protected key management, durable storage controls, retention, monitoring, and operational procedures. |

## Key boundary

The package defines `IManagedKeySigningClient`:

```csharp
public interface IManagedKeySigningClient
{
    ValueTask<ManagedKeySignResult> SignAsync(
        ManagedKeySignRequest request,
        CancellationToken cancellationToken = default);
}
```

A host-owned implementation can call Azure Key Vault, Managed HSM, a cloud KMS, an HSM appliance, or an organization-owned signing service. The implementation should sign a precomputed AsiBackbone artifact hash and return provider-neutral metadata.

The client must not return:

- private keys;
- symmetric keys;
- client secrets;
- access tokens;
- managed identity tokens;
- connection strings;
- raw credential material.

## Dependency injection

Use `AddAsiBackboneManagedKeySigning(...)` to register the managed-key provider as `IAsiBackboneSigningService`.

```csharp
services.AddAsiBackboneManagedKeySigning(
    options =>
    {
        options.ProviderName = "azure-key-vault";
        options.KeyId = "https://vault-name.vault.azure.net/keys/audit-signing-key";
        options.KeyVersion = "00000000000000000000000000000000";
        options.SignatureAlgorithm = "RSASSA-PKCS1-v1_5-SHA256-MANAGED-KEY";
        options.HashAlgorithm = "SHA-256";
        options.RequireKeyVersion = true;
        options.ReturnUnsignedOnFailure = true;
        options.MaxRetryAttempts = 2;
    },
    serviceProvider => new HostOwnedManagedKeySigningClient());
```

The package also supports using an already-registered `IManagedKeySigningClient`:

```csharp
services.AddSingleton<IManagedKeySigningClient, HostOwnedManagedKeySigningClient>();

services.AddAsiBackboneManagedKeySigning(options =>
{
    options.ProviderName = "managed-hsm";
    options.KeyId = "audit-signing-key";
    options.KeyVersion = "2026-06";
});
```

## Signing metadata

Successful signing returns `SigningMetadata` with:

| Field | Source |
| --- | --- |
| Signing hash | Original `SigningRequest.SigningHash`. |
| Hash algorithm | Request or configured hash algorithm. |
| Signature | Managed-key client result. |
| Signature algorithm | Managed-key client result. |
| Key ID | Managed-key client result. |
| Key version | Managed-key client result or resolved request/options value. |
| Provider | Configured provider name. |
| Signed UTC | Managed-key client result. |
| Provider operation ID | Safe managed-key client result metadata. |

The provider adds safe metadata such as `provider_kind = managed-key`, `remote_key_material = true`, `raw_private_key_loaded = false`, `signing_status`, and `retry_attempts`.

## Failure handling

When `ReturnUnsignedOnFailure` is `true`, the provider returns unsigned signing metadata with failure details:

```text
signing_status = failed
failure_code = managedkey.signing.provider-unavailable
failure_message = TimeoutException
```

This lets governance pipelines route failures through host policy: deny, defer, require acknowledgment, escalate, retry, or dead-letter.

When `ReturnUnsignedOnFailure` is `false`, the provider rethrows the signing exception so a high-assurance host can fail closed immediately.

Supported failure codes include:

| Failure | Code |
| --- | --- |
| Unsupported hash algorithm | `managedkey.signing.hash-algorithm-unsupported` |
| Requested key ID mismatch | `managedkey.signing.key-mismatch` |
| Missing key version when required | `managedkey.signing.key-version-missing` |
| Requested key version mismatch | `managedkey.signing.key-version-mismatch` |
| Provider unavailable | `managedkey.signing.provider-unavailable` |
| Generic managed-key signing failure | Provider-supplied or `managedkey.signing.failed` |

## Retry behavior

`ManagedKeySigningService` retries only when the host-owned client throws `ManagedKeySigningException` with `IsRetryable = true` and retry attempts remain.

Timeouts and non-retryable provider errors are surfaced as failure metadata or thrown, depending on `ReturnUnsignedOnFailure`.

## Verification boundary

This package implements signing through a managed-key adapter boundary. Verification remains a separate host or provider responsibility.

Downstream verifiers can use the preserved metadata:

- provider name;
- key ID;
- key version;
- signature algorithm;
- signing hash;
- hash algorithm;
- signed timestamp.

Hosts should provide a matching verification service when signed records must be trusted later. A signed artifact should not be treated as verified merely because the managed-key adapter returned signature metadata.

## Operational prerequisites

Before production use, hosts should document:

- how the managed-key client authenticates to the key system;
- which identity has sign permission;
- which identity has verify/read-public-key permission;
- whether signing and verification permissions are separated;
- which key ID and key version are active;
- how disabled, retired, revoked, or missing key versions are surfaced;
- timeout and retry policy;
- monitoring and alerting for signing failure rates;
- fallback behavior when signing is unavailable.

## Safe wording

Safe wording:

- "The artifact hash was signed through the configured managed-key client."
- "The provider returned key ID, key version, signature algorithm, signature value, and signed timestamp metadata."
- "Private key material remains outside AsiBackbone Core."
- "Signing failures are observable and policy-routable."
- "The managed-key package is a released adapter boundary; the concrete key client is host-owned."

Avoid wording such as:

- "This package provides Azure Key Vault support by default."
- "Managed-key signing makes records tamper-proof."
- "Signed means verified."
- "The audit trail is legally non-repudiable."

Use **tamper-evident** only when the deployed system includes signing, verification, durable append-only storage controls, audit-chain or anchoring strategy, key-retention policy, monitoring, and incident response.

## Related documentation

- [Production Wording and Stable Signing Boundaries](production-wording-and-alpha-limitations.md)
- [Signing Provider Package Boundary](signing-provider-package-boundary.md)
- [Signing-Ready Receipts and Key Handling](signing-ready-receipts-and-key-handling.md)
- [Signed Audit and Outbox Records](signed-audit-and-outbox-records.md)
- [Verification Policy and Result Handling](verification-policy-and-result-handling.md)
- [Cryptographic Security Posture and Production Guidance](cryptographic-security-posture.md)
