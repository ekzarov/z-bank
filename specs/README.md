# Bank of Z Stage 5 Delivery Design

The specifications below cover every scenario row in
`analysis/legacy_user_flows.xlsx`. They are ordered delivery slices, not a
single big-bang implementation backlog. After the Stage 6 design gate is clean
and the owner approves implementation, each slice independently traverses
Stages 7, 8, 9, and 10 before the next slice starts.

| Order | Slice | Workbook rows | Stage 4 decisions |
|---|---|---|---|
| 1 | `001-secure-access-shell` | 8-20, 109, 113-117 | D-001, D-002, D-014 |
| 2 | `002-customer-management` | 22-39, 87, 90, 97 | D-008, D-009, D-010 |
| 3 | `003-account-management` | 41-58, 88, 110-112 | D-007, D-009, D-011 |
| 4 | `004-cash-transactions` | 60-78, 89 | D-003 through D-006, D-011 |
| 5 | `005-funds-transfers` | 80-85 | D-004, D-006, D-011 |
| 6 | `006-transaction-history` | 91-96, 98 | D-006, D-008 |
| 7 | `007-monthly-statements` | 100-107 | D-012 |
| 8 | `008-data-initialization` | 119-126 | D-013 |
| 9 | `009-delivery-operations` | 128-153 | D-014, D-015 |

Rows marked `Runtime: simulated`, `Inferred`, or `Partial` retain that evidence
classification. SDD coverage does not promote them to real-runtime verified.
