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

## 3. Absolute Workstation Test Isolation & Provider Scope

- **SQLite Provider Scope:** SQLite in-memory (`Microsoft.EntityFrameworkCore.Sqlite` with `DataSource=:memory:`) is an available fast-test provider for isolated unit and integration testing, but is not mandatory for all tests.
- **SQL Server Test Isolation:** Tests requiring Microsoft SQL Server must execute against explicitly isolated, disposable databases or throwaway test environments.
- **Zero Pollution of Operational Databases & Configuration:** Tests must **never** touch customer or developer operational databases, host machine operational configurations, `%ProgramData%\Clovent`, `%LocalAppData%\Clovent`, or registry keys.
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

## 5. Real SQL Server Validation Triggers & Concurrency Criteria

Automated in-memory tests provide rapid feedback but do not prove SQL Server compatibility. Real SQL Server validation is strictly required when:
1. Adding or altering EF Core migrations (`[__EFMigrationsHistory]`, `Up`/`Down` scripts). Note: CBOS 1.2.3 requires zero schema migrations.
2. Changing column types, precision, nullability, or indexes.
3. Modifying raw SQL queries, sequence generators, or concurrency tokens (`RowVersion` in CBOS 1.3.0).
4. Qualifying a candidate build for release packaging.
5. **Concurrency Acceptance Criteria:** Concurrency evaluation must demonstrate bounded, observable concurrency acceptance criteria, safe retry/failure behavior, and financial integrity under multi-threaded load (rather than unrealistic zero-deadlock promises).

---

## 6. Truthful Runtime Evidence Vocabulary (Strict)

Accuracy in verification reporting is mandatory. All agents and engineers must use the following standard evidence vocabulary:

| Standard Vocabulary Term | Meaning & Evidence Standard |
|---|---|
| **`STATIC SOURCE CONFIRMED`** | Code was inspected structurally via AST, grep, or file viewing. |
| **`AUTOMATED TEST VALIDATED`** | Passing execution of automated unit/integration tests in a test runner. |
| **`REAL SQL SERVER VALIDATED`** | Executed against an explicitly isolated Microsoft SQL Server instance (not in-memory SQLite). |
| **`LIVE UI EXECUTED`** | The actual Windows WinForms executable was launched and interactively exercised on a physical or virtual display. |
| **`LIVE UI NOT EXECUTED`** | Interactive execution on a physical/virtual display was not performed. **This terminology must NOT imply that headless validation occurred.** |
| **`VISUAL STUDIO DESIGNER UI EXECUTED`** | Form was interactively opened and edited inside the Visual Studio Designer. |
| **`VISUAL STUDIO DESIGNER UI NOT EXECUTED`**| Form was not opened inside the interactive Visual Studio Designer. **Does not imply design-time execution or headless validation.** |
| **`CLEAN MACHINE ACCEPTED`** | Verified inside a fresh, pristine Windows Sandbox or clean VM without development SDKs. **CBOS 1.2.2 clean-machine acceptance is marked as PENDING unless actual acceptance evidence is supplied; a frozen baseline designation is not a passed gate.** |
| **`EXTERNAL INTEGRATION VALIDATED`** | Validated against live external endpoints (not simulated/in-memory test fakes). |
| **`TARGET ONLY`** | Design goal or roadmap milestone not yet validated against running software. |
| **`SYNTHETIC BENCHMARK`** | Performance measured in an isolated micro-benchmark harness. |
| **`END-TO-END MEASUREMENT`** | Performance measured during realistic operational workflow execution. |
