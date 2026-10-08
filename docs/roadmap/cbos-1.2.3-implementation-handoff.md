# CBOS 1.2.3 Implementation Handoff & Hardening Specification

| Attribute | Details |
| :--- | :--- |
| **Milestone Target** | CBOS 1.2.3 — Controlled Attended Single-Terminal Pilot |
| **Preceding Baseline** | CBOS 1.2.2 Frozen Internal Acceptance Baseline (`d9d38cbb17828fe771568ca5ab6abb036fe8798b`) |
| **Database Migration Invariant** | **STRICT ZERO DATABASE MIGRATIONS** |
| **Audience** | Implementation Leads, Engineering Agents, QA Engineers |
| **Status** | **DRAFT — PENDING TECHNICAL SIGN-OFF** |
| **Classification** | **PUBLIC-SAFE** |

---

## 1. Executive Summary & Architectural Invariants

This handoff document provides the detailed technical specification for the ten canonical hardening tasks required to deliver **CBOS 1.2.3** as a controlled, attended, single-terminal commercial pilot release. This document is a **draft pending formal technical sign-off** and does not self-certify release approval or proven feasibility.

### Core Architectural Invariants:
1. **Strict Zero Database Migrations:** No schema modifications, no EF Core migrations, and no schema snapshot alterations may be introduced in CBOS 1.2.3. All persistence capabilities (idempotency, outbox messages, day close aggregation, and snapshot storage) must leverage existing database tables and columns created in the 1.2.2 baseline. Aggregate concurrency tokens (`RowVersion`) belong strictly to CBOS 1.3.0.
2. **Workstation State Isolation:** All automated tests must run against disposable databases or temporary folders. Tests must never modify `%ProgramData%`, `%LocalAppData%`, or operational databases.
3. **Multi-Stream Ownership Model:**
   - **Stream A:** Lead financial/security/configuration/messaging implementation (`src/Clovent.Restaurant*`, `src/Clovent.Identity*`, `src/Clovent.Authentication*`, `src/Clovent.Desktop`).
   - **Stream B:** Installer/signing/release engineering, without business-code edits (`installer/**`, `tools/ReleaseGuard/**`, `Tools/Clovent.Installer.Provisioner/**`).
   - **Stream C:** Tests, synchronized with Stream A (`src/Clovent.*.Tests/**`).
   - **High-Conflict File Ownership Table (Strict Single Active Owner):**
     | High-Conflict Path | Single Active Owner | Rationale |
     | :--- | :--- | :--- |
     | `src/Clovent.Desktop/Restaurant/Orders/RestaurantPosForm.cs` | **Stream A** | Primary POS UI touched across payment, void, and rounding workflows |
     | `src/Clovent.Restaurant/Orders/Order.cs` | **Stream A** | Order aggregate root handling lines, status, voids, and receipt snapshots |
     | `src/Clovent.Restaurant.Application/Payments/Commands/RecordPaymentCommand.cs` | **Stream A** | Payment command handling idempotency, balance enforcement, and credit limits |
     | `src/Clovent.Desktop/Restaurant/Shared/ManagerAuthorizationForm.cs` | **Stream A** | Manager elevation modal and authorization fallback logic |
     | `src/Clovent.Desktop/Program.cs` | **Stream A** | Host startup registrations (Outbox processor hosted service, config store) |
     | `src/Clovent.Desktop/Seed/DevelopmentUserSeedStartupTask.cs` | **Stream A** | Startup seed task gating and credential protection |
     | `installer/Clovent.BusinessOperatingSystem.iss` | **Stream B** | Installer bundling and package metadata |
     | `tools/ReleaseGuard/ScanReleasePackage.ps1` | **Stream B** | Release packaging integrity and Authenticode verification |
     | `Tools/Clovent.Installer.Provisioner/Clovent.Installer.Provisioner.csproj` | **Stream B** | First-party provisioner signing and build pipeline |
     | `src/Clovent.Desktop.Tests/**` & `src/Clovent.Restaurant*.Tests/**` | **Stream C** | Authoring unit and integration regression fixtures synchronized with Stream A |
   - **Implementation Launch Invariant:** Implementation is strictly not launched within governance closure. Workstreams remain ready for authorized branch work.
4. **Credential Investigation Scope:** In this current correction pass, no operational databases were queried, no account or credential tables were inspected, no password hashes were extracted, and no candidate credentials were tested. Findings and credential values from prior diagnostic passes have been completely removed from deliverable documents and exported review packages. (This qualification applies to the current correction pass; it does not claim that earlier diagnostic investigation never occurred).

---

## 2. Canonical Hardening Tasks (TASK-01 to TASK-10)

### TASK-01: RBAC & Manager Elevation Hardening

- **Existing Source Paths & Classes:**
  - `src/Clovent.Desktop/Authorization/AdministrativePrivilegeChecker.cs`
  - `src/Clovent.Desktop/Restaurant/Shared/ManagerAuthorizationForm.cs`
  - `src/Clovent.Desktop/Authorization/IManagerAuthorizationService.cs`
  - `src/Clovent.Desktop/Forms/Shell/MainForm.cs`
  - `src/Clovent.Desktop/Configuration/DatabaseConnectionDialog.cs`
  - `src/Clovent.Desktop/Licensing/SoftwareRegistrationForm.cs`
