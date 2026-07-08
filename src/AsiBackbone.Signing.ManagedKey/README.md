# AsiBackbone.Signing.ManagedKey

Provider-neutral managed-key signing adapter for AsiBackbone governance artifacts.

This package keeps `AsiBackbone.Core` provider-neutral while allowing host applications to connect signing flow to a managed key system, HSM, cloud KMS, Azure Key Vault / Managed HSM adapter, or organization-owned signing service.

> **New to AsiBackbone?** Start with the concept, not this package: [Intent to Execution: An Accountability Pattern](https://cdcavell.github.io/AsiBackbone/articles/intent-to-execution-pattern.html) and the [documentation site](https://cdcavell.github.io/AsiBackbone/). This README covers one package in the family.

> **Important:**
> This package does not include a live Azure Key Vault, HSM, cloud KMS, certificate store, or blockchain implementation by default. Host applications provide the client and credentials; private keys, tokens, secrets, and raw key material must not be returned to AsiBackbone.

## Boundary

```csharp
public interface IManagedKeySigningClient
{
    ValueTask<ManagedKeySignResult> SignAsync(
        ManagedKeySignRequest request,
        CancellationToken cancellationToken = default);
}
```

Host applications provide the client implementation and credentials. Private keys, symmetric keys, access tokens, client secrets, connection strings, managed identity tokens, and raw key material must not be returned to AsiBackbone.

The default managed-key signature descriptor is `RSASSA-PSS-SHA256-MANAGED-KEY`. It is provider-neutral metadata that the host-owned managed-key client maps to the concrete algorithm name used by its KMS, HSM, or signing service. Hosts whose key service still requires PKCS#1 v1.5 should override `SignatureAlgorithm` explicitly and document that compatibility choice.

## Dependency injection

The production-oriented registration fails closed by default. Signing failures throw unless the host explicitly opts into unsigned failure metadata.

```csharp
services.AddAsiBackboneManagedKeySigning(
    options =>
    {
        options.ProviderName = "azure-key-vault";
        options.KeyId = "https://vault-name.vault.azure.net/keys/audit-signing-key";
        options.KeyVersion = "00000000000000000000000000000000";
        options.SignatureAlgorithm = "RSASSA-PSS-SHA256-MANAGED-KEY";
        options.RequireKeyVersion = true;
        options.ReturnUnsignedOnFailure = false;
    },
    serviceProvider => new HostOwnedManagedKeySigningClient());
```

For samples, tests, diagnostics, or explicit policy-routed fallback behavior, use the local-validation registration:

```csharp
services.AddAsiBackboneManagedKeySigningForLocalValidation(
    options =>
    {
        options.ProviderName = "managed-key-local-validation";
        options.KeyId = "local-validation-key";
        options.KeyVersion = "local-v1";
    },
    serviceProvider => new HostOwnedManagedKeySigningClient());
```

The registration wires `ManagedKeySigningService` as `IAsiBackboneSigningService`. Verification remains a separate provider or host responsibility.

## Failure behavior

When `ReturnUnsignedOnFailure` is `false`, provider, validation, and configuration-related signing failures throw so high-assurance hosts can fail closed at the governance boundary.

When `ReturnUnsignedOnFailure` is `true`, signing failures return unsigned `SigningMetadata` with safe failure metadata:

- `signing_status = failed`
- `failure_code`
- `failure_message`
- `provider_kind = managed-key`
- `raw_private_key_loaded = false`
- `retry_attempts`

Unsigned failure metadata is not a successful signature and must not be described as a signed governance artifact.

## Safe metadata

Signed results preserve provider-neutral metadata:

- signing hash;
- hash algorithm;
- signature value or signature reference;
- signature algorithm;
- key ID;
- key version;
- provider descriptor;
- signed UTC timestamp;
- safe provider operation ID when supplied.

Provider metadata keys that appear to contain secrets, tokens, credentials, private key material, or connection strings are filtered.

## Non-goals

This package does not provide tamper-evidence, immutable storage, append-only database behavior, external anchoring, legal non-repudiation, or compliance certification by itself. Those guarantees require durable storage controls, verification, audit-chain or anchoring strategy, key-retention policy, monitoring, and incident response.