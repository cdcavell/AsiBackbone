# CDCavell.AsiBackbone.Signing.ManagedKey

Provider-neutral managed-key signing adapter for AsiBackbone governance artifacts.

This package keeps `CDCavell.AsiBackbone.Core` provider-neutral while allowing host applications to connect signing flow to a managed key system, HSM, cloud KMS, Azure Key Vault / Managed HSM adapter, or organization-owned signing service.

## Important boundary

This package does **not** include a live Azure Key Vault, HSM, cloud KMS, certificate store, or blockchain implementation by default. It defines the managed-key signing adapter and a host-owned client boundary:

```csharp
public interface IManagedKeySigningClient
{
    ValueTask<ManagedKeySignResult> SignAsync(
        ManagedKeySignRequest request,
        CancellationToken cancellationToken = default);
}
```

Host applications provide the client implementation and credentials. Private keys, symmetric keys, access tokens, client secrets, connection strings, managed identity tokens, and raw key material must not be returned to AsiBackbone.

## Dependency injection

```csharp
services.AddAsiBackboneManagedKeySigning(
    options =>
    {
        options.ProviderName = "azure-key-vault";
        options.KeyId = "https://vault-name.vault.azure.net/keys/audit-signing-key";
        options.KeyVersion = "00000000000000000000000000000000";
        options.SignatureAlgorithm = "RSASSA-PKCS1-v1_5-SHA256-MANAGED-KEY";
        options.RequireKeyVersion = true;
        options.ReturnUnsignedOnFailure = true;
    },
    serviceProvider => new HostOwnedManagedKeySigningClient());
```

The registration wires `ManagedKeySigningService` as `IAsiBackboneSigningService`. Verification remains a separate provider or host responsibility.

## Failure behavior

When `ReturnUnsignedOnFailure` is `true`, signing failures return unsigned `SigningMetadata` with safe failure metadata:

- `signing_status = failed`
- `failure_code`
- `failure_message`
- `provider_kind = managed-key`
- `raw_private_key_loaded = false`
- `retry_attempts`

When `ReturnUnsignedOnFailure` is `false`, provider exceptions are rethrown so high-assurance hosts can fail closed.

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
