# Public API XML Documentation

AsiBackbone generates XML documentation for package and DocFX output. Public API documentation is part of the governance contract because consumers rely on stable names, decision semantics, acknowledgment behavior, audit records, signing boundaries, and host-owned integration points.

## Staged CS1591 policy

`CS1591` is no longer suppressed at repository scope. Instead, existing package projects opt into a project-scoped compatibility baseline with `AsiBackboneSuppressMissingXmlDocs=true` while the current public API documentation gaps are inventoried.

This is intentionally staged for the `2.x` line:

1. Inventory every public package project listed in `eng/xml-docs/public-api-projects.txt`.
2. Clean one package surface at a time.
3. Move clean package projects into `eng/xml-docs/staged-enforcement-projects.txt`.
4. Run enforcement mode for only those selected package surfaces.
5. Remove project-scoped suppression from a project only when its public API XML documentation is complete or its remaining exceptions are documented.

This avoids a destabilizing repository-wide cleanup while preventing new package surfaces from inheriting a blanket `CS1591` suppression by default.

## Inventory mode

Run inventory mode to build the selected package projects with `CS1591` unsuppressed and write a Markdown report:

```powershell
./scripts/Validate-XmlDocumentation.ps1 -Mode Inventory
```

The generated report is written to:

```text
artifacts/xml-docs/cs1591-inventory.md
```

CI uploads the same file as the `asi-backbone-cs1591-inventory` artifact.

## Enforcement mode

Run enforcement mode after at least one project has been cleaned and added to `eng/xml-docs/staged-enforcement-projects.txt`:

```powershell
./scripts/Validate-XmlDocumentation.ps1 -Mode Enforce
```

Enforcement mode builds only the staged projects with:

```text
/p:AsiBackboneSuppressMissingXmlDocs=false
/p:AsiBackboneEnforceMissingXmlDocs=true
```

This treats `CS1591` as an error for those projects without forcing immediate enforcement across every package.

## Intentional exceptions

Existing source package projects currently set `AsiBackboneSuppressMissingXmlDocs=true` as a legacy compatibility baseline. That property is an intentional transitional exception, not a permanent default.

Do not add the property to new projects unless the exception is documented with:

- the reason the project cannot yet satisfy `CS1591`;
- the expected cleanup path;
- whether the project appears in the inventory list;
- whether the exception affects NuGet or DocFX consumer documentation.

## Generated documentation output

`GenerateDocumentationFile` remains enabled centrally so NuGet packages and DocFX continue receiving generated XML documentation. This policy changes only how missing public API comments are surfaced and phased into enforcement; it does not disable XML documentation generation.

## Related files

- `Directory.Build.props`
- `eng/xml-docs/public-api-projects.txt`
- `eng/xml-docs/staged-enforcement-projects.txt`
- `scripts/Validate-XmlDocumentation.ps1`
- `.github/workflows/ci.yml`
