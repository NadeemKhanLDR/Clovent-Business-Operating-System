# Company ownership / aging and transaction reconciliation — 12 November 2026

## Revision and isolation

New branch: **audit/company-transaction-integration-20261112**.

| Input | Verified full SHA |
|---|---|
| Latest transaction/reporting tip at start | 0ae813467ca1cad94fea59ea7a5ec9ed44bdf01a |
| Approved ownership/aging source | 2e572088aa236e49b54e56de991d7c1cdc1af9e8 |
| Common ancestor | 27217fddf9e7adecc15e2cb6502c85f6a47b4bfc |
| Ownership patch applied on latest tip | 45a2b8d28f4aa0b061da9ed9599e5ced41118995 |

Worktree: C:/Users/NexGen/.codex/visualizations/2026/11/04/01a1a636-2871-7d12-87ed-1a1ba60aa9bf/cbos-company-transaction-review

Both source branches/worktrees were clean and were preserved. Tool inventories exposed
no separate active integration agent to address (only this agent/chat). No other worktree
or live editor was written. The integration is a reviewed cherry-pick plus corrections,
not a main merge. Three conflicts were resolved by retaining the real ownership/aging
assertions and historical documentation; the latest continuity/shift implementation was
retained. The partial test-class declaration needed an explicit integration correction.

Comparing complete source tips also exposed inherited reporting fixes absent from the
latest tip. Exact card aliases, frozen method labels and non-cash credit presentation
were carried forward from the ownership source's ancestry, rather than silently lost.
The latest tip's existing KPI calculation was retained. BillSalesExcludingTax is explicit;
the mixed example is bill payable 243, excluding-tax bill amount 225, tax 18. No assertion
that every sales metric is 243 was introduced. The latest CloseShift changes were retained.

The final correction commit containing this document is the handoff for this combined
review. Earlier standalone branch results are not substituted for the executions below.

## Findings and verified corrections

| Finding | Cause | Correction / evidence |
|---|---|---|
| Foreign financial targets could reach handlers through caller IDs | Several financial requests lacked a persisted ownership check at the runtime boundary | Standard DI handlers now authorize order/customer/report targets through FinancialCompanyScope, trusted ambient context and persisted hierarchy before processing |
| Refund branch metadata could come from the request or a warehouse-ID fallback | Branch IDs were treated as attribution without authoritative binding | Runtime refunds reject contradictory claimed branches and use the verified persisted branch |
| Collection/payment shift or completion organization claims could conflict with authority | These are request fields, not authority | Shift company/branch and claimed organization are checked against the trusted company/organization and order hierarchy |
| Replay accepted a signed foreign warehouse without tenant verification | Signature verification is not company authorization | Runtime replay validates active scope against persisted warehouse/branch before locks/idempotency and again before persistence |
| Replay authorization failure could still acknowledge an existing foreign payment | Generic exception recovery performed unscoped idempotency lookup | Recovery revalidates scope; both normal and recovery duplicate paths verify the persisted warehouse, completed state, non-voided payment and matching amount before acknowledgement |
| Bulk operations bypass SaveChanges owner/deletion guards | ExecuteUpdate/ExecuteDelete do not call the override | New physical triggers prohibit established owner reassignment, customer/ledger deletion and unapproved legacy-owner assignment |
| A schema-DML principal could fabricate mapping approval rows | Mapping table lacked a separate permission boundary | Hardening migration denies public access to mapping metadata; an ordinary no-login schema-DML user is rejected, while normal owned-account financial DML remains usable |
| Mapping ignored order/refund/shift/allocation ownership contradictions | Validation previously checked accounts and ledger owners only | Mapping now rejects missing/foreign/ambiguous transaction hierarchy and cross-customer allocation references before assigning ownership |
| Non-cash restored advance was labelled received money in movement rows | Header fix did not carry through to the row | Separate external receipts, non-cash AR credits and non-cash advance credits; corresponding grid columns and reconciliation assertions retained |

No transaction-lock algorithm or recovery journal format was replaced. Authorization
checks wrap the existing command/persistence behavior. New and existing customer/ledger
owners remain company-bound; unmapped account/ledger rows remain excluded, never defaulted
to whichever company happens to execute the request.

### Authority source and scope boundaries

Desktop login and POS entry use ITrustedScopeResolver with the authenticated stored
session user, verify persisted hierarchy/user assignment, then establish ambient scope.
Platform resolver and session-wiring regressions are executed. CompanyId is not a
customer-create DTO ownership field. The financial guard reads that trusted context;
warehouse, branch, shift, customer and organization IDs in requests are targets/claims
to validate, never instructions to establish scope. A journal's branch/warehouse IDs
similarly do not create an ambient company context.

Scoped runtime persistence filters customer, ledger and allocation reads. Customer
statement/ledger queries also explicitly check owners and reject absent scope. Standard
DI supplies the accessor even when Infrastructure is composed alone. The existing
unscoped DbContext/design-time constructors and manually constructed handler test seams
remain trusted tooling seams, not supported interactive tenant APIs. Native SQL row-level
security is not introduced; direct privileged SQL remains outside application query filters.

No production customer-repository bulk/raw SQL bypass was found. Tests nevertheless cover
foreign scoped bulk DML affecting zero rows and physical protections against reassignment
and deletion, including accounts without ledger dependencies.

### Mapping and preservation

Use these exact migrations, in order, from one reviewed artifact:

1. 20261111220000_AddCustomerCompanyOwnership — nullable expand/quarantine columns,
   indexes, composite ledger/customer company FK and explicit mapping table.
2. 20261112010000_PreventCompanyOwnershipReassignment — ownership/no-delete/approved
   legacy assignment triggers and administrative mapping-table protection.

Null legacy ownership remains supported during the controlled expand stage. It is not
permission to expose those accounts or to guess a default company. New runtime rows
are stamped from the verified company before insert. The loader requires complete explicit
mapping, matching actual database, integrated authentication and nonempty approval evidence.
Duplicate CSV identities fail before opening SQL; the table key prevents multiple owners.
Different existing approvals cannot be silently overwritten.

The SQL action rejects an account linked to conflicting companies, foreign/unknown order
or refund warehouse hierarchy, foreign/unknown ledger shifts, missing customer/order/ledger
references and cross-customer allocations. Such history stays unchanged and quarantined;
account splitting or financial remediation requires a separate reviewed plan.

Mapping changes ownership metadata only. Before/after controls check IDs, account balances
and timestamps, original ledger amounts/dates/references, allocations, audit entries,
orders, payments and refunds. A deterministic SQL check-constraint injection forces failure
after customer assignment but before ledger assignment completes: the real SQL transaction
rolls both assignments back and preserves balances. The synthetic constraint is then
removed; no operational schema was touched.

The SQL model/snapshot pending-change check passes. Migration Up operations, physical FK/
triggers, loader, preflights, invalid inputs, rollback and idempotent mapping were exercised.
This is not execution of every historical migration or operational backup restoration.

### Aging

Approved rule: retain proportional shares, floor bucket shares to cents, then distribute
residual cents by largest fractional remainder. Equal remainders go oldest first:
60+, 31–60, 16–30, 8–15, 1–7, Current. Ledger amounts and debt-allocation policy do not change.

| Independent example | Expected and actual |
|---|---|
| Two 0.25 debts aged 2 and 10 days; collection 0.49 | Receivable 0.01; 1–7 bucket 0.00; 8–15 bucket 0.01; sum 0.01 |
| Two equal 0.015 shares | Younger 0.01, older 0.02; sum 0.03 |
| SQL decimal(18,2) maximum 9999999999999999.99, equal fractional halves | Younger 4999999999999999.99, older 5000000000000000.00; exact sum |
| Zero receivable / customer advance | All aging buckets zero; advance remains separately positive |
| Days 0,1,7,8,15,16,30,31,60,61 | Each lands in its expected bucket; total preserved |

Negative helper receivables and inconsistent input totals are rejected. Intermediate shares
may be fractional; this does not add new settled-currency precision or alter historical
ledger chronology.

## Actual combined verification

Evidence directory: C:/Users/NexGen/.codex/visualizations/2026/11/04/01a1a636-2871-7d12-87ed-1a1ba60aa9bf/company-transaction-evidence-20261112

Build outputs: C:/Users/NexGen/AppData/Local/Temp/CBOS-company-transaction-20261112.

| Execution | Actual result |
|---|---|
| Full application suite, application-final-current.trx | 525 passed, 0 failed/skipped |
| Full Restaurant domain suite, domain-complete.trx | 255 passed, 0 failed/skipped |
| Trusted resolver / scope enforcement, trusted-scope.trx | 33 passed, 0 failed/skipped |
| Combined infrastructure, infrastructure-reconciled.trx | 43 passed, 0 failed/skipped |
| Additional mapping rollback probe, mapping-recovery.trx | 1 passed; supplements an existing case, not an additional unique combined case |
| Desktop formatters, production DI and authenticated session wiring, desktop-complete.trx | 18 passed, 0 failed/skipped |
| Debug / Release solution builds | Passed, zero warnings/errors |

Of the 43 infrastructure cases, **29 use SQL LocalDB**, 10 use test-owned SQLite
serialization databases and 4 use temporary SQLite/protected-journal replay fixtures.
SQL: Microsoft.EntityFrameworkCore.SqlServer, **(localdb)\MSSQLLocalDB**, engine
**17.00.4025**. sql-reconciled.jsonl records **29 created / 29 cleanup-verified** GUID
databases; corresponding name sets match. The supplemental rollback probe also creates
and cleans its own generated database. Guard checks include provider, actual DB_NAME,
integrated authentication, explicit disposable names and DB_ID=NULL after deletion.
No application configuration target or production/customer database was opened.

The new SQL command-pipeline case uses real handlers, transaction/persistence behaviors,
SQL repositories and real company scope logic. Its hierarchy adapter reads actual
synthetic SQL hierarchy rows. Foreign collection, payment and refund requests leave
balances/financial row counts unchanged; an owned collection posts 10 once and leaves
90 outstanding. A signed foreign replay with an existing payment key is rejected, retained
for manager review and not reported as duplicate. An authorized own-warehouse journal colliding with the same foreign committed key is also rejected; no replay order/payment is added.

Older transaction/worker and cryptography stress fixtures explicitly supply synthetic
authorization collaborators: they qualify transaction/locking/cryptography behavior,
not tenant authorization. Their original financial assertions remain intact; no failure
was skipped or assertion weakened. Real authority checks are separately exercised as
described above. Initial compilation/fixture failures and intermediate logs remain in
evidence; they are not presented as successful executions.

## Concrete deployment checklist — separate authorization required

### Preflight

1. Pin this final commit, migration/script hashes, code version and exact expected database.
   Confirm target migration history and all required Identity/MasterData/Restaurant schemas.
   Review the full target's pending migration chain; this audit did not qualify an unknown
   customer schema or every historical upgrade path.
2. Under an authorized read-only target session, run
   [OwnershipBaselinePreflight.sql](../2026-11-11/company-ownership/OwnershipBaselinePreflight.sql).
   Capture identity, version, IDs, opening/outstanding balances, debit/credit controls,
   allocation totals, order/payment/refund/audit counts and existing migration history.
   Repeat stable controls after writers are stopped.
3. Obtain the complete reviewed UTF-8 CSV from
   [mapping-template.csv](../2026-11-11/company-ownership/mapping-template.csv):
   CustomerId, CompanyId, ApprovedBy, ApprovedAtUtc (UTC), EvidenceReference.
   Include inactive accounts, advances, opening balances and customers without orders.
   Resolve ambiguous/cross-company histories explicitly. Do not infer mapping from
   transactions or choose a default company.
4. Verify current company registry and reference graph, custom payment aliases, supported
   decimal precision and expected aging results. Plan TempDB/log capacity for preservation
   snapshots, a bounded maintenance timeout, a verified full backup and protected journal
   retention. No operational capacity/restore check was performed here.

### Maintenance and admission

1. With separately approved maintenance authority, stop application and offline-continuity
   writers at every affected terminal. Preserve pending journals and their hashes. Confirm
   no active write transaction and capture final baseline controls.
2. Use the reviewed database-owner/sysadmin maintenance principal to apply both migrations.
   Do not grant persistent app/OS privileges or give the financial app mapping-table access.
   Public mapping-table denial is intentional; approval text alone is not authorization.
3. Run [OwnershipPreflight.sql](../2026-11-11/company-ownership/OwnershipPreflight.sql).
   Require both migration history entries, all five enabled protection triggers, expected
   schema and unchanged pre-map financial controls. Null legacy owners are expected before
   assignment, but the new application must not be admitted yet.
4. Run [Invoke-ExplicitMapping.ps1](../2026-11-11/company-ownership/Invoke-ExplicitMapping.ps1)
   in review-only mode first (no connection opens). With the separate apply authorization,
   use its Apply switch on the exact same approved artifact/target. It hashes the parsed
   bytes and executes
   [ApplyExplicitMapping.sql](../2026-11-11/company-ownership/ApplyExplicitMapping.sql)
   on a dedicated connection. Save output, source/mapping hashes and approval evidence.
5. Require zero unmapped accounts/ledger rows, unchanged financial/history controls,
   matching ledger/customer company keys and no invalid references. Verify owned and
   foreign reads, collection/refund denial and company-switch reuse before admitting traffic.
   New/empty-database commissioning and seed scope must be separately qualified.

### Recovery

1. On validation failure or timeout, keep maintenance closed; preserve the exact error,
   target, mapping hash, approval rows and control outputs. Never fix a map by guessing,
   deleting financial records, or changing an established owner.
2. A failed ownership transaction rolls back customer and ledger assignments; already
   staged approval rows can remain. Reconnect to the same verified target and inspect the
   read-only controls. An uncertain acknowledgement is not proof that mapping failed.
3. If assignments are still null and controls unchanged, diagnose the rejected reference/
   permission/input and retry only the identical approved artifact where appropriate.
   Different approval metadata is deliberately rejected and requires review.
4. If mapping committed, do not run a destructive Down migration. Use an approved forward
   repair or the verified backup/restore plan under DBA authority; retain journals and
   reconciliation evidence. Do not reopen a multi-company application with known isolation
   failures merely to return to an older build.

## Manual checks and remaining limitations

- Under Company A, verify A balances, collect 10 from 100 to 90, and confirm B's known
  customer/order/shift IDs cannot reveal data or post payments/refunds. Switch to B and
  refresh/reopen the same screens; sign out and confirm no previously rendered A data
  remains. UI cache invalidation and actual rendering remain unverified.
- Verify 0.01 aging tie goes to 8–15 rather than 1–7; inspect all day boundaries and an
  advance-only statement. Check non-cash AR/advance credit columns at normal/narrow widths.
- Verify mixed sale 250 minus discounts 25 plus exclusive tax 18 gives bill 243 and an
  excluding-tax bill amount 225. Reprint new method labels and unchanged historical generic
  receipts; compare Tax Summary and Refund Audit over different sale/refund dates.
- Retry owned pending replay and reject a foreign/mismatched branch journal. Do not treat
  a signature or a committed idempotency key as tenant authority. No cross-branch refund
  policy is invented: contradictory order/shift/claimed branch targets fail closed.
- Existing UTC/business-date contract differences, custom tender mappings, exhaustive
  permission/manager-approval coverage, operational startup/seed paths, UI and real
  printing are not qualified by this integration.
- The pre-existing continuity signing-key design uses static source key material
  (EmergencyTransaction.cs); no key provisioning/rotation or journal format migration was
  attempted. These ownership tests do **not** certify complete offline tamper resistance.
  That requires a separate compatible security review before offline integrity acceptance.

**Still missing for deployment:** the real approved legacy mapping, authorized target and
maintenance window, target upgrade/backup/restore qualification and manual acceptance.
Deployment remains separately authorized. Live UI and physical printing are
**NOT VERIFIED**. No main merge, push, package, operational migration, Visual Studio
control, Sandbox or Hyper-V operation occurred.
