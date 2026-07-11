# Managed-Key Signing Provider

This article documents the released `AsiBackbone.Signing.ManagedKey` provider package.

In this software project, **ASI** means **Accountable Systems Infrastructure**. AsiBackbone provides provider-neutral signing, verification, and audit metadata seams. It does not provide immutable storage, external anchoring, blockchain, legal evidence guarantees, compliance certification, production key management, or tamper-evidence by itself.

> [!IMPORTANT]
> The managed-key package is a stable provider adapter and client boundary. It does not include a live Azure Key Vault, Managed HSM, AWS KMS, GCP Cloud KMS, certificate store, cloud KMS, or HSM implementation by default. Host applications supply the actual managed-key client, credentials, verification path, monitoring, and operational policy.
>
> Production runtime signing remains provider-neutral. AsiBackbone does not ship or maintain first-party production signing providers or production-style signing sample hosts. See the [Production Managed-Key Integration Guide](production-managed-key-integration.md).

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