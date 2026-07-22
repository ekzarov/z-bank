# Stage NN Review - Pass NNN

## Metadata

- Date: YYYY-MM-DD
- Stage: NN
- Pass: NNN
- Agent/tool: identify the product and session/task, without credentials
- Orchestration packet/revision: identifier and digest, or `direct`
- Result: `clean` | `findings` | `blocked`

## Independence Declaration

- [ ] I did not create or edit any artifact in this review scope.
- [ ] My current context does not contain the primary agent's working session.
- [ ] I formed my own evidence inventory before reading prior conclusions when
      the stage requires from-scratch discovery.

If any box cannot be checked, stop and set Result to `blocked` with the reason.

## Scope and Inputs

List every repository revision, artifact, source area, specification, deployed
URL/channel, and owner decision used. For Stage 2, distinguish the initial
code-only inventory from the later comparison inputs.

## Method

Describe the complete sweep performed. Do not write only "reviewed" or
"looks good". State how completeness, evidence references, and contradictions
were checked.

For an orchestrated external review, list every deterministic batch, its scope,
checkpoint, context reset, raw-response digest/reference, and final
acknowledgement. State whether `git status --short` was clean before and after.

## Findings

Use one subsection per finding with severity, affected workbook rows or SDD
requirements, concrete evidence, and required action. Write `None` only after
the declared scope has been fully checked.

## Automated Gates

Record each command/check and its result. Include the workbook audit whenever
the filled map changed.

## Conclusion and Next Gate

State why the result is clean, has findings, or is blocked; where the process
loops next; and the exact owner decision or independent pass required.
