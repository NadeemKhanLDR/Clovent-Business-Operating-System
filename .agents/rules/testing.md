# Testing & Quality Verification Rules

**Scope:** `src/**/*.Tests/**`, QA, test execution, verification reporting  
**Authoritative Reference:** [AGENTS.md](../../AGENTS.md)

---

## 1. Test Engineering Workflow

Every substantial implementation or bug-fixing task must follow this eight-step engineering pipeline:
1. **Investigate Existing Behavior:** Inspect relevant code, database configurations, and existing tests.
2. **Identify Root Cause:** Document the precise mechanism causing the bug or failure.
3. **Implement Targeted Fix:** Make minimal, clean modifications adhering to bounded-context architecture and WinForms/High-DPI rules.
4. **Compile Debug Build:** Run `dotnet build Clovent.BusinessOperatingSystem.slnx -c Debug` and confirm 0 errors.
5. **Run Targeted Tests:** Execute tests directly covering the altered component.
6. **Run Regression Tests:** Run broader test suites for related bounded contexts.
7. **Update Documentation:** Update authoritative guides in `docs/` if business logic or deployment behavior changed.
8. **Deliver Structured Final Report:** Conclude with the full technical report.

---

## 2. Test Execution Commands & Strategy

Do not run the entire solution's test suites for minor, localized changes. Use progressive testing:

### A. Targeted Feature Tests First
Run tests matching the specific namespace or class:
```powershell
dotnet test src\Clovent.Desktop.Tests\Clovent.Desktop.Tests.csproj --filter "FullyQualifiedName~<FeatureName>"
```
Examples:
- `dotnet test src\Clovent.Desktop.Tests\Clovent.Desktop.Tests.csproj --filter "FullyQualifiedName~BusinessFormatters"`
- `dotnet test src\Clovent.Desktop.Tests\Clovent.Desktop.Tests.csproj --filter "FullyQualifiedName~EndOfDay"`
- `dotnet test src\Clovent.Restaurant.Application.Tests\Clovent.Restaurant.Application.Tests.csproj`

### B. Affected Regression Suites Second
Execute the full test project of the affected module (e.g. `Clovent.Desktop.Tests`, `Clovent.Inventory.Tests`).

### C. Full Test Verification for Milestone/Major Changes
Execute full test passes when coordinating cross-cutting architectural changes across multiple contexts.

---

## 3. Absolute Workstation Test Isolation

- **In-Memory SQLite Isolation:** Unit and integration tests requiring relational persistence must use SQLite in-memory (`Microsoft.EntityFrameworkCore.Sqlite` with `DataSource=:memory:`).
- **Zero Workstation State Pollution:** Automated tests must never read from or write to host machine `%ProgramData%\Clovent`, `%LocalAppData%\Clovent`, the registry, or developer machine SQL Server instances.
- **Temporary Test Directories:** Tests verifying file operations, logs, or continuity journals must create and clean up isolated temporary directories (`Path.GetTempPath()`).
- **Test Fakes & Harnesses:** Use test fakes for application handler verification.

---

## 4. Change-to-Test Mapping

Every code modification must have direct, verifiable test coverage:
- **Domain & Application Logic:** Covered by unit tests asserting state changes, domain events, and boundary exceptions.
- **Financial Calculations:** Every change to money math, rounding, discounts, tax, or day close must include parameterized invariant tests asserting penny-exact results.
- **EF Core Configurations:** Tested via entity mapping and round-trip persistence tests.
- **Defect Remediation:** Every bug fix must introduce a regression test recreating the failure condition before proving the fix.

---

## 5. Real SQL Server Validation Triggers

Automated in-memory tests provide rapid feedback but do not prove SQL Server compatibility. Real SQL Server validation is strictly required when:
1. Adding or altering EF Core migrations (`[__EFMigrationsHistory]`, `Up`/`Down` scripts).
2. Changing column types, precision, nullability, or indexes.
3. Modifying raw SQL queries, sequence generators, or concurrency tokens (`RowVersion`).
4. Qualifying a candidate build for release packaging.

---

## 6. Truthful Runtime Evidence Vocabulary (Strict)

Accuracy in verification reporting is mandatory. All agents and engineers must use the following standard evidence vocabulary:

| Standard Vocabulary Term | Meaning & Evidence Standard |
|---|---|
| **`STATIC SOURCE CONFIRMED`** | Code was inspected structurally via AST, grep, or file viewing. |
| **`AUTOMATED TEST VALIDATED`** | Passing execution of automated unit/integration tests in a test runner. |
| **`REAL SQL SERVER VALIDATED`** | Executed against an actual Microsoft SQL Server instance (not in-memory SQLite). |
| **`LIVE UI EXECUTED`** | The actual Windows WinForms executable was launched and interactively exercised on a physical or virtual display. |
| **`LIVE UI NOT EXECUTED`** | The UI was validated headlessly via unit tests, reflection, `DrawToBitmap`, or layout math without live interactive execution. |
| **`VISUAL STUDIO DESIGNER UI EXECUTED`** | Form was interactively opened and edited inside the Visual Studio Designer. |
| **`VISUAL STUDIO DESIGNER UI NOT EXECUTED`**| Form was verified structurally or via compile tests without opening VS Designer. |
| **`CLEAN MACHINE ACCEPTED`** | Verified inside a fresh, pristine Windows Sandbox or VM without development SDKs. |
| **`EXTERNAL INTEGRATION VALIDATED`** | Validated against live external endpoints (not simulated/in-memory test fakes). |
| **`TARGET ONLY`** | Design goal or roadmap milestone not yet validated against running software. |
| **`SYNTHETIC BENCHMARK`** | Performance measured in an isolated micro-benchmark harness. |
| **`END-TO-END MEASUREMENT`** | Performance measured during realistic operational workflow execution. |
