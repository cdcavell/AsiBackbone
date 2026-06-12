# Quality Reports

AsiBackbone uses quality reports to make the validation surface easier to inspect from the documentation site.

Coverage and mutation analysis answer different questions:

| Report | Question answered | Purpose |
| --- | --- | --- |
| Coverage Report | Did the tests execute this code? | Shows the tested surface area and highlights unvisited code paths. |
| Mutation Analysis | Would tests fail if behavior changed? | Checks assertion strength by introducing small code mutations and verifying tests catch them. |

For a governance package, both views matter. Coverage helps show that policy, acknowledgment, audit, and capability-token paths are exercised. Mutation analysis helps show that the tests are strong enough to detect behavior changes in important decision logic.

## Available reports

### Coverage Report

- [Open Coverage Report](../coverage/index.html)

The coverage report is generated from the test suite using Coverlet and ReportGenerator. It is published with the documentation site when the documentation workflow runs successfully.

### Mutation Analysis

- [Open Core Mutation Analysis](../mutation/core/index.html)
- [Open ASP.NET Core Mutation Analysis](../mutation/aspnetcore/index.html)

The mutation analysis reports are generated with Stryker.NET for targeted governance behavior. The Core report focuses on evaluator and policy-pipeline behavior, including denial precedence, decision outcome selection, reason-code preservation, and related edge cases. The ASP.NET Core report focuses on acknowledgment challenge round-trip behavior, including safe-default challenge shaping, correlation preservation, and conversion of host acknowledgment responses back into Core acknowledgment language.

> [!NOTE]
> If a report link is unavailable, the related workflow may not have generated that report yet. Generate the reports locally or rerun the **Publish Quality Reports** workflow after the report-producing steps are configured.

## Local validation

Restore tools before running report commands:

```bash
dotnet tool restore
```

Generate the standard coverage report through the normal test and ReportGenerator flow:

```bash
dotnet test ./AsiBackbone.slnx --configuration Release --collect:"XPlat Code Coverage" --results-directory ./artifacts/test-results

dotnet reportgenerator \
  -reports:"./artifacts/test-results/**/coverage.cobertura.xml" \
  -targetdir:"./artifacts/coverage-report" \
  -reporttypes:"Html;MarkdownSummaryGithub;Cobertura" \
  -assemblyfilters:"-*.Tests" \
  -filefilters:"-**/bin/**;-**/obj/**;-**/*.g.cs"
```

Run the initial Core mutation analysis from the Core test-project folder:

```bash
cd ./tests/CDCavell.AsiBackbone.Core.Tests
dotnet tool run dotnet-stryker -- --config-file stryker-config.json
```

The Core mutation report is written to:

```text
artifacts/mutation-report/core
```

## Interpreting mutation results

Mutation testing should be reviewed as a quality signal, not as a raw score chase.

Surviving mutants should be classified as:

- missing assertion or weak test behavior;
- equivalent or non-actionable mutant;
- acceptable gap to document for a later issue;
- intentional behavior that should be protected by a stronger test.

For Issue #121, the highest-value review area is Core evaluator behavior: deny-wins composition, decision outcomes, reason-code preservation, policy version/hash behavior where applicable, audit receipt boundaries, and null/default edge cases.
