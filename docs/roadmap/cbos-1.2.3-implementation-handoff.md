# CBOS 1.2.3 Implementation Handoff & Hardening Specification

| Attribute | Details |
| :--- | :--- |
| **Milestone Target** | CBOS 1.2.3 — Controlled Attended Single-Terminal Pilot |
| **Preceding Baseline** | CBOS 1.2.2 Frozen Internal Acceptance Baseline (`d9d38cbb17828fe771568ca5ab6abb036fe8798b`) |
| **Database Migration Invariant** | **STRICT ZERO DATABASE MIGRATIONS** |
| **Audience** | Implementation Leads, Engineering Agents, QA Engineers |
| **Status** | **APPROVED IMPLEMENTATION HANDOFF** |
| **Classification** | **PUBLIC-SAFE** |

---

## 1. Executive Summary & Architectural Invariants

This handoff document defines the complete, concrete engineering specification for the ten canonical hardening tasks required to deliver **CBOS 1.2.3** as a controlled, attended, single-terminal commercial pilot release.

### Core Architectural Invariants:
1. **Zero Database Migrations:** No schema modifications, no EF Core migrations, and no schema snapshots may be introduced in CBOS 1.2.3. All persistence guarantees (idempotency, outbox messages, day close aggregation, and snapshot storage) must leverage existing database tables and columns created in the 1.2.2 baseline. Aggregate concurrency tokens (`RowVersion`) belong strictly to CBOS 1.3.0.
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
   - **Implementation Launch Invariant:** Do not launch implementation within governance closure. Workstreams remain ready for subsequent authorized branch work.

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
  - `ManagerAuthorizationForm.PerformAuthorizeAsync()` has a fallback: if `_authService == null`, it sets `DialogResult = DialogResult.OK` and returns `true`, failing open when authorization services are missing or unconfigured.
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
- **Current Defect / Required Behavior:**
  - PIN sign-in resolves credentials by matching PBKDF2 hash against database rows. Unmatched PIN submissions do not associate with a user account, bypassing per-user failed-attempt counters and allowing unlimited brute-force attempts.
  - Source-confirmed seed behavior: `DevelopmentUserSeedStartupTask.cs` matches on username `"admin"` or email `"admin@clovent.local"` and, when `Desktop:SeedDevelopmentUser` is enabled, overwrites the account's password hash if it does not verify against the built-in development password constant. In production or commissioned environments, running this task risks overwriting an administrator's real credentials.
  - Required behavior: Introduce terminal-level in-memory rate limiting and progressive delays (e.g. after 3 invalid PINs, introduce a 2-second delay; after 5, lock PIN input for 30 seconds). Protect seed startup tasks so they are strictly disabled in production builds and configuration flags cannot reset existing credentials.
- **Intended Bounded Change:**
  - Implement a terminal-level rate limiter service (`ITerminalPinThrottleService`) in Desktop and wire into `LoginForm.cs` / `LoginService.cs`.
  - Modify `DevelopmentUserSeedStartupTask.cs`: if an existing user is found, never alter their password; only seed if no user exists and environment is explicitly non-production (`#if DEBUG` or explicit development opt-in).
- **Dependencies:** TASK-01.
- **Existing Tests to Extend & Regression Cases:**
  - Extend: `src/Clovent.Desktop.Tests/Login/LoginServiceTests.cs`
  - Regression cases:
    - Rapid invalid PIN submissions trigger terminal-level delay and lockout.
    - Valid PIN resets the failure counter upon successful login.
    - Existing administrator credentials are not overwritten by seed tasks when `SeedDevelopmentUser` is enabled.
- **Acceptance Criteria:**
  - Unmatched PIN submissions trigger progressive delay and lockout.
  - Seed tasks never overwrite existing credentials.
