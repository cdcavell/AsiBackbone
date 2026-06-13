# Developer Checklist

Use this checklist before opening a pull request against the stable AsiBackbone package family.

## Local validation

```bash
dotnet restore AsiBackbone.slnx
dotnet build AsiBackbone.slnx --configuration Release
dotnet test AsiBackbone.slnx --configuration Release --no-build --no-restore
```

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
