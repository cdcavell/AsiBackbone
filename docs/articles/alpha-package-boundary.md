# Alpha Package Boundary

This article defines the intended `0.1.0-alpha.1` boundary for `CDCavell.ASIBackbone.Core`.

`0.1.0-alpha.1` should establish the first stable language for the package family. It should not try to deliver persistence, web integration, signing infrastructure, robotics integration, or AI model functionality.

> [!IMPORTANT]
> The alpha package is a governance and policy-control foundation. It is not an ASI implementation and does not host, train, run, or orchestrate AI models.

## Release intent

The first alpha should answer one question:

> What are the shared framework-neutral primitives that future ASIBackbone packages will build on?

It should provide enough vocabulary for later implementation issues to reference the same concepts consistently.

## Required boundary statements

The alpha documentation and package metadata should preserve these boundaries:

* Core has no ASP.NET Core dependency.
* Core has no Entity Framework Core dependency.
* Core has no database-provider dependency.
* Core has no NetCoreApplicationTemplate dependency.
* Core has no AI model dependency.
* Core has no robotics or physical execution dependency.
* Core remains framework-neutral and host-neutral.

## Initial package responsibility

`CDCavell.ASIBackbone.Core` is responsible for defining the first set of shared abstractions and primitives around:

* governance spine
* intent/request evaluation
* actor context
* policy context
* constraints
* decision result outcomes
* operation result outcomes
* reason codes
* acknowledgment or responsibility-handshake workflows
* audit residue and receipts
* capability-token concepts
* policy version and hash tracking
* correlation IDs

## Suggested first vertical slice

The first useful vertical slice should be testable without a web host or database.

```text
Create intent/request
  -> Build policy context
  -> Evaluate constraints
  -> Produce decision result
  -> Produce audit receipt shape
```

The initial unit tests should cover:

* allow
* deny
* defer
* require acknowledgment
* escalate
* missing or invalid context
* reason code assignment
* correlation ID propagation
* policy version/hash propagation

## Non-goals for `0.1.0-alpha.1`

The first alpha should not include:

* EF Core entity mappings
* migrations
* DbContext ownership
* ASP.NET Core middleware
* ASP.NET Core endpoint mapping
* authentication provider integration
* claims translation implementation
* durable ledger implementation
* cryptographic signing implementation
* robot command schemas
* external system gateway SDKs
* AI inference or training APIs

Those may be future package responsibilities after the Core language stabilizes.

## Future implementation sequence

A practical implementation sequence is:

1. Core abstractions and value objects
2. Policy pipeline contracts
3. Decision result model
4. Acknowledgment workflow contracts
5. Audit receipt contracts
6. Capability-token contracts
7. In-memory validation package or samples
8. ASP.NET Core integration
9. EF Core storage integration
10. Signing support
11. Gateway examples
12. Robotics or high-risk external execution examples

## Alpha acceptance checklist

Before `0.1.0-alpha.1`, the repository should be able to show:

* documented Core domain language
* documented Core package boundary
* README language that avoids overclaiming
* no Core dependency on ASP.NET Core, EF Core, NetCoreApplicationTemplate, or AI model packages
* XML documentation for public Core primitives as they are introduced
* tests proving basic decision outcome behavior once implementation begins

## Stable-release caution

Names introduced in alpha may change before a stable `1.0.0` release. However, the conceptual boundary should remain stable: Core defines host-neutral governance primitives; integration packages own persistence, web, signing, storage, samples, and external execution concerns.
