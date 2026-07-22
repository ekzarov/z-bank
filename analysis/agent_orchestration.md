# Cross-Agent Orchestration Protocol

This protocol lets a primary agent delegate review to an external agent CLI
without asking the owner to relay messages. It applies to implementation-slice
peer review and to formal independent passes at Stages 2, 6, and 10.

The primary agent remains accountable for the work. Agreement between models
is not evidence, does not replace tests, and never grants owner approval.

## Review Modes

### Slice peer review

After implementing a delivery slice and passing its automated tests, the
primary agent MUST invoke a fresh external-agent session to review the current
requirements and diff. The reviewer is read-only and focuses on defects,
regressions, security, performance, missing tests, and SDD divergence.

The primary agent verifies every finding against source, tests, and governed
requirements. It may accept or reject a finding with recorded evidence. At
most two challenge-and-response rounds are allowed. An unresolved material
finding blocks the slice and is escalated to the owner; it is never silently
discarded by the primary agent.

Slice peer review improves implementation quality but is not a Stage 2, 6, or
10 independent pass unless every formal-pass rule below is also satisfied.

### Formal independent pass

The primary agent may act as an orchestration transport for an eligible
external reviewer. The external reviewer, not the primary agent, owns the
review conclusion. It MUST use a fresh session with no shared authoring
context, complete the independence declaration, work read-only from an
isolated worktree or immutable revision, and cover the entire declared scope
without sampling.

For a formal pass, the packet supplies immutable base/head revisions and the
reviewer independently regenerates and enumerates the authoritative diff and
scope. An orchestrator-supplied diff is convenience context only and cannot be
the reviewer's source of completeness.

The orchestrator may challenge a factual finding by supplying concrete
evidence. The reviewer must issue the final classification. The orchestrator
MUST NOT rewrite a `findings` or `blocked` conclusion into `clean`. Raw reviewer
responses and discussion rounds are durably retained under
`analysis/reviews/evidence/<packet-id>/` with a digest referenced by the
immutable review report.

## Review Packet

Before invocation, the primary agent creates a deterministic packet containing:

1. a packet identifier and repository revision;
2. the review mode, scope, exclusions, and required result schema;
3. links to the applicable constitution, methodology, SDD, tasks, and workbook;
4. the base and head revisions plus the diff or exact commands to obtain it;
5. automated test commands and their latest results;
6. independence and read-only constraints;
7. the expected output location outside the reviewed worktree.

Before sending a packet, the primary agent MUST classify its contents for
external-model use. Secrets, credentials, personal data, regulated data, and
repository content not approved for the selected service MUST NOT leave the
authorized environment. If a safe packet cannot be formed, external review is
`blocked` and the owner chooses an approved reviewer/environment; redaction must
not remove evidence required by the declared scope.

The reviewer first echoes the packet identifier, revision, scope, and its
eligibility. A mismatch blocks the review before substantive work begins.

## Read-Only Execution

- Use a new external-agent session ID. Never resume an authoring or earlier
  review session.
- Disable project hooks, memory, and tools that can write when the external CLI
  supports it. Permit only the minimum read/search/test tools.
- A pre-commit slice peer review may inspect the author's expected dirty delta.
  Persist its initial `git status --short`, diff digest, and untracked-file
  manifest, then require the exact same state after review. Any reviewer-added
  delta blocks the review.
- A formal pass uses a committed immutable review ref in a separate worktree.
  The reviewer independently regenerates the scope from the declared base/head
  revisions. Record `git status --short` before and after; both states must be
  clean.
- The reviewer never commits, pushes, opens a PR, updates the workbook, or
  edits the status file. The primary agent materializes the reviewer's
  structured result without changing its conclusion.
- Authentication preflight and a one-line read-tool smoke check must pass
  before a long invocation. Authentication, model, tool, or timeout failures
  produce `blocked`, never `clean`.

## Context Budget and Checkpoints

Large reviews MUST be partitioned into deterministic batches before they are
started. A batch names its exact rows, files, requirements, or diff paths and
produces a structured checkpoint with:

- packet identifier, revision, and batch identifier;
- completed and remaining scope;
- findings and unresolved questions;
- test/audit commands already run;
- context reset count and the next expected batch.

The orchestrator persists the canonical packet, expected worktree state, every
checkpoint, raw response, and digest under the durable evidence location before
starting the next batch. If the orchestrator itself compacts, restarts, or loses
context, it reloads that state and revalidates it before sending another
message. Failure to do so blocks the review.

Do not rely on a single opaque long-running prompt for a full repository pass.
If the CLI exposes context usage, the orchestrator stops before the configured
limit. In an interactive session it may request `/compact`; in a noninteractive
session it starts a fresh session. In both cases it resends the canonical
review packet, the complete checkpoint, and the still-required diff/evidence.
The reviewer must echo the packet, revision, completed scope, and remaining
scope before continuing.

Missing acknowledgements, reviewer or orchestrator context overflow, an
unverifiable checkpoint, a timed-out batch, or a missing batch makes the whole pass `blocked`. A final
consolidation may return `clean` only when every planned batch is present and
the consolidating fresh reviewer confirms full-scope coverage and repository
cleanliness.

Blocked attempts are never deleted or relabeled. Record their reason and exact
lost scope, then assign that scope to later eligible fresh sessions. A pass may
close only when `unresolved_blocked_scopes` is zero and the consolidator checks
the closure mapping. Account quotas and tool denials are blockers just like
timeouts; they are not evidence about the reviewed artifacts.

## Discussion and Completion

1. The reviewer returns structured findings with severity, file/row evidence,
   requirement impact, and a proposed check or correction.
2. The primary agent independently validates each finding.
3. Accepted findings are fixed and all affected tests are rerun.
4. Rejected findings receive concrete evidence in a challenge packet.
5. The reviewer may respond once more; no more than two total discussion
   rounds are permitted.
6. A material final diff is reviewed again by a fresh external session.
7. The primary agent reports accepted, rejected, and unresolved findings plus
   exact test results. The durable report uses a compact interaction log:
   reviewer finding, evidence, primary-agent disposition, correction, and
   repeat-review outcome. Commit, push, merge, and owner-gate actions follow the
   owner's current explicit instructions; the external reviewer performs none
   of them.

For formal passes, also follow
[`reviews/README.md`](reviews/README.md), create the immutable stage report,
append `review_passes` in `migration_status.yaml`, and stop at the owner gate.