- **Current Defect / Required Behavior:**
  - `AdministrativePrivilegeChecker.HasAdministrativePrivileges()` contains a fast-path username check: `string.Equals(userName, "admin", ...)` or `"administrator"`, allowing any user with the username "admin" to bypass role verification.
  - `ManagerAuthorizationForm.PerformAuthorizeAsync()` contains a fallback: if `_authService == null`, it sets `DialogResult = DialogResult.OK` and returns `true`, failing open when authorization services are missing or unconfigured.
  - Required behavior: Eliminate username strings from privilege checks; require authenticated roles (`Administrator`) or explicit granular permissions. `ManagerAuthorizationForm` must fail closed (display error and return `false`) if `_authService` is null or throws an error.
  - Pre-login configuration changes require Windows UAC elevation; post-login changes require authenticated application Administrator authorization. Role checks do not bypass OS ACLs.
- **Intended Bounded Change:**
  - Remove username checks from `AdministrativePrivilegeChecker.cs`; query `ICurrentSession.Roles` or `IPermissionService`.
  - Remove fallback in `ManagerAuthorizationForm.cs` lines 281-285; fail closed if `_authService` is null.
- **Dependencies:** None.
- **Existing Tests to Extend & Regression Cases:**
  - Extend: `src/Clovent.Desktop.Tests/Security/SecurityAndLicensingHardeningTests.cs`
  - Extend: `src/Clovent.Desktop.Tests/Restaurant/Shared/ManagerAuthorizationFormTests.cs`
  - Regression cases:
    - User with username "admin" lacking Administrator role is denied administrative privileges.
    - User with custom username having Administrator role is granted administrative privileges.
    - `ManagerAuthorizationForm` fails closed when `_authService` is null, unconfigured, or throws network/DB exceptions.
- **Acceptance Criteria:**
  - Zero literal username checks in `AdministrativePrivilegeChecker`.
  - Zero fail-open paths in `ManagerAuthorizationForm`.
- **Zero-Migration Compliance:** 100% compliant (UI and application layer only).

---

### TASK-02: PIN Throttling + Development Seed Protection

- **Existing Source Paths & Classes:**
  - `src/Clovent.Desktop/Login/LoginService.cs`
  - `src/Clovent.Desktop/Forms/Identity/LoginForm.cs`
  - `src/Clovent.Desktop/Seed/DevelopmentUserSeedStartupTask.cs`
  - `src/Clovent.Desktop/appsettings.json`
  - `src/Clovent.Authentication/Credentials/UserCredentials.cs`
- **Proposed Components:**
  - `[Proposed New Service] src/Clovent.Desktop/Login/ITerminalPinThrottleService.cs`
- **Current Defect / Required Behavior:**
  - PIN sign-in resolves credentials by matching PBKDF2 hash against database rows. Unmatched PIN submissions do not associate with a user account, bypassing per-user failed-attempt counters and allowing unthrottled brute-force attempts.
  - Source-confirmed seed behavior: `DevelopmentUserSeedStartupTask.cs` matches on username `"admin"` or email `"admin@clovent.local"` and, when `Desktop:SeedDevelopmentUser` is enabled, overwrites the account's password hash if it does not verify against the built-in development password constant.
  - Required behavior:
    1. Apply rate limiting and progressive delays at the **authentication service boundary** (`LoginService` / `AuthenticateByPin`), not merely in the WinForms UI controls. Unmatched PIN attempts trigger progressive delays (e.g. 2-second delay after 3 failures; 30-second lockout after 5 failures).
    2. **Throttling Scope & Restart Limitations:** A process-local in-memory rate limiter protects against rapid automated guessing in the running application process, but resets if the application process restarts or if multiple application instances run concurrently. Full terminal-wide protection across restarts would require persistent cross-process state; this limitation must be explicitly documented.
    3. **Seed Protection:** `DevelopmentUserSeedStartupTask` must remain permanently disabled in production builds (guarded by `#if DEBUG` and environment gating) regardless of configuration flags. In all environments, existing user credentials must **never** be reset or overwritten by seeding; if a user already exists, seed execution must be a complete no-op.
- **Intended Bounded Change:**
  - Implement `ITerminalPinThrottleService` and enforce throttling at the authentication service boundary in `LoginService.cs`.
  - Modify `DevelopmentUserSeedStartupTask.cs`: if an existing user is found, never alter their password; only seed if no user exists and environment is explicitly non-production (`#if DEBUG`).
- **Dependencies:** TASK-01.
- **Existing Tests to Extend & Regression Cases:**
  - Extend: `src/Clovent.Desktop.Tests/Login/LoginServiceTests.cs`
  - Regression cases:
    - Rapid invalid PIN submissions trigger delay and lockout at the service boundary.
    - Valid PIN resets the failure counter upon successful login.
    - Existing administrator credentials are not overwritten by seed tasks when `SeedDevelopmentUser` is enabled.
