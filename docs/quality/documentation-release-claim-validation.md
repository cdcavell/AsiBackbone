# Documentation Release-Claim Validation

AsiBackbone validates current documentation against the release identity declared in `Directory.Build.props`.

The check exists to prevent evergreen pages from continuing to describe an older package line as current, stable, active, or canonical after the repository advances to a new release line. It does not reject ordinary historical version references, migration guidance, compatibility comparisons, or version-specific release records.

## Validation command

Run the repository validation from the repository root:

```powershell
./scripts/Validate-DocumentationReleaseClaims.ps1
```

Run the deterministic fixture suite with:

```powershell
./scripts/Test-DocumentationReleaseClaims.ps1
```

Both commands run in the **Version Consistency** workflow.

## Source of truth

The validator parses `<VersionPrefix>` from `Directory.Build.props` as XML. From a value such as `3.0.0`, it derives:

- exact current version: `3.0.0`;
- current minor line: `3.0.x`;
- current major line: `3.x`.

The script does not duplicate the release version in its own source or configuration.

## Scanned documentation

The default scan includes:

- `README.md`, `CONTRIBUTING.md`, `GOVERNANCE.md`, and `SECURITY.md`;
- `docs/index.md`;
- Markdown articles under `docs/articles/`;
- package README files under `src/`.

Fenced code blocks are ignored because version examples inside commands or sample text are not necessarily release-posture claims.

When the validator finds a stale claim, it reports the file, line number, matched version, expected version, and source line. In GitHub Actions the failure is also emitted as a file-and-line annotation.

## Historical paths and reviewed exceptions

Configuration lives in:

```text
eng/documentation-release-claims.json
```

Use `excludedPaths` only for path classes whose purpose is explicitly historical, such as version-specific release notes, release-readiness records, archived quickstarts, or migration guides.

Use `allowedClaims` for a narrow exception inside an otherwise current document. Every entry must include:

- a path or narrowly scoped path pattern;
- a line-level regular expression;
- a written reason explaining why the exception is intentional or where its correction is tracked.

Prefer fixing stale wording over adding an exception. Prefer version-neutral phrases such as **current release line** in evergreen guidance when an exact version is unnecessary.

Do not add broad exclusions for all documentation or all references to an older major version. Older versions remain valid in historical comparisons; the validator is concerned with language that presents them as the active release posture.

## Fixture coverage

The deterministic fixtures verify that:

- a current release claim passes;
- a stale current release claim fails;
- an ordinary historical version mention passes;
- an explicitly excluded historical file passes;
- stale failure output identifies the file, line, stale value, and expected value.

The fixture runner starts a child PowerShell process for each scenario so the validator's success and failure exit codes are tested exactly as CI observes them.

## Updating the rule

When a new documentation category is added:

1. decide whether the page is evergreen or explicitly historical;
2. keep evergreen pages in the scan whenever possible;
3. add a narrow path exclusion only when historical provenance is the page's purpose;
4. add a line exception only when the wording is intentionally retained and the reason is documented;
5. add or update a deterministic fixture when detection behavior changes;
6. run the fixture suite and the repository validator before opening the pull request.

This validation is a documentation-currency guard. It does not change package versions, release semantics, runtime behavior, public API, or compatibility policy.
