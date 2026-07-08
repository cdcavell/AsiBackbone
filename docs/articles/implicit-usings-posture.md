# Implicit Usings Posture

This note records the repository policy for C# implicit global usings and future `GlobalUsings.cs` files.

## Decision

Keep repository-wide implicit usings enabled through `Directory.Build.props`.

```xml
<ImplicitUsings>enable</ImplicitUsings>
```

This is the long-term default for the current stable package family because it matches modern .NET SDK conventions and avoids low-value `System.*` boilerplate. Package-boundary clarity should continue to come from explicit project references, package references, namespace declarations, and normal file-level `using` directives for provider-specific dependencies.

Do not add `GlobalUsings.cs` files only to mirror SDK-provided implicit usings. Add an explicit global-usings file only when it improves reviewability for a specific project boundary, such as a test project with repeated test framework namespaces or a sample host with intentionally shared host-scaffolding namespaces. Keep those files small, local to the project, and reviewed as package-boundary documentation rather than formatting cleanup.

## Current audit

The root `Directory.Build.props` enables implicit usings for SDK-style projects unless a project overrides the setting. No current project disables implicit usings. The reviewed posture by project type is:

| Project type | Current behavior | Policy |
| --- | --- | --- |
| Core package | Inherits repository-wide implicit usings. | Keep enabled. Core remains framework-neutral through project/package references and explicit non-`System.*` usings, not by disabling SDK conveniences. |
| Dependency injection, storage, testing, and analyzer packages | Inherit repository-wide implicit usings. | Keep enabled. Use file-level usings for package-specific dependencies so package seams remain visible in review. |
| ASP.NET Core package | Inherits repository-wide implicit usings. | Keep enabled. Continue using explicit ASP.NET Core and Microsoft Extensions namespaces when they communicate host-adapter dependencies. |
| EF Core package | Inherits repository-wide implicit usings. | Keep enabled. Keep EF Core dependencies visible through project references, package references, and explicit EF Core usings. |
| Signing packages | Inherit repository-wide implicit usings. | Keep enabled. Do not use global usings to obscure cryptography, key-management, or provider-adapter dependencies. |
| OpenTelemetry package | Inherits repository-wide implicit usings. | Keep enabled. Keep telemetry provider dependencies explicit at the file or project-reference level. |
| Templates package and generated template host | Inherit repository-wide implicit usings in the package project and generated host scaffolding. | Keep enabled. Generated template code should stay idiomatic for modern .NET users; add explicit usings only when they help template readability. |
| Tests | Inherit repository-wide implicit usings. | Keep enabled. A test-local `GlobalUsings.cs` may be added later only if it reduces repeated test framework boilerplate without hiding package dependencies. |
| Samples | Inherit repository-wide implicit usings. | Keep enabled. Samples should remain approachable and idiomatic while keeping host-owned infrastructure dependencies visible. |
| Benchmarks | Inherit repository-wide implicit usings. | Keep enabled. Benchmark-specific dependencies should remain explicit unless a local global-usings file materially improves benchmark readability. |

## Review guidance

When adding or changing a project, use this order of preference:

1. Keep the repository default unless there is a clear boundary or reviewability reason to change it.
2. Prefer normal file-level `using` directives for provider-specific, package-specific, cryptographic, persistence, telemetry, ASP.NET Core, EF Core, or benchmark namespaces.
3. Use a project-local `GlobalUsings.cs` only for repeated, intentional namespaces that are part of that project's local development surface.
4. Avoid disabling `ImplicitUsings` for a single project unless the project has a documented reason and the change is reviewed as a package-boundary decision.
5. Avoid churn-only changes that add, remove, or alphabetize using directives without improving package-boundary clarity.

This policy keeps the codebase idiomatic for modern .NET while preserving AsiBackbone's package-boundary review discipline.
