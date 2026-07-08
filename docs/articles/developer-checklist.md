# Developer Checklist

Use this checklist before opening a pull request against the stable AsiBackbone package family.

## Local validation

```bash
dotnet restore AsiBackbone.slnx
dotnet build AsiBackbone.slnx --configuration Release
dotnet test AsiBackbone.slnx --configuration Release --no-build --no-restore
```

## Solution build configurations

`AsiBackbone.slnx` intentionally treats `Release` as the full validation configuration. Use the `Release` commands above when you need evidence that the repository source and test surface has been built and tested.

Do not treat a `Debug` solution build as a complete repository validation pass. Several projects include solution-level entries such as:

```xml
<Build Solution="Debug|*" Project="false" />
```

Those entries remove the project as an explicit top-level target for `Debug|*` solution builds. They do not necessarily prevent MSBuild from building the project transitively when another included project references it. The practical rule is:

- `Debug` solution builds are local inner-loop or IDE convenience builds only.
- `Release` solution builds are the canonical local validation path.
- CI and stable release validation also use `Release` for solution restore, build, test, formatting, package, documentation, and smoke-test checks.

The current `Debug|*` exclusions were reviewed for documentation clarity. No exclusion is treated as accidental in this checklist; if a project becomes part of the normal Debug inner loop later, remove the corresponding solution entry and update this table in the same change.

| Excluded project | Why it is excluded from `Debug|*` solution builds | Full validation path |
| --- | --- | --- |
| `src/AsiBackbone.AspNetCore/AsiBackbone.AspNetCore.csproj` | Host adapter package; not required for the smallest solution-level Debug inner loop. | `dotnet build AsiBackbone.slnx --configuration Release` and `dotnet test AsiBackbone.slnx --configuration Release`. |
| `src/AsiBackbone.Core/AsiBackbone.Core.csproj` | Core package is still validated by the canonical Release path; Debug exclusion avoids implying Debug is the repository-wide validation configuration. | Release solution build/test plus the CI Core branch coverage gate. |
| `src/AsiBackbone.EntityFrameworkCore/AsiBackbone.EntityFrameworkCore.csproj` | EF Core persistence integration is host-owned infrastructure and should be validated through the full Release path. | Release solution build/test and EF Core integration tests. |
| `src/AsiBackbone.OpenTelemetry/AsiBackbone.OpenTelemetry.csproj` | Optional governance-emission provider; not part of the minimal Debug inner loop. | Release solution build/test and OpenTelemetry tests. |
| `src/AsiBackbone.Signing.LocalDevelopment/AsiBackbone.Signing.LocalDevelopment.csproj` | Local-development signing provider; not production key custody and not required for the minimal Debug inner loop. | Release solution build/test and local-development signing tests. |
| `src/AsiBackbone.Storage.InMemory/AsiBackbone.Storage.InMemory.csproj` | Non-durable storage helper for tests, samples, and local validation; not required for the smallest Debug solution target set. | Release solution build/test and storage-related tests. |
| `src/AsiBackbone.Templates/AsiBackbone.Templates.csproj` | Template package validation is handled through package and template smoke checks instead of Debug solution builds. | Release build/pack plus `eng/smoke-tests/template-package-smoke.sh`. |
| `src/AsiBackbone.Testing/AsiBackbone.Testing.csproj` | Test-harness package; validated with the full Release test surface rather than the partial Debug solution target set. | Release solution build/test and testing-harness tests. |
| `benchmarks/AsiBackbone.Benchmarks/AsiBackbone.Benchmarks.csproj` | Benchmark harness; benchmarks are not normal Debug solution validation. | Build/run the benchmark project intentionally, normally in Release. |
| `benchmarks/AsiBackbone.Benchmarks.BenchmarkDotNet/AsiBackbone.Benchmarks.BenchmarkDotNet.csproj` | BenchmarkDotNet project; benchmark execution should be an explicit performance activity, not a default Debug solution build. | Build/run the BenchmarkDotNet project intentionally, normally in Release. |
| `samples/PlainAspNetCoreHost/AsiBackbone.Samples.PlainAspNetCoreHost.csproj` | Sample host; samples demonstrate adoption paths and should not be confused with package validation from a Debug solution build. | Release solution build and sample/smoke validation when sample behavior changes. |
| `tests/AsiBackbone.AspNetCore.Tests/AsiBackbone.AspNetCore.Tests.csproj` | ASP.NET Core integration tests belong to the full Release validation surface. | `dotnet test AsiBackbone.slnx --configuration Release`. |
| `tests/AsiBackbone.Core.Tests/AsiBackbone.Core.Tests.csproj` | Core tests and coverage are canonical CI validation, not partial Debug solution evidence. | Release solution test plus the CI Core branch coverage gate. |
| `tests/AsiBackbone.EntityFrameworkCore.Tests/AsiBackbone.EntityFrameworkCore.Tests.csproj` | EF Core integration tests belong to the full Release validation surface. | `dotnet test AsiBackbone.slnx --configuration Release`. |
| `tests/AsiBackbone.OpenTelemetry.Tests/AsiBackbone.OpenTelemetry.Tests.csproj` | Provider tests belong to the full Release validation surface. | `dotnet test AsiBackbone.slnx --configuration Release`. |
| `tests/AsiBackbone.Signing.LocalDevelopment.Tests/AsiBackbone.Signing.LocalDevelopment.Tests.csproj` | Local-development signing tests belong to the full Release validation surface. | `dotnet test AsiBackbone.slnx --configuration Release`. |
| `tests/AsiBackbone.Testing.Tests/AsiBackbone.Testing.Tests.csproj` | Testing-harness tests belong to the full Release validation surface. | `dotnet test AsiBackbone.slnx --configuration Release`. |

