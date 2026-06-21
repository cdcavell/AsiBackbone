# Governance

This document describes how the **AsiBackbone** project is governed: who makes
decisions, how those decisions are reached, how contributions are triaged, and
when releases happen. Transparency and consistency are the guiding principles.

**AsiBackbone** is a stable governance‑infrastructure library. Its primary purpose 
is to serve as a reference implementation and conceptual anchor for accountable 
decision‑flow in .NET systems.

The project values clarity, accountability, and minimalism — preferring explicit 
boundaries over implicit behavior.

---

## Table of Contents

1. [Roles and Responsibilities](#roles-and-responsibilities)
2. [Decision-Making Model](#decision-making-model)
3. [Triage Workflow](#triage-workflow)
4. [Issue and PR Ownership](#issue-and-pr-ownership)
5. [Release Cadence](#release-cadence)
6. [AI-Assisted Development Policy](#ai-assisted-development-policy)
7. [Amendments to This Document](#amendments-to-this-document)

---

## Roles and Responsibilities

### Core Maintainers

Core Maintainers have write access to the repository and carry final
responsibility for the project's direction, quality, and community health.

| GitHub Handle | Area of Responsibility |
|---|---|
| *@cdcavell* | Architecture, API design, CI/CD, infrastructure, documentation, community |

> **Becoming a maintainer:** Regular contributors who demonstrate sustained
> quality contributions, sound judgment, and alignment with project values may
> be nominated by any existing maintainer. Confirmation requires a simple
> majority vote among current Core Maintainers with no active objections after a
> 7-day comment period.

> **Bootstrap governance note:** While the project has fewer than two active
> Core Maintainers, the current Core Maintainer may handle routine changes,
> routine releases, security releases, and governance updates after documenting
> the decision in the relevant issue, pull request, or security advisory.

### Triagers

Triagers have permission to label, assign, and close issues and pull requests,
but do not merge code. This role is designed to reward active community members
and reduce maintainer bottleneck.

### Contributors

Anyone who opens an issue, submits a pull request, improves documentation, or
participates in discussion is a Contributor. No formal application is required.
All contributions are subject to the [Code of Conduct](CODE_OF_CONDUCT.md).

See [Contributing to AsiBackbone](CONTRIBUTING.md) for information on contributing.

### Emeritus Maintainers

Former Core Maintainers who have stepped back from active duties. Their
expertise is welcome in discussions but they hold no voting rights.

---

## Decision-Making Model

AsiBackbone uses a **Consensus-Seeking with Fallback Vote** model.

### Everyday decisions

Changes that fall within an existing, documented design (bug fixes, doc
improvements, dependency updates, test additions) may be merged by any Core
Maintainer after:

- At least **one approving review** from a Core Maintainer other than the
  author, and
- CI checks passing (all required status checks green).

### Significant decisions

Changes that introduce new public APIs, alter the project's architecture,
modify governance, or affect the release strategy require:

- A **GitHub Discussion** or issue opened with the `proposal` label,
- A **minimum 5-business-day** comment period,
- **Lazy consensus**: if no Core Maintainer raises a blocking objection within
  the comment period, the proposal is considered accepted.

### Disputed decisions

If consensus cannot be reached:

1. Any Core Maintainer may call a **formal vote** by adding the `vote-open`
   label to the discussion.
2. Each Core Maintainer casts one vote (`+1` / `0` / `-1` with a required
   written rationale for `-1`).
3. A **simple majority** of votes cast decides the outcome.
4. Ties are resolved by the longest-serving active Core Maintainer.
5. Results are documented in the discussion thread and linked from the relevant
   PR or issue.

### Veto & escalation

A `-1` vote is not a veto by itself — it triggers discussion. If a Core
Maintainer believes a decision would cause irreversible harm to the project,
they may invoke a **48-hour hold** (comment `HOLD: <reason>`) during which no
merge may occur. After 48 hours, the normal voting procedure resumes.

---

## Triage Workflow

New issues and pull requests are processed within **3 business days** of
opening. The triage owner (any Triager or Core Maintainer) applies one of the
labels below and takes the corresponding action.

### Issue labels

| Label | Meaning | Action |
|---|---|---|
| `bug` | Confirmed defect | Assign priority (`P0`–`P3`); link to milestone if applicable |
| `enhancement` | Feature request | Move to `proposal` workflow if significant; label `good-first-issue` if small |
| `question` | Usage question | Answer or link to docs; close after resolution |
| `duplicate` | Already tracked | Link to canonical issue; close with comment |
| `wontfix` | Out of scope | Explain decision; close |
| `needs-info` | Waiting on reporter | Auto-close after 14 days of no response |
| `good-first-issue` | Suitable for new contributors | Keep unassigned until claimed |

### Pull request labels

| Label | Meaning | Action |
|---|---|---|
| `needs-review` | Awaiting maintainer review | Assigned to an available Core Maintainer |
| `needs-changes` | Author action required | Author must push or respond within 14 days |
| `blocked` | Waiting on external factor | Document blocker in comment; revisit weekly |
| `ready-to-merge` | Approved; CI green | Any Core Maintainer may merge |
| `do-not-merge` | Hold per governance | Must remain until explicitly removed by a maintainer |

### Stale policy

* **Issues:** Marked `stale` after **60 days** of inactivity; closed after a
  further **14 days** without response.
* **Pull requests:** Marked `stale` after **30 days** of inactivity; closed
  after a further **14 days** without response.
* Stale items may always be reopened with new information.

---

## Issue and PR Ownership

* The **author** of a PR is responsible for keeping it up-to-date with `main`,
  responding to review feedback, and resolving conflicts.
* A PR may be **adopted** by another contributor if the original author does not
  respond within 14 days of a review request; adoption requires a comment
  notifying the original author.
* Core Maintainers may **close any PR** that has been abandoned (no author
  response for 30+ days) or is irreparably conflicting with the project
  direction, with a written explanation.
* **Issue ownership:** Issues are not assigned unless a contributor explicitly
  states they are actively working on it. Assignments lapse after 21 days of
  inactivity; maintainers may reassign or remove the assignment with a comment.

---

## Release Cadence

AsiBackbone follows **Semantic Versioning 2.0.0** (`MAJOR.MINOR.PATCH`).

| Stream | Frequency | Notes |
|---|---|---|
| **Patch** (`x.y.Z`) | As needed | Bug fixes, security patches; no new public API |
| **Minor** (`x.Y.0`) | Approximately every 8 weeks | New features, backward-compatible changes |
| **Major** (`X.0.0`) | As warranted | Breaking changes; requires proposal and vote |

### Release process

1. A Core Maintainer opens a **release PR** that:
   - Bumps the version in all relevant files,
   - Updates `CHANGELOG.md` with entries since the last release,
   - Ensures all CI checks pass.
2. At least **one other Core Maintainer** reviews and approves the release PR.
3. The merging maintainer creates an **annotated Git tag** (`git tag -a vX.Y.Z`)
   and pushes it; CI publishes the release artifact automatically.
4. A **GitHub Release** is created, with the changelog excerpt as the body.
5. If Zenodo integration is enabled, the tagged release is archived for DOI minting according to Zenodo's processing timeline.

> **Note:** After the 1.2.0 release, AsiBackbone is expected to prioritize stability,
> documentation clarity, security patches, and carefully scoped refinements.
> New feature work should be proposal-driven and evaluated against the project's
> governance-infrastructure boundaries.

### Security releases

Security patches (`x.y.Z`) that address a CVE or private disclosure bypass the
standard review window and may be merged by any two Core Maintainers. The
`SECURITY.md` coordinated-disclosure process governs notification timelines.

---

## AI-Assisted Development Policy

**AI‑assisted tools may be used in this project to accelerate development, documentation, 
and refactoring, but all contributions remain human‑reviewed and human‑owned.** Maintainers 
are responsible for validating the correctness, originality, and appropriateness of any 
AI‑generated code or text before it is merged. AI tools do not make architectural decisions, 
define project direction, or act as authors of record; they serve only as optional assistants. 
All final contributions must reflect intentional human judgment, align with the project’s 
governance boundaries, and comply with the repository’s licensing and contribution requirements.

---

## Amendments to This Document

Changes to `GOVERNANCE.md` are **significant decisions** and follow the
5-business-day proposal process described above. The resulting PR must include
an updated changelog entry and must be merged by a Core Maintainer other than
the one who opened it.
