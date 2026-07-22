# Feature 003 peer review 001

- Agent: Claude Code CLI, read-only plan mode
- Result: FINDINGS (no blocking findings)
- Scope: Feature 003 production code, tests, migration semantics, and UI

## Findings

1. The ten-active-account invariant used a count followed by an insert without
   a serializable transaction, allowing two concurrent creates to exceed the
   limit.
2. The portfolio UI loaded only the first 50 rows and exposed no pagination,
   so accumulated closed accounts could become unreachable.
3. The account detail route read `ActivatedRoute.snapshot` once and would not
   react if Angular reused the component for another account id.
4. Customer-forbidden account mutations were not covered by API tests.
5. Atomic rollback of account plus audit persistence lacked a forced-failure
   SQL Server test.
6. SQL check constraints and decimal precision lacked direct persistence tests.

## Observations

- Confirmed relationship-based non-disclosing reads, string identifiers,
  target enum serialization, soft closure, rowversion mapping, and migration
  Up/Down semantics.
- Suggested replacing the customer-id length literal with
  `CustomerRules.IdLength`.
- Sequence gaps are correctly permitted by D-023.

## Orchestrator adjudication

All six findings are accepted for correction in this slice. The customer-id
constant cleanup is also accepted. The observations about provenance exposure,
closed accounts not counting toward the active limit, and currency formatting
do not contradict the approved SDD and require no scope change.