- **Acceptance Criteria:**
  - Unmatched PIN submissions trigger progressive delay and lockout at the service boundary.
  - Seed tasks never overwrite existing credentials and cannot execute in production builds.
- **Zero-Migration Compliance:** 100% compliant (in-memory rate limiting and C# startup task gating).

---

### TASK-03: Financial Rounding Contract

- **Existing Source Paths & Classes:**
  - `src/Clovent.Restaurant/OrderLines/OrderLine.cs`
  - `src/Clovent.Restaurant/Orders/Order.cs`
  - `src/Clovent.Restaurant.Application/Orders/OrderTotalsCalculator.cs`
  - `src/Clovent.Desktop/Restaurant/Orders/RestaurantPosForm.cs`
  - `src/Clovent.Restaurant.Application/Continuity/EmergencyJournalReplayer.cs`
- **Proposed Components:**
  - `[Proposed New Service] src/Clovent.Restaurant/DomainServices/MoneyRoundingPolicy.cs`
- **Current Defect / Required Behavior:**
  - Financial calculations lack a single centralized rounding policy. Sporadic ad-hoc rounding occurs across components.
  - Required behavior: Implement a centralized domain service `MoneyRoundingPolicy` using `MidpointRounding.AwayFromZero` as the selected pilot midpoint direction.
  - **Supported Calculation Contract Sequence:**
    1. **Line Gross Base:** $\text{UnitPrice} \times \text{Quantity}$.
    2. **Line Discounts:** Deducted to produce line net amount.
    3. **Inclusive Tax Extraction:**
       $$\text{Net Price} = \text{RoundAwayFromZero}\left(\frac{\text{Gross Price}}{1 + \text{TaxRate}}\right)$$
       $$\text{Inclusive Tax} = \text{Gross Price} - \text{Net Price}$$
    4. **Exclusive Tax Addition:**
       $$\text{Exclusive Tax} = \text{RoundAwayFromZero}(\text{Line Subtotal} \times \text{TaxRate})$$
    5. **Order-Level Discount Allocation:** Apportioned across lines using the Largest Remainder Method (Hamilton-Hare) to distribute residual cents without rounding loss.
    6. **Service Charges & Fees:** Calculated on order subtotal and rounded AwayFromZero.
    7. **Payable Total:** $\sum \text{Line Subtotals} + \sum \text{Exclusive Taxes} - \text{Order Discounts} + \sum \text{Service Charges}$.
  - **Worked Dummy Examples:**
    - *Example 1 (Residual Cent Allocation):*
      An order discount of \$1.00 is applied across 3 identical \$10.00 items (Total = \$30.00).
      Unrounded discount per item = \$1.00 / 3 = \$0.333333...
      Base truncated discount = \$0.33 per item (\$0.99 total, residual = \$0.01).
      The largest remainder method allocates the residual cent to Line 1:
      Line 1 discount: \$0.34 (Net = \$9.66).
      Line 2 discount: \$0.33 (Net = \$9.67).
      Line 3 discount: \$0.33 (Net = \$9.67).
      Sum of discounts = \$1.00 exactly; sum of net lines = \$29.00 exactly. Zero penny drift.
    - *Example 2 (Inclusive Tax Extraction):*
      1 item with shelf price \$10.00 including 16% sales tax (TaxRate = 0.16).
      Pre-tax Net = \$10.00 / 1.16 = \$8.620689...
      $\text{RoundAwayFromZero}(8.620689...) = \$8.62$.
      $\text{Inclusive Tax} = \$10.00 - \$8.62 = \$1.38$.
      $\text{Net} (\$8.62) + \text{Tax} (\$1.38) = \$10.00$ exactly.
    - *Example 3 (Exclusive Tax Addition):*
      1 item with base price \$10.00 subject to 16% exclusive sales tax.
      $\text{Tax Amount} = \text{RoundAwayFromZero}(\$10.00 \times 0.16) = \$1.60$.
      $\text{Payable Total} = \$10.00 + \$1.60 = \$11.60$.
  - **Consistency Across Engines:**
    The identical calculation contract governs online POS calculations (`OrderTotalsCalculator`), offline continuity replay (`EmergencyJournalReplayer`), receipt printing, and Day Close summaries.
  - **Historical Transaction Immutability:**
    Do **not** recalculate completed historical transactions using current rules. Completed historical order records and snapshot JSON remain permanently immutable.
- **Intended Bounded Change:**
  - Introduce `MoneyRoundingPolicy` in `Clovent.Restaurant/DomainServices/`.
  - Refactor `OrderTotalsCalculator` and calculation paths to route through `MoneyRoundingPolicy`.
- **Dependencies:** None.
- **Existing Tests to Extend & Regression Cases:**
  - Extend: `src/Clovent.Restaurant.Tests/Orders/OrderTests.cs`
  - Extend: `src/Clovent.Restaurant.Tests/OrderLines/OrderLineTests.cs`
  - Regression cases:
    - Apportioning odd discounts across multiple items preserves penny-exact balance without floating-point error.
    - Inclusive and exclusive tax calculations match receipt totals to the penny.
    - Deterministic rounding at .005 boundaries under `AwayFromZero`.
- **Acceptance Criteria:**
  - Zero ad-hoc calls to `Math.Round` in financial calculation paths.
  - Centralized `MoneyRoundingPolicy` governs all lines, taxes, discounts, and totals.
- **Zero-Migration Compliance:** 100% compliant (pure domain calculation logic).

---

### TASK-04: BalanceEpsilon Elimination + Completed Void Restriction

- **Existing Source Paths & Classes:**
  - `src/Clovent.Restaurant/Orders/Order.cs`
  - `src/Clovent.Desktop/Restaurant/Orders/PosPaymentRules.cs`
  - `src/Clovent.Restaurant.Application/Payments/Commands/RecordPaymentCommand.cs`
  - `src/Clovent.Desktop/Restaurant/Orders/RestaurantPosForm.cs`
  - `src/Clovent.Restaurant/Orders/OrderStatus.cs`
- **Current Defect / Required Behavior:**
  - `PosPaymentRules.BalanceEpsilon = 0.005m` allows orders with a remaining half-cent balance to be marked settled.
  - `Order.Void()` allows orders with `Status == OrderStatus.Completed` to be voided, creating pseudo-refunds and corrupting ledger history.
  - Required behavior:
    1. **Eliminate Tolerance:** Remove `BalanceEpsilon`. Specify exact zero for normalized outstanding balance:
       $$\text{Normalized Outstanding Balance} == 0.00m$$
       Do NOT use `balance <= 0m` as a settlement check.
    2. **Tender Definitions:**
       - **Gross Cash Tender:** Physical currency presented by the customer.
       - **Change:** Cash returned to the customer:
         $$\text{Change} = \max(0.00m, \text{Gross Cash Tender} - \text{Remaining Order Balance})$$
       - **Applied Payment:** Net tender reducing order balance:
         $$\text{Applied Payment} = \text{Gross Cash Tender} - \text{Change}$$
         For non-cash tenders (Card, On Account), applied payment cannot exceed remaining balance.
    3. **Unexplained Overpayment / Negative Balance:** If applied tender exceeds the order balance such that normalized balance is negative ($\text{Balance} < 0.00m$), settlement must reject completion (throw domain violation / `UnexplainedOverpaymentException`). Negative balances cannot be silently treated as settled.
    4. **Unpaid Draft Cancellation:** Legitimate cancellation (`Order.Cancel()`) is permitted only if the order has **zero posted financial effects** (zero recorded payments, zero customer ledger entries, zero inventory transactions).
    5. **Voiding Restriction:** Do **not** permit voiding an order with posted financial effects merely because its status is not yet `Completed`. If an order is `Open` or `Held` but has one or more posted payments, `Order.Void()` must be rejected. Posted financial transactions must be reversed via formal compensating entries.
- **Intended Bounded Change:**
  - In `Order.cs`: in `Void()`, add check: `if (Status == OrderStatus.Completed || _paymentIds.Count > 0) throw RestaurantDomainException.OrderWithFinancialEffectsCannotBeVoided(Id);`.
  - In `PosPaymentRules.cs`: enforce exact zero balance: `IsSettled(decimal balance) => balance == 0.00m;`.
  - In `RecordPaymentCommand.cs`: enforce exact penny balance comparison and reject overpayment.
  - In `RestaurantPosForm.cs`: lock void button for completed orders or orders with posted payments.
- **Dependencies:** TASK-03.
- **Existing Tests to Extend & Regression Cases:**
  - Extend: `src/Clovent.Restaurant.Tests/Orders/OrderTests.cs`
  - Extend: `src/Clovent.Desktop.Tests/Restaurant/Orders/PosPaymentRulesTests.cs`
  - Regression cases:
    - Attempting to void a `Completed` order throws domain exception and fails.
    - Attempting to void an `Open` order with posted payments throws domain exception and fails.
    - Valid unpaid draft order with zero payments can be cancelled.
    - Order with balance of $0.01 or -$0.01 is rejected; order with balance of $0.00 is settled.
- **Acceptance Criteria:**
  - `Order.Void()` rejects orders with posted financial effects.
  - `BalanceEpsilon` eliminated; exact zero-balance settlement enforced.
- **Zero-Migration Compliance:** 100% compliant (domain logic and validation).

---

### TASK-05: Payment Idempotency + Credit-Limit Approval

- **Existing Source Paths & Classes:**
  - `src/Clovent.Restaurant/Payments/Payment.cs`
  - `src/Clovent.Restaurant.Infrastructure/Persistence/Configurations/PaymentConfiguration.cs`
  - `src/Clovent.Restaurant/Payments/IPaymentRepository.cs`
  - `src/Clovent.Restaurant.Application/Payments/Commands/RecordPaymentCommand.cs`
  - `src/Clovent.Desktop/Restaurant/Orders/RestaurantPosForm.cs`
  - `src/Clovent.Desktop/Restaurant/Shared/ManagerAuthorizationForm.cs`
  - `src/Clovent.Restaurant/Customers/Customer.cs`
  - `src/Clovent.Restaurant/Customers/CustomerLedgerEntry.cs`
- **Current Defect / Required Behavior:**
  - Rapid double-clicks or retransmissions can create duplicate payment records.
  - Manager elevation for credit-limit override can be bypassed if handled via an unverified caller-provided boolean or string.
  - Required behavior:
    1. **Payment Idempotency Protocol:**
       - Retain the existing `[Restaurant].[Payments].[IdempotencyKey]` column (NVARCHAR(200)) and unique filtered index `IX_Payments_IdempotencyKey`.
       - A preliminary repository lookup via `GetByIdempotencyKeyAsync` is an **optimization** to avoid redundant work, **not concurrency protection**.
       - True concurrency protection relies on the database unique index.
       - One attempt identity (`IdempotencyKey`) is generated once per intentional payment attempt and reused across retries.
       - If a retry or retransmission reuses an `IdempotencyKey` but differs in immutable payment details (`OrderId`, `PaymentMethodId`, `Amount`, `CustomerId`), the system must reject the request immediately (`IdempotencyKeyConflictException`).
       - Payment aggregate, customer-ledger entry, order state transition, and Outbox messages must be committed **atomically** in a single database transaction (`UnitOfWorkBehavior`).
       - **Unique-Conflict Recovery:** Under concurrent submissions of the same `IdempotencyKey`, the concurrent execution encountering a unique constraint violation (`DbUpdateException`) must catch the exception, reload the committed payment using a clean DbContext, verify that immutable details match, and return the committed original result (`PaymentDto.FromDomain(payment)`).
       - After an ambiguous timeout or retry, resubmission returns the committed original result without executing duplicate customer ledger postings, duplicate order completions, or duplicate Outbox messages.
    2. **Trusted Manager Approval Protocol:**
       - A caller-provided boolean, manager name, client DTO, or audit string is **not proof of authorization**.
       - The application layer must execute trusted validation of:
         a. Authenticated approving manager identity and required permission (`pos.exceedcreditlimit`) verified through `IManagerAuthorizationService`.
         b. Cashier session context (challenge occurred within active cashier's session).
         c. Specific challenged action (`pos.exceedcreditlimit`).
         d. Target `OrderId` and `CustomerId`.
         e. Exact payment amount and calculated credit-limit excess.
         f. Single-use consumption and short TTL (e.g. 60-second in-memory/cryptographic token) preventing replay across multiple payments.
       - The approval must be bound to the payment attempt (`IdempotencyKey`), allowing safe retries of that exact attempt while preventing reuse for any subsequent payment.
       - Raw manager credentials (passwords, PINs) must **never** be passed into persisted commands or logs.
       - Audit text in `CustomerLedgerEntry.Description` or `Order.Notes` is an evidentiary record, **never** the authorization source.
- **Intended Bounded Change:**
  - In `RecordPaymentCommand`: retain idempotency key; implement atomic unit-of-work commit; catch unique constraint violations and recover by returning the original committed payment.
  - In `RecordPaymentCommandHandler`: validate trusted manager authorization token; verify manager permission, order context, amount excess, and single-use status before applying credit sale.
- **Dependencies:** TASK-01, TASK-04.
- **Existing Tests to Extend & Regression Cases:**
  - Extend: `src/Clovent.Restaurant.Application.Tests/Payments/PaymentHandlerTests.cs`
  - Extend: `src/Clovent.Restaurant.Application.Tests/Payments/PaymentIdempotencyTests.cs`
  - Regression cases:
    - Simultaneous submissions with identical `IdempotencyKey` produce exactly one payment and return identical results.
    - Retry after successful commit returns original result without duplicate ledger entries or Outbox messages.
    - Transaction rollback on failure leaves zero orphan payment or ledger rows.
    - Separate intentional equal-value payments with distinct idempotency keys both succeed.
    - Submission with reused `IdempotencyKey` but different amount is rejected.
    - Credit sale exceeding credit limit without valid manager approval fails closed.
- **Precise Blocker & Zero-Migration Persistence Analysis:**
  - *Payment Idempotency Feasibility:* Supported by existing 1.2.2 schema (`IdempotencyKey` column, unique filtered index, entity property, and repository). Zero migrations required.
  - *Manager Approval Blocker / Scope Boundary:* Dedicated relational tables or entity foreign-key properties for manager approvals (e.g. `Payment.ManagerOverrideUserId` referencing `Users.Id`, or a standalone `[Restaurant].[ManagerApprovals]` table) **do not exist in the 1.2.2 baseline schema**. Introducing new database columns or foreign keys would violate the strict zero-migration constraint and is deferred to CBOS 1.3.0. For the CBOS 1.2.3 pilot, manager approvals must be validated in the application-layer pipeline and recorded in existing audit fields (`[Authentication].[LoginAttempts]` and description text). If database-level relational foreign-key approval persistence is required, this constitutes an **unresolved blocker requiring an authorized schema migration**.

---

### TASK-06: Business Day Close Aggregation

- **Existing Source Paths & Classes:**
  - `src/Clovent.Restaurant.Application/DayClose/Queries/GetBusinessDaySummaryQuery.cs`
  - `src/Clovent.Restaurant.Application/DayClose/Commands/CloseBusinessDayCommand.cs`
  - `src/Clovent.Restaurant.Application/DayClose/Dtos/BusinessDaySummaryDto.cs`
  - `src/Clovent.Desktop/Restaurant/EndOfDay/EndOfDayCloseDialog.cs`
  - `src/Clovent.Restaurant/DayClose/BusinessDayClose.cs`
- **Current Defect / Required Behavior:**
  - In `GetBusinessDaySummaryQueryHandler.cs`, Tax and Discounts are hardcoded to `0m, 0m, 0m` in the returned DTO.
  - Required behavior:
    1. Aggregate real Tax and real Discount totals from all completed orders in the business day period.
    2. Record `Refund = 0m` explicitly because refunds are disabled for the pilot, never using zero to conceal missing or unsupported data.
    3. Calculate Net Cash Sales including the cash portion of every supported split tender (Cash + Card, Cash + On Account) without double-counting collections or advances.
- **Intended Bounded Change:**
  - In `GetBusinessDaySummaryQueryHandler`: load completed orders for the operating period and compute:
    `decimal totalTax = orders.Sum(o => o.ExclusiveTaxAmount + o.InclusiveTaxAmount);`
    `decimal totalDiscounts = orders.Sum(o => o.DiscountAmount);`
  - Pass aggregated totals into `BusinessDaySummaryDto`.
  - Ensure shift drawer reconciliation terms match non-overlapping definitions.
- **Dependencies:** TASK-03, TASK-04.
- **Existing Tests to Extend & Regression Cases:**
  - Extend: `src/Clovent.Restaurant.Application.Tests/DayClose/BusinessDayCloseHandlerTests.cs`
  - Regression cases:
    - Day close summary correctly reflects non-zero tax and discount totals from orders.
    - Refund is reported as $0.00 with pilot explanation.
    - Cash portion of split tender is included in net cash sales without duplicating A/R collections.
- **Acceptance Criteria:**
  - Day close DTO returns exact non-zero sums for taxes and discounts.
  - Zero terms represent actual zero values, not placeholder concealment.
- **Zero-Migration Compliance:** 100% compliant (`BusinessDayClose` and DTO fields already exist).

---

### TASK-07: Outbox Processor Startup

- **Existing Source Paths & Classes:**
  - `src/Clovent.Desktop/Program.cs`
  - `src/Clovent.Platform/Bootstrap/ApplicationBootstrapper.cs`
  - `src/Clovent.Restaurant.Infrastructure/Outbox/OutboxProcessor.cs`
  - `src/Clovent.Restaurant.Application/Outbox/IOutboxProcessor.cs`
  - `src/Clovent.Desktop/Restaurant/OperationsHealthForm.cs`
- **Proposed Components:**
  - `[Proposed New Test File] src/Clovent.Desktop.Tests/Startup/OutboxStartupLifecycleTests.cs`
- **Current Defect / Required Behavior:**
  - `OutboxProcessor` is currently started on-demand when the operator opens `OperationsHealthForm` or enters the POS, rather than automatically at application boot.
  - Required behavior: Background outbox processing must begin automatically upon application startup as a hosted service or managed startup task, and shut down cleanly on application exit without requiring the operator to open Operations Health.
- **Intended Bounded Change:**
  - Register `OutboxProcessor` as an `IHostedService` in Microsoft.Extensions.Hosting pipeline in `Clovent.Desktop/Program.cs`.
  - Implement graceful cancellation token handling during desktop form close.
- **Dependencies:** None.
- **Existing Tests to Extend & Regression Cases:**
  - Add: `src/Clovent.Desktop.Tests/Startup/OutboxStartupLifecycleTests.cs` [Proposed]
  - Regression cases:
    - Outbox processor begins polling upon application host start.
    - Outbox processor gracefully stops without deadlocks or leaked threads when host shuts down.
- **Acceptance Criteria:**
  - Outbox processing starts automatically without opening Operations Health.
  - Clean shutdown with zero hung background threads.
- **Zero-Migration Compliance:** 100% compliant (host lifecycle registration).

---

### TASK-08: Continuity Replay + Receipt Snapshot Repair

- **Existing Source Paths & Classes:**
  - `src/Clovent.Restaurant.Application/Continuity/EmergencyJournalReplayer.cs`
  - `src/Clovent.Restaurant.Application/Outbox/Dtos/OutboxPayloads.cs`
  - `src/Clovent.Restaurant.Application/Outbox/Handlers/InventoryPostingOutboxHandler.cs`
  - `src/Clovent.Restaurant.Application/Outbox/Handlers/QuickBooksSyncOutboxHandler.cs`
  - `src/Clovent.Restaurant/Orders/Order.cs` (`ReceiptSnapshotJson`)
  - `src/Clovent.Restaurant/Continuity/EmergencyTransaction.cs`
  - `src/Clovent.Restaurant.Application.Tests/Outbox/FailureInjectionMatrixTests.cs`
- **Proposed Components:**
  - `[Proposed New Test File] src/Clovent.Restaurant.Application.Tests/Outbox/InventoryPostingOutboxHandlerTests.cs`
- **Current Defect / Required Behavior:**
  - Payloads generated by `EmergencyJournalReplayer.cs` for `InventoryPosting` use an anonymous object `{ OrderId, WarehouseId, Lines }` which mismatches `InventoryPostingPayload` (`Items` property containing `Sku`, `Name`, `Quantity`). This causes JSON deserialization failure in `InventoryPostingOutboxHandler`, sending replayed transactions to DeadLetter.
  - `EmergencyJournalReplayer.cs` omits sale-time SKU and product name snapshots needed for historical receipt reprints.
  - Required behavior:
    1. **Version-Aware Compatibility & Validation:** Outbox handlers must inspect message payload versions. Known valid legacy payload formats may be adapted deterministically.
    2. **Recoverable Failure on Missing Data:** If essential data (SKU, quantity, line items) is missing, the handler must produce a **visible recoverable failure** and preserve the original raw payload in DeadLetter for manual operator review. Handlers must **never invent financial values** or mark failed inventory postings as successful.
    3. **Receipt Snapshot Integrity:** At order completion (both normal completed orders and replayed continuity orders), capture immutable sale-time SKU, variant name, unit price, tax, and discount breakdown in `ReceiptSnapshotJson`. Historical snapshots must **never be overwritten** to conceal missing original data. If a historical receipt lacks specific fields, reprints must render missing fields as unavailable ("N/A") rather than retroactively synthesizing data.
- **Intended Bounded Change:**
  - In `EmergencyJournalReplayer.cs`: produce strongly typed `InventoryPostingPayload` with required line items.
  - In `Order.cs`: capture immutable sale-time snapshot in `ReceiptSnapshotJson` on normal completion and replay.
  - In Outbox handlers: enforce version-aware validation and preserve raw payload on dead-lettering.
- **Dependencies:** TASK-03, TASK-07.
- **Existing Tests to Extend & Regression Cases:**
  - Extend: `src/Clovent.Restaurant.Application.Tests/Continuity/EmergencyJournalReplayerTests.cs`
  - Extend: `src/Clovent.Restaurant.Application.Tests/Outbox/FailureInjectionMatrixTests.cs`
  - Add: `src/Clovent.Restaurant.Application.Tests/Outbox/InventoryPostingOutboxHandlerTests.cs` [Proposed]
  - Regression cases:
    - Offline emergency transaction replays cleanly into primary database.
    - Replayed outbox message successfully deserializes in `InventoryPostingOutboxHandler` without entering DeadLetter.
    - Normal completed-order receipts and replayed continuity receipts capture immutable sale-time snapshots.
    - Reprints of historical receipts render exact sale-time SKU and names without overwriting original snapshot data.
    - Payloads missing essential fields fail visibly to DeadLetter with raw payload preserved intact.
- **Acceptance Criteria:**
  - Zero DeadLetter messages caused by replay payload contract mismatch.
  - Sale-time snapshots captured immutably for normal and replayed orders.
- **Zero-Migration Compliance:** 100% compliant (`OutboxMessage.Payload` is an existing `nvarchar(max)` column).

---

### TASK-09: DB Config Precedence + Atomic Critical Config Writes

- **Existing Source Paths & Classes:**
  - `src/Clovent.Desktop/Configuration/DatabaseSecretStore.cs`
  - `src/Clovent.Desktop/Commissioning/Services/CommissioningStateService.cs`
  - `src/Clovent.Desktop/Configuration/DatabaseConnectionSettings.cs`
  - `src/Clovent.Desktop/Program.cs`
- **Proposed Components:**
  - `[Proposed New Class] src/Clovent.Desktop/Configuration/AtomicFileWriter.cs`
  - `[Proposed New Test File] src/Clovent.Desktop.Tests/Configuration/AtomicConfigurationWriterTests.cs`
- **Current Defect / Required Behavior:**
  - `DatabaseSecretStore.GetConfigFilePath()` checks user-level config in `%LocalAppData%` *first*, allowing stale user settings to override commissioned `%ProgramData%` machine settings.
  - `DatabaseSecretStore.Save()` uses direct `File.WriteAllText()`, risking corruption or 0-byte truncation if the process or power terminates during write.
  - Required behavior:
    1. Commissioned `%ProgramData%\Clovent\BusinessOperatingSystem\Config\database.config.json` takes strict precedence. `%LocalAppData%` is checked only as a fallback for controlled development/test scenarios.
    2. Implement atomic file writes for Tier-1 critical configuration: write to temporary file (`.tmp`), flush to disk, validate JSON structure, and atomically replace the target file (`File.Replace` / atomic move).
- **Intended Bounded Change:**
  - In `DatabaseSecretStore.GetConfigFilePath()`: check `machinePath` first; if present, return `machinePath`.
  - In `DatabaseSecretStore.Save()`: implement atomic replacement routine `AtomicFileWriter.WriteJsonAtomic(path, toSave, JsonOptions)`.
- **Dependencies:** TASK-01.
- **Existing Tests to Extend & Regression Cases:**
  - Extend: `src/Clovent.Desktop.Tests/Configuration/DatabaseConnectionSettingsTests.cs`
  - Add: `src/Clovent.Desktop.Tests/Configuration/AtomicConfigurationWriterTests.cs` [Proposed]
  - Regression cases:
    - When both ProgramData and LocalAppData configs exist, ProgramData config is loaded.
    - Simulated process interruption during write leaves existing valid configuration intact.
- **Acceptance Criteria:**
  - ProgramData machine config takes strict precedence.
  - Critical configuration writes are fully atomic and corruption-safe.
- **Zero-Migration Compliance:** 100% compliant (filesystem configuration management).

---

### TASK-10: Authenticode / Release Verification Pipeline

- **Existing Source Paths & Classes:**
  - `tools/ReleaseGuard/ScanReleasePackage.ps1`
  - `installer/Clovent.BusinessOperatingSystem.iss`
  - `Tools/Clovent.Installer.Provisioner/Clovent.Installer.Provisioner.csproj`
  - `src/Clovent.Desktop/Clovent.Desktop.csproj`
  - `docs/release/release-checklist.md`
- **Current Defect / Required Behavior:**
  - Code signing documentation and scripts did not explicitly enumerate the first-party single-file database provisioner (`Clovent.Installer.Provisioner.exe`) in the signing scope alongside the desktop executable and installer.
  - Required behavior:
    1. Explicitly include `Clovent.Installer.Provisioner.exe`, `Clovent.Desktop.exe`, approved first-party DLLs (`Clovent.*.dll`), and final installer in the Authenticode signing scope.
    2. Strictly preserve third-party binaries and vendor digital signatures (DevExpress, Microsoft, SQLite).
    3. Maintain signing credentials in external protected key storage.
    4. Compute and verify SHA-256 integrity hash on the exact signed installer intended for distribution.
- **Intended Bounded Change:**
  - Update `installer/Clovent.BusinessOperatingSystem.iss` and release scripts to sign `Clovent.Installer.Provisioner.exe` before bundling.
  - Ensure `ScanReleasePackage.ps1` verifies the signature of first-party provisioner.
- **Dependencies:** All previous tasks (TASK-01 through TASK-09).
- **Existing Tests to Extend & Regression Cases:**
  - Run `tools/ReleaseGuard/ScanReleasePackage.ps1` against published release directory and extracted installer payload.
  - Assert 0 violations and valid digital signatures on all first-party binaries.
- **Acceptance Criteria:**
  - All first-party executables and DLLs are signed with RFC 3161 timestamps.
  - Third-party signatures remain intact.
  - Release package passes ReleaseGuard scan with exit code 0.
- **Zero-Migration Compliance:** 100% compliant (packaging and signing pipeline).

---

## 3. Identified Commands for Validating Implementation Changes

Identified commands for testing and validating implementation changes once authorized:

```powershell
# 1. Clean Build in Debug and Release Configurations
dotnet build Clovent.BusinessOperatingSystem.slnx -c Debug
dotnet build Clovent.BusinessOperatingSystem.slnx -c Release

# 2. Targeted Test Execution by Task
dotnet test src/Clovent.Desktop.Tests/Clovent.Desktop.Tests.csproj --filter "FullyQualifiedName~SecurityAndLicensingHardeningTests"
dotnet test src/Clovent.Desktop.Tests/Clovent.Desktop.Tests.csproj --filter "FullyQualifiedName~ManagerAuthorizationFormTests"
dotnet test src/Clovent.Restaurant.Tests/Clovent.Restaurant.Tests.csproj --filter "FullyQualifiedName~OrderTests"
dotnet test src/Clovent.Restaurant.Application.Tests/Clovent.Restaurant.Application.Tests.csproj --filter "FullyQualifiedName~PaymentHandlerTests|FullyQualifiedName~PaymentIdempotencyTests"
dotnet test src/Clovent.Restaurant.Application.Tests/Clovent.Restaurant.Application.Tests.csproj --filter "FullyQualifiedName~BusinessDayCloseHandlerTests"
dotnet test src/Clovent.Restaurant.Application.Tests/Clovent.Restaurant.Application.Tests.csproj --filter "FullyQualifiedName~EmergencyJournalReplayerTests"
dotnet test src/Clovent.Desktop.Tests/Clovent.Desktop.Tests.csproj --filter "FullyQualifiedName~DatabaseConnectionSettingsTests"

# 3. Full Solution Regression Execution
dotnet test Clovent.BusinessOperatingSystem.slnx -c Debug

# 4. Self-Contained Release Publish
dotnet publish src/Clovent.Desktop/Clovent.Desktop.csproj -c Release -r win-x64 --self-contained true -o artifacts/release/Clovent.BusinessOperatingSystem-win-x64

# 5. Automated Security & Release Hygiene Scan
powershell -ExecutionPolicy Bypass -File tools/ReleaseGuard/ScanReleasePackage.ps1 -ReleaseDir artifacts/release/Clovent.BusinessOperatingSystem-win-x64
```

---

## 4. Implementation Readiness Sign-Off

**Status:** **DRAFT — PENDING TECHNICAL SIGN-OFF**

The ten canonical hardening tasks are defined and mapped against actual codebase source files. Implementation is strictly not launched within this governance closure phase. Development will proceed in authorized feature worktrees following the multi-stream ownership model under the zero database migration constraint once formal technical sign-off is granted.