When package-consumer behavior changes, run the smoke checks when possible:

```bash
bash ./eng/smoke-tests/external-consumer-smoke.sh
bash ./eng/smoke-tests/stable-package-integration-smoke.sh
```

For documentation changes, preview the DocFX site locally:

```bash
dotnet tool restore
dotnet tool run docfx docs/docfx.json --serve
```

## Package boundary checklist

- Core remains framework-neutral.
- ASP.NET Core integration remains a thin host adapter.
- EF Core integration remains host-owned.
- In-memory storage remains non-durable and local-validation focused.
- Samples and smoke tests do not become hidden requirements for consumers.
- Provider-specific behavior is not implied to be part of `1.0.0` unless explicitly released as stable.

## Production hygiene checklist

- Production source under `src/` must not use `NotImplementedException` as a placeholder.
- Use explicit production behavior instead: fail-closed governance result, domain-specific exception, `NotSupportedException`, or `InvalidOperationException` as appropriate.
- Keep tests, samples, and template scaffolding clearly separated from production library execution paths.
- Update [Production Placeholder Exception Guardrails](production-placeholder-exception-guardrails.md) when an intentional allowance is added.

## Public API checklist

- Is this API part of a stable package?
- Is the change additive, or does it remove or rename public behavior?
- Does it affect XML documentation, README content, or DocFX articles?
- Does it affect package-consumer smoke tests?
- Does it require a release-note entry?
- Does it require an API compatibility note?
- Does it affect extension points used by host applications?

## Documentation checklist

Documentation should be updated when a change affects package installation, public API usage, host responsibilities, package boundaries, schema shape, release notes, sample behavior, or smoke-test expectations.

## Pull request checklist

- Create a focused branch.
- Keep the PR scoped to the issue.
- Include a concise summary.
- List tests run or state that tests were not run.
- Link or close the related issue.
- Include documentation updates when public behavior changes.
- Avoid unrelated formatting churn.

## Release-readiness checklist

- Release notes are current.
- Package versions are aligned.
- Public docs and README links resolve.
- Known limitations are documented.
- Stable API and schema boundaries are reviewed.
- Package-consumer smoke tests pass or failures are explained.
- Future provider work is not presented as already stable.