- **Zero-Migration Compliance:** 100% compliant (in-memory rate limiting and C# startup task gating).

---

### TASK-03: Financial Rounding Contract

- **Existing Source Paths & Classes:**
  - `src/Clovent.Restaurant/Pricing/` or `src/Clovent.Restaurant/DomainServices/`
  - `src/Clovent.Restaurant/Orders/Order.cs`
  - `src/Clovent.Restaurant/Orders/OrderLine.cs`
  - `src/Clovent.Restaurant.Application/Orders/OrderTotalsCalculator.cs`
  - `src/Clovent.Desktop/Restaurant/Orders/RestaurantPosForm.cs`
  - `src/Clovent.Restaurant.Application/Continuity/EmergencyJournalReplayer.cs`
- **Current Defect / Required Behavior:**
  - Financial calculations lack a single centralized rounding policy. Sporadic ad-hoc rounding occurs across components.
  - Required behavior: Implement a centralized domain service `MoneyRoundingPolicy` using `MidpointRounding.AwayFromZero` as the selected pilot midpoint direction.
  - Formally complete calculation contracts:
    1. Define exact rounding boundaries (intermediate precision preserved; rounded to 2 decimals at contract boundaries).
    2. Tax and discount execution sequence (line discounts -> exclusive/inclusive taxes -> order discounts -> service charges).
    3. Apportionment and allocation algorithms with residual cent handling across line items (largest-remainder method).
    4. Mathematical consistency across online sales, Continuity/replay, receipts, payments, and Day Close.
- **Intended Bounded Change:**
  - Introduce `MoneyRoundingPolicy` in `Clovent.Restaurant`.
  - Refactor `OrderTotalsCalculator` and line item calculation paths to invoke `MoneyRoundingPolicy`.
- **Dependencies:** None.
- **Existing Tests to Extend & Regression Cases:**
  - Extend: `src/Clovent.Restaurant.Tests/Orders/`
  - Add parameterized unit tests:
    - Apportioning an odd discount ($1.00 across 3 items at $10.00 each) produces lines summing to exactly $29.00 without floating cent loss.
    - Inclusive and exclusive tax calculations match receipt totals to the penny.
    - `AwayFromZero` midpoint rounding produces deterministic results at .005 boundaries.
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
    1. Eliminate `BalanceEpsilon`; require exact 2-decimal zero balance ($\text{Balance} == 0.00m$) for completion.
    2. Disallow `Order.Void()` when `Status == OrderStatus.Completed` (throw `RestaurantDomainException.CompletedOrderCannotBeVoided`).
    3. Preserve legitimate unpaid draft cancellation (`Order.Cancel()` before payments).
    4. Disable and lock "Void" UI buttons in `RestaurantPosForm` for completed orders.
- **Intended Bounded Change:**
  - In `Order.cs`: in `Void()`, add check: `if (Status == OrderStatus.Completed) throw RestaurantDomainException.CompletedOrderCannotBeVoided(Id);`.
  - In `PosPaymentRules.cs`: update `IsSettled(decimal balance) => balance <= 0m;`.
  - In `RecordPaymentCommand.cs`: enforce exact penny balance comparison.
  - In `RestaurantPosForm.cs`: lock void button for completed orders.
- **Dependencies:** TASK-03.
- **Existing Tests to Extend & Regression Cases:**
  - Extend: `src/Clovent.Restaurant.Tests/Orders/OrderTests.cs`
  - Extend: `src/Clovent.Desktop.Tests/Restaurant/Orders/PosPaymentRulesTests.cs`
  - Regression cases:
    - Attempting to void a `Completed` order throws domain exception and fails.
    - Valid unpaid draft order can be cancelled before payment.
    - Order with balance of $0.01 is not settled; order with balance of $0.00 is settled.
- **Acceptance Criteria:**
  - `Order.Void()` rejects `Completed` orders.
  - `BalanceEpsilon` eliminated; zero-balance settlement enforced.
- **Zero-Migration Compliance:** 100% compliant (domain logic and validation).

---

### TASK-05: Payment Idempotency + Credit-Limit Approval

- **Existing Source Paths & Classes:**
  - `src/Clovent.Restaurant/Payments/Payment.cs`
  - `src/Clovent.Restaurant.Application/Payments/Commands/RecordPaymentCommand.cs`
  - `src/Clovent.Desktop/Restaurant/Orders/RestaurantPosForm.cs`
  - `src/Clovent.Desktop/Restaurant/Shared/ManagerAuthorizationForm.cs`
  - `src/Clovent.Restaurant/Customers/Customer.cs`
- **Current Defect / Required Behavior:**
  - Rapid double-clicks or retransmissions can create duplicate payment records.
  - Manager elevation for credit-limit override can be bypassed if handled via a simple unverified boolean parameter.
  - Required behavior:
    1. Enforce exact payment identity: generate `PaymentAttemptId` (mapped to `IdempotencyKey`) once per intentional attempt; reuse across retries; reject reuse if payment details (amount, method, order) conflict.
    2. Managerial credit-limit override requires structured manager credentials verified through `IManagerAuthorizationService`, producing an immutable authorization audit record rather than accepting a bare boolean.
- **Intended Bounded Change:**
  - In `RecordPaymentCommand`: query existing payment by `IdempotencyKey` via `IPaymentRepository.GetByIdempotencyKeyAsync`. If details match, return existing payment (idempotent success); if details conflict, reject with error.
  - In `RestaurantPosForm.cs`: pass verified manager authorization result into `RecordPaymentCommand`.
  - In `CustomerLedgerEntry`: record manager override audit string in `Description` (`$"On Account Sale ({order.OrderNumber}) [Approved by Manager: {managerName}]"`).
- **Dependencies:** TASK-01, TASK-04.
- **Existing Tests to Extend & Regression Cases:**
  - Extend: `src/Clovent.Restaurant.Application.Tests/Payments/RecordPaymentCommandTests.cs`
  - Regression cases:
    - Double submission with identical `IdempotencyKey` returns identical result without creating a duplicate record.
    - Submission with reused `IdempotencyKey` but different amount is rejected.
    - Credit sale exceeding credit limit fails without manager authorization.
    - Credit-limit approval cannot be forged by manipulating a client-side boolean.
- **Acceptance Criteria:**
  - Double payment submission is strictly idempotent.
  - Manager approval for credit-limit override is fail-closed and validated.
- **Zero-Migration Compliance & Persistence Proof:**
  - **Payment Idempotency (Proven Feasibility):**
    - Database column `[Restaurant].[Payments].[IdempotencyKey]` (NVARCHAR(200)) already exists in 1.2.2 schema with unique filtered index `IX_Payments_IdempotencyKey` (`WHERE [IdempotencyKey] IS NOT NULL`).
    - Aggregate property `Payment.IdempotencyKey` and repository method `IPaymentRepository.GetByIdempotencyKeyAsync` already exist and are wired.
    - Zero migrations required.
  - **Verifiable Manager Approval (Proven Feasibility vs. Open Issue):**
    - *Proven Feasibility:* Authentication attempts are durably persisted in `[Authentication].[LoginAttempts]` by `ManagerAuthorizationService`. Transaction-level audit is persisted in existing unmigrated columns: `CustomerLedgerEntry.Description` (NVARCHAR(MAX)) and `Order.Notes` (NVARCHAR(1000)).
    - *Unproven Assumption Refuted:* No dedicated relational approval table (e.g. `[Restaurant].[ManagerApprovals]`) or entity foreign-key property (e.g. `Payment.ManagerOverrideUserId`) exists in the 1.2.2 baseline schema.
    - *Recorded Open Issue / Boundary:* Introducing dedicated relational approval columns requires schema modifications, which violates the strict zero-migration constraint and is deferred to CBOS 1.3.0. For CBOS 1.2.3 pilot, manager approvals are enforced in the application pipeline and logged in existing audit text fields.

---

### TASK-06: Business Day Close Aggregation

- **Existing Source Paths & Classes:**
  - `src/Clovent.Restaurant.Application/DayClose/Queries/GetBusinessDaySummaryQuery.cs`
  - `src/Clovent.Restaurant.Application/DayClose/Commands/CloseBusinessDayCommand.cs`
  - `src/Clovent.Restaurant.Application/DayClose/Dtos/BusinessDaySummaryDto.cs`
  - `src/Clovent.Desktop/Restaurant/EndOfDay/EndOfDayCloseDialog.cs`
  - `src/Clovent.Restaurant/DayClose/BusinessDayClose.cs`
- **Current Defect / Required Behavior:**
  - In `GetBusinessDaySummaryQueryHandler.cs`, Tax and Discounts are hardcoded to `0m, 0m, 0m` in the returned DTO!
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
  - `src/Clovent.Desktop/Startup/ApplicationBootstrapper.cs`
  - `src/Clovent.Restaurant.Infrastructure/Outbox/OutboxProcessor.cs`
  - `src/Clovent.Restaurant.Application/Outbox/IOutboxProcessor.cs`
  - `src/Clovent.Desktop/Restaurant/OperationsHealthForm.cs`
- **Current Defect / Required Behavior:**
  - `OutboxProcessor` is currently started on-demand when the operator opens `OperationsHealthForm` or enters the POS, rather than automatically at application boot.
  - Required behavior: Background outbox processing must begin automatically upon application startup as a hosted service or managed startup task, and shut down cleanly on application exit without requiring the operator to open Operations Health.
- **Intended Bounded Change:**
  - Register `OutboxProcessor` as an `IHostedService` in Microsoft.Extensions.Hosting pipeline in `Clovent.Desktop/Program.cs`.
  - Implement graceful cancellation token handling during desktop form close.
- **Dependencies:** None.
- **Existing Tests to Extend & Regression Cases:**
  - Add: `src/Clovent.Desktop.Tests/Startup/OutboxStartupLifecycleTests.cs`
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
- **Current Defect / Required Behavior:**
  - Payloads generated by `EmergencyJournalReplayer.cs` for `InventoryPosting` use an anonymous object `{ OrderId, WarehouseId, Lines }` which mismatches `InventoryPostingPayload` (`Items` property containing `Sku`, `Name`, `Quantity`). This causes JSON deserialization failure in `InventoryPostingOutboxHandler`, sending replayed transactions to DeadLetter!
  - `EmergencyJournalReplayer.cs` omits sale-time SKU and product name snapshots needed for historical receipt reprints.
  - Required behavior:
    1. Align all outbox payloads produced during replay (`InventoryPosting`, `QuickBooksSync`, `CloudSync`, `AnalyticsEvent`) with their respective strongly typed DTO definitions in `OutboxPayloads.cs`.
    2. Include SKU, Variant Name, and snapshot line details so inventory posting succeeds without DeadLetter.
    3. Ensure backward compatibility: deserializers must tolerate legacy or partial payloads without throwing.
    4. Capture and preserve immutable sale-time SKU and product names in `ReceiptSnapshotJson` to support historical reprints.
- **Intended Bounded Change:**
  - In `EmergencyJournalReplayer.cs`: use strongly typed `InventoryPostingPayload` and `InventoryPostingLineItem` when generating Outbox messages.
  - In `Order.cs`: ensure receipt snapshots capture exact sale-time names and SKUs.
- **Dependencies:** TASK-03, TASK-07.
- **Existing Tests to Extend & Regression Cases:**
  - Extend: `src/Clovent.Restaurant.Application.Tests/Continuity/EmergencyJournalReplayerTests.cs`
  - Extend: `src/Clovent.Restaurant.Application.Tests/Outbox/InventoryPostingOutboxHandlerTests.cs`
  - Regression cases:
    - Offline emergency transaction replays cleanly into primary database.
    - Replayed outbox message successfully deserializes in `InventoryPostingOutboxHandler` without entering DeadLetter.
    - Reprints of historical replayed receipts render exact sale-time SKU and names.
- **Acceptance Criteria:**
  - Zero DeadLetter messages caused by replay payload contract mismatch.
  - Historical receipt reprints use captured sale-time snapshot data.
- **Zero-Migration Compliance:** 100% compliant (`OutboxMessage.Payload` is an existing `nvarchar(max)` column).

---

### TASK-09: DB Config Precedence + Atomic Critical Config Writes

- **Existing Source Paths & Classes:**
  - `src/Clovent.Desktop/Configuration/DatabaseSecretStore.cs`
  - `src/Clovent.Desktop/Commissioning/Services/CommissioningStateService.cs`
  - `src/Clovent.Desktop/Configuration/DatabaseConnectionSettings.cs`
  - `src/Clovent.Desktop/Program.cs`
- **Current Defect / Required Behavior:**
  - `DatabaseSecretStore.GetConfigFilePath()` checks user-level config in `%LocalAppData%` *first*, allowing stale user settings to override commissioned `%ProgramData%` machine settings!
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
  - Add: `src/Clovent.Desktop.Tests/Configuration/AtomicConfigurationWriterTests.cs`
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

## 3. Implementation Verification & Build Commands

Certified commands for validating implementation changes:

```powershell
# 1. Clean Build in Debug and Release Configurations
dotnet build Clovent.BusinessOperatingSystem.slnx -c Debug
dotnet build Clovent.BusinessOperatingSystem.slnx -c Release

# 2. Targeted Test Execution by Task
dotnet test src/Clovent.Desktop.Tests/Clovent.Desktop.Tests.csproj --filter "FullyQualifiedName~SecurityAndLicensingHardeningTests"
dotnet test src/Clovent.Desktop.Tests/Clovent.Desktop.Tests.csproj --filter "FullyQualifiedName~ManagerAuthorizationFormTests"
dotnet test src/Clovent.Restaurant.Tests/Clovent.Restaurant.Tests.csproj --filter "FullyQualifiedName~OrderTests"
dotnet test src/Clovent.Restaurant.Application.Tests/Clovent.Restaurant.Application.Tests.csproj --filter "FullyQualifiedName~RecordPaymentCommandTests"
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

The ten canonical hardening tasks are fully mapped, decoupled, bounded, and verified against actual codebase source files. Implementation is strictly not launched within this governance closure phase. Development will proceed in authorized feature worktrees following the multi-stream ownership model with zero database migrations.
