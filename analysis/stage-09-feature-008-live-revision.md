# Stage 9 Live Revision - Feature 008

## Scope

- Feature: `008-data-initialization`
- Workbook rows: 119-126
- Date: 2026-07-23
- Result: clean after the delivery correction loop

## Modern Walkthrough

The deployed revision was inspected as an operator-controlled data lifecycle:

- API startup used the restricted `bankofz_api` SQL principal.
- Schema inspection reported the expected single pending migration.
- The explicit operator command applied that migration and recorded its audit.
- Existing identities and banking data remained intact.
- No reset, import, migration, or provisioning ran as an API startup side
  effect.
- Public customer, operator, and administrator access remained available.

## Delivery Findings

The first public Playwright run produced 13 passes and one failure. The failed
insufficient-funds scenario used a hard-coded `1000.00` transfer. Repeated
demo runs had legitimately increased the account balance beyond that value.
The test now derives a rejected amount from the live available balance.

The repeated full run then exposed a second accumulated-state assumption: the
account test expected the newly generated account link on the page selected by
the UI's last-page calculation, while seeded account `10000099` can sort after
new generated IDs. The test now opens the successfully created account by its
returned ID because the scenario under test is account and cash management,
not list pagination. The pagination observation is retained for a later UX
slice rather than changing an accepted account-list contract inside Feature
008.

After those test corrections, all 14 public HTTPS scenarios passed. No target
product defect, parity gap, data loss, or unauthorized startup mutation was
observed.

## Gates

- Backend unit tests: 99 passed.
- Real SQL Server integration tests: 67 passed.
- Angular tests: 47 passed.
- Angular production build: passed.
- Public HTTPS Playwright: 14 passed.
- Legacy simulator: 28 passed.
- Workbook audit: passed, 109 closed and 26 open rows.
- SDD audit: passed.
- Legacy evidence audit: passed, 512 references.
- Claude implementation peer review: two usable rounds; final verdict clean.

The delivered slice proceeds to fresh read-only Stage 10 acceptance.
