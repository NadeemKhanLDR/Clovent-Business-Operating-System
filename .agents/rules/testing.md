# Testing & Quality Verification Rules

**Scope:** `src/**/*.Tests/**`, QA, test execution, verification reporting  
**Authoritative Reference:** [AGENTS.md](file:///d:/Clovent%20Business%20Operating%20System/AGENTS.md)

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

## 3. Test Fixtures & In-Memory Isolation

- **Isolated SQLite In-Memory Database:** Integration tests requiring relational persistence use `Microsoft.EntityFrameworkCore.Sqlite` with `DataSource=:memory:`.
- **Zero Real Database Mutation:** Automated unit/integration tests must never connect to or mutate the production SQL Server database (`Clovent_BusinessOperatingSystem`).
- **Fake Repositories & MediatR Handlers:** Use test support fakes (`FakeRecommendationRuleRepository`, test mediator harnesses) for application handler verification.

---

## 4. Verification Claims & Terminology (Strict)

Accuracy in verification reporting is paramount. The following distinctions are strictly enforced:

### UI Verification Claims:
- **`LIVE UI EXECUTED`:** State this **only** if the actual Windows executable was interactively launched and visually/interactively exercised by an operator or GUI automation tool on a real display surface.
- **`LIVE UI NOT EXECUTED`:** State this whenever work was validated via unit tests, headless test runners, reflection, `DrawToBitmap`, form constructor instantiation, or layout calculation tests.
  - *Never* describe constructor instantiation, reflection, or automated test runners as "live UI testing".

### Visual Studio Designer Claims:
- **`VISUAL STUDIO DESIGNER UI EXECUTED`:** State this **only** if the form was opened and viewed directly within an active Visual Studio Designer host.
- **`VISUAL STUDIO DESIGNER NOT EXECUTED`:** State this if Designer compatibility was verified structurally via CodeDom syntax audits, constructor inspection, or automated tests without physically opening Visual Studio Designer.
