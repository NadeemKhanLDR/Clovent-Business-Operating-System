# Approved company ownership and proportional aging

Historical standalone checkpoint. The current combined revision, strengthened migration
and active preflight/maintenance/recovery checklist are in
[company/transaction review](../../2026-11-12/company-transaction-review.md).

Continuation of review commit **92a39844932d8c29862d87b08dba6b964408ca9d** on
`audit/reporting-integration-review-20261111`. Original transaction and reporting
branches are preserved. The user approved company-owned customers/ledgers with explicit
legacy mapping, and proportional aging with largest fractional remainders.

During the final check another workstream had created
`audit/final-transaction-reporting-integration-20261111` at
**0ae813467ca1cad94fea59ea7a5ec9ed44bdf01a** in the D: integration worktree.
It was not imported or edited. This follow-up remains pinned to 92a3984; its test
results do not qualify 0ae8134. No difference was found in the customer production files
or RestaurantDbContext between those checkpoints at inspection, but a later integration
must still rerun combined checks. The commit containing this README is the policy handoff.

## Implemented contract

- Customer and ledger CompanyId are assigned once. New runtime accounts use the verified
  active company; new ledger entries must match their customer's owner. Ownership is not
  exposed as a caller-selectable customer-create field.
- Customer, ledger and allocation reads in runtime persistence are company-filtered.
  Missing/empty scope gives no rows; statement/ledger requests explicitly reject missing
  scope. Ledger reads also require the parent account to belong to that company.
- Statement handlers check ownership independently of repository filtering. Other customer
  queries declare company-scope requirements. Runtime persistence DI always supplies the
  execution accessor, even if only Infrastructure is registered.
- New/modified company-sensitive account, ledger, order/customer and allocation writes
  reject a different or unmapped owner. A stale tracked account cannot be saved after
  switching companies. Unrelated outbox persistence does not acquire a new customer-scope
  requirement. No transaction locks/recovery implementation was replaced.
- Explicit contexts constructed without the accessor remain trusted design-time/disposable
  harness contexts. They must not be used by interactive code. Actual persistence DI
  injection, including missing scope, is SQL-tested; this is not an ambient global-admin mode.
- Legacy null owners are quarantined, not guessed from orders, branches or balances.
  They become visible only after reviewed explicit mapping. Creation/update timestamps,
  opening/outstanding balances and original ledger facts are unchanged by mapping.

The nullable columns are the **expand/quarantine stage**, not an assertion that historical
ownership is optional. The mapping action requires full account/ledger coverage before it
commits. It does not split shared legacy accounts, resolve conflicting historic use, or
invent how an opening balance should be distributed. Those cases require explicit mapping
review before deployment.

### Aging rounding

Retain the existing proportional debt shares. Floor each nonnegative aggregate bucket
share to cents, then distribute the remaining cents by descending fractional remainder.
**Tie order is oldest first: 60+, 31–60, 16–30, 8–15, 1–7, Current.**
The total target uses MoneyRoundingPolicy. This changes report rounding, not payment
allocation, ledger amounts, FIFO behavior or persisted financial facts.

For two 0.25 debts aged 2 and 10 days and an unallocated payment 0.49, the receivable is
0.01. Equal raw shares are 0.005; the 8–15-day bucket gets 0.01, the 1–7 bucket gets 0.00,
and the sum is exactly 0.01. The original invariant assertion remains enabled.

## Prepared migration and mapping action

Migration: `20261111220000_AddCustomerCompanyOwnership`.
It adds nullable ownership columns, indexes, an explicit approval mapping table, and
a physical composite company/customer foreign key from ledger entries. The FK is
declared in the migration because EF alternate keys would force null legacy owners to
be required during quarantine; preserve/review this explicit constraint in future migrations.
The current global customer-code uniqueness rule is unchanged.

Files:

- `mapping-template.csv`: **empty template**, not approved deployment data.
- `Invoke-ExplicitMapping.ps1`: validates UTF-8 CSV and integrated-auth target. Default
  invocation is review-only and does not open a connection. Apply requires the explicit
  Apply switch and matching ExpectedDatabase.
