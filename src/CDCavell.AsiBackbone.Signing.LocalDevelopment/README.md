# CDCavell.AsiBackbone.Signing.LocalDevelopment

`CDCavell.AsiBackbone.Signing.LocalDevelopment` provides a local-development RSA signing and verification provider for exercising AsiBackbone signing abstractions without Azure Key Vault, HSM, cloud KMS, certificate-store, or external infrastructure dependencies.

> [!IMPORTANT]
> This package is intended for local development, samples, and tests. It is not a production managed-key provider and does not provide tamper-evidence, immutability, legal non-repudiation, compliance certification, or protected key custody by itself.

## Boundary

This package implements:

- `IAsiBackboneSigningService`
- `IAsiBackboneSignatureVerificationService`

It signs the `SigningRequest.SigningHash` value using an in-process RSA key generated for the service instance and returns provider-neutral `SigningMetadata`.

Core remains provider-neutral. `CDCavell.AsiBackbone.Core` does not reference this package.

## Metadata returned

Successful signing results include:

- signing hash
- hash algorithm
- Base64 signature value
- signature algorithm descriptor
- key ID
- key version
- provider descriptor
- signed UTC timestamp
- local-development warning metadata

Signing failures in normal flow return unsigned signing metadata with explicit `signing_status`, `failure_code`, and `failure_message` values unless the host opts out by setting `ReturnUnsignedOnFailure = false`.

## Example registration

```csharp
var localSigningOptions = LocalDevelopmentSigningOptions.Create(
    keyId: "sample-local-dev-key",
    keyVersion: "dev");

var localSigningService = new LocalDevelopmentSigningService(localSigningOptions);

builder.Services.AddSingleton(localSigningService);
builder.Services.AddSingleton<IAsiBackboneSigningService>(localSigningService);
builder.Services.AddSingleton<IAsiBackboneSignatureVerificationService>(localSigningService);
```

## Example flow

```text
AuditLedgerRecord
  -> CanonicalPayloadBuilder.ForAuditLedgerRecord(...)
  -> CanonicalPayloadHasher.ComputeHash(...)
  -> SigningRequest
  -> LocalDevelopmentSigningService.SignAsync(...)
  -> SignatureVerificationRequest
  -> LocalDevelopmentSigningService.VerifyAsync(...)
```

## Non-goals

This package does not:

- integrate with Azure Key Vault, Managed HSM, local machine certificate stores, or cloud KMS services;
- persist private key material;
- provide production key rotation;
- provide legal non-repudiation;
- verify an audit hash chain;
- provide immutable storage or external anchoring;
- make unsigned, signed, or verified records tamper-evident by default.

Use a managed-key or HSM-backed provider for production workflows where signing is part of a security or audit-control boundary.
