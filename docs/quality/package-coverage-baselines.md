# Adapter and Provider Package Coverage Baselines

The repository-wide coverage gate protects the full solution, but consumer-facing adapter and provider packages also need to remain visible on their own. A high total line-coverage percentage can hide a weak integration surface if Core tests dominate the aggregate result.

This page records the staged package-specific coverage posture introduced for issue #511.

## Current gate posture

| Gate | Scope | Coverage type | Threshold | Purpose |
| --- | --- | --- | --- | --- |
| Repository-wide coverage gate | Full solution | Line coverage | 75% minimum | Keeps broad test coverage from regressing across the package family. |
| Core branch coverage gate | `AsiBackbone.Core` through `AsiBackbone.Core.Tests` | Branch coverage | 90% minimum | Protects the framework-neutral governance engine. |
| Adapter/provider package baseline gate | Selected packages in `eng/coverage/package-coverage-baselines.csv` | Line coverage | Package-specific floor | Makes each selected adapter/provider package visible independently from the total solution coverage number. |

The package baseline gate intentionally starts with low visibility floors. These floors are not maturity targets; they are the first regression guard that ensures each selected package produces an independent coverage artifact and cannot silently disappear behind the repository-wide total.

## Tracked package surfaces

The current baseline file tracks these package surfaces:

| Package | Test project | Include filter | Initial line floor |
| --- | --- | --- | ---: |
| `AsiBackbone.AspNetCore` | `tests/AsiBackbone.AspNetCore.Tests/AsiBackbone.AspNetCore.Tests.csproj` | `[AsiBackbone.AspNetCore]*` | 1% |
| `AsiBackbone.EntityFrameworkCore` | `tests/AsiBackbone.EntityFrameworkCore.Tests/AsiBackbone.EntityFrameworkCore.Tests.csproj` | `[AsiBackbone.EntityFrameworkCore]*` | 1% |
| `AsiBackbone.OpenTelemetry` | `tests/AsiBackbone.OpenTelemetry.Tests/AsiBackbone.OpenTelemetry.Tests.csproj` | `[AsiBackbone.OpenTelemetry]*` | 1% |
| `AsiBackbone.Signing.ManagedKey` | `tests/AsiBackbone.Signing.ManagedKey.Tests/AsiBackbone.Signing.ManagedKey.Tests.csproj` | `[AsiBackbone.Signing.ManagedKey]*` | 1% |
| `AsiBackbone.Analyzers` | `tests/AsiBackbone.Analyzers.Tests/AsiBackbone.Analyzers.Tests.csproj` | `[AsiBackbone.Analyzers]*` | 1% |

## CI behavior

CI runs:

```powershell
./scripts/Validate-PackageCoverageBaselines.ps1 -Configuration Release -NoBuild -NoRestore
```

The script reads `eng/coverage/package-coverage-baselines.csv`, runs package-scoped Coverlet targets, writes a Markdown summary, and uploads package coverage artifacts under `asi-backbone-package-coverage-baselines`.

The package-specific artifacts are intentionally separate from:

- `asi-backbone-coverage`, the full-solution 75% line-coverage artifact;
- `asi-backbone-core-branch-coverage`, the Core-only 90% branch-coverage artifact.

## Exclusions

The package baseline gate excludes test assemblies through Coverlet and does not currently target:

- generated code;
- samples and templates unless they become explicit package-quality targets;
- benchmarks;
- local-development-only helpers that do not represent stable consumer-facing behavior.

`AsiBackbone.Signing.LocalDevelopment`, `AsiBackbone.Storage.InMemory`, `AsiBackbone.DependencyInjection`, `AsiBackbone.Testing`, and template-host scenarios can be added later when their package-specific coverage expectations are calibrated.

## Next hardening threshold

The next threshold is to replace the initial 1% visibility floors with calibrated baselines after the first CI package-specific reports are reviewed.

Recommended sequence:

1. Review the first `asi-backbone-package-coverage-baselines` artifacts.
2. Record observed package-specific line coverage for each tracked package.
3. Raise each floor to a conservative package-specific threshold slightly below the observed stable value.
4. Prioritize higher-risk consumer surfaces first: ASP.NET Core, EF Core, OpenTelemetry, and managed-key signing.
5. Keep analyzer coverage separate because analyzer execution and meaningful behavior coverage may require different test shape than runtime adapters.
6. Document any package-specific exclusion before lowering or skipping a target.

This keeps the hardening path incremental while making coverage debt explicit and regression-sensitive.

## Related files

- `eng/coverage/package-coverage-baselines.csv`
- `scripts/Validate-PackageCoverageBaselines.ps1`
- `.github/workflows/ci.yml`
- `docs/quality/core-branch-coverage.md`