- `ApplyExplicitMapping.sql`: checks the actual target and company registry; rejects
  missing, invalid, conflicting or unapproved mapping. Uses a dedicated connection and
  a serializable transaction. It verifies preserved financial/history facts in both
  directions before commit and requires all account/ledger owners to be mapped.

Each CSV row requires CustomerId, CompanyId, ApprovedBy, ApprovedAtUtc (UTC offset zero),
and EvidenceReference. The loader hashes the exact bytes parsed before opening SQL,
uses parameterized statements and refuses to overwrite an existing different approval.
Staging approval rows may remain if validation/connection fails after staging; financial
ownership assignment is atomic. Retry the identical reviewed mapping after diagnosing the
error. Do not silently replace approval rows or infer missing owners.

### Required deployment sequence — not performed

1. Obtain a complete reviewed customer-to-company mapping, including opening balances,
   advances, inactive customers and accounts without orders. Resolve legacy shared or
   conflicting accounts explicitly. Verify every target company against Identity.Companies.
2. Review the exact target, backup/restore plan and maintenance window. Stop application
   writers. Apply only the expand migration to the approved target through the controlled
   migration runner. Do not start the new application between expansion and completed mapping.
3. Run the mapping loader first in review-only mode, then apply the approved identical
   artifact on the verified target. Save output, mapping SHA-256 and approval evidence.
   The SQL action checks complete coverage and unchanged financial/history facts.
4. Validate company A/B isolation, scope reuse, opening balances and statements on the
   migrated target, then admit application traffic. Startup/seed paths must establish
   authoritative company scope before creating customer data.

**Missing deployment prerequisite:** actual approved legacy mapping rows and an authorized
target/maintenance action. No operational database was read, migrated or mapped here.
The loader was executed only against disposable synthetic SQL. Down migration intentionally
rejects destructive ownership removal; a rollback requires a separately reviewed forward
repair or the approved backup/restore procedure.

## Executed evidence and limits

Evidence directory: C:/Users/NexGen/.codex/visualizations/2026/11/04/01a1a636-2871-7d12-87ed-1a1ba60aa9bf/transaction-reporting-integration-evidence-20261111-ownership-aging

Artifacts: C:/Users/NexGen/AppData/Local/Temp/CBOS-company-aging-20261111.

- Full application suite: **514 passed**, including both formerly failing assertions.
- Full domain suite: **250 passed**, including largest remainder, older-first tie,
  exact amounts and zero tests.
- Combined infrastructure suite: **33 passed**: 23 disposable SQL cases plus 10 test-owned
  SQLite serialization cases. This includes the original reporting/recovery/locking/outbox
  coverage and two ownership/migration cases.
- Formatter/reconciliation/production composition: **12 passed**.
- Debug and Release solution builds: **PASS, zero warnings/errors** (build-debug.log /
  build-release.log).

SQL identity: Microsoft.EntityFrameworkCore.SqlServer, (localdb)\MSSQLLocalDB,
**17.00.4025**. SQL guards verify actual catalog/provider and generated GUID targets,
then delete and verify DB_ID is NULL. Final sql-final.jsonl contains **23 created /
23 cleanup-verified** pairs. No credentials are logged.

The ownership SQL case verifies real persistence registration, A/B isolation using the
same context, unknown-owner quarantine, absent scope, automatic new ownership, rejection
of a foreign tracked write, and ledger endpoint denial. The migration case recreates
the pre-expand shape inside a disposable database, runs the real migration Up operations
through the SQL Server provider, rejects incomplete/invalid/unapproved mapping, executes
the prepared PowerShell loader, verifies idempotent mapping and unchanged financial facts,
and rejects a forged ledger company via the physical FK.

This qualifies the **ownership expand operations and mapping action**; it does not qualify
the entire historical migration chain, operational backup restoration, customer mapping
data that has not been supplied, startup seeding on an operational target, or every
authorization surface in the application. Earlier fixture/compiler/loader errors remain
in evidence logs; final passing results supersede them rather than hiding them.
No package upgrade, production access, deployment, push, merge, packaging or VM work occurred.

Use the single active [consolidated manual checklist](../reporting-integration-review.md#one-consolidated-manual-checklist).
Live UI, Designer rendering, physical printing and operational migration remain **NOT VERIFIED**.
