# Clovent Business Operating System (CBOS) — Gemini / Antigravity Context

This repository's authoritative coding-agent instructions are defined in [AGENTS.md](AGENTS.md). **Read and obey AGENTS.md before planning or making changes.**

Specialized domain rule files in `.agents/rules/` contain deep engineering specifications:
- [financial-integrity.md](.agents/rules/financial-integrity.md) — Financial accuracy, rounding governance, ledger immutability, currency precision.
- [winforms-ui.md](.agents/rules/winforms-ui.md) — WinForms, DevExpress 26.1, High-DPI PerMonitorV2, Designer safety.
- [database.md](.agents/rules/database.md) — Single DB `Clovent_BusinessOperatingSystem`, schemas, isolated migrations, least privilege.
- [security.md](.agents/rules/security.md) — DPAPI, ProgramData ACLs, UAC elevation, offline RSA-2048 licensing, brute-force defense.
- [testing.md](.agents/rules/testing.md) — xUnit, targeted test execution, live UI vs automated verification claims, evidence vocabulary.
- [release.md](.agents/rules/release.md) — Self-contained win-x64 publishing, exact artifact rule, ReleaseGuard verification.

---

## Antigravity Operational Directives

1. **Automatic Discovery & Hierarchy:**
   - Antigravity automatically discovers `AGENTS.md`, `GEMINI.md`, and `.agents/rules/*.md` by walking directory trees.
   - `AGENTS.md` is the primary project-wide authority.
   - `.agents/rules/*.md` provide deeper domain-specific constraints.
   - If instructions conflict with repository code, investigate code and tests before altering architecture.

2. **Parallel Work & High-Conflict File Governance:**
   - Always work within isolated Git worktrees for parallel implementation tasks.
   - Never edit high-conflict files concurrently across uncoordinated streams (`RestaurantPosForm.cs`, `Program.cs`, `ApplicationBootstrapper.cs`, `OperationsHealthForm.cs`, `CompleteOrderCommand.cs`, `OrderTotalsCalculator.cs`, installer scripts).
   - Maintain a maximum of 2–3 active implementation streams at any time.

3. **Escalation Triggers (Stop and Escalate):**
   - **Ambiguous Financial Semantics:** Never guess or silently implement accounting policy (such as rounding midpoint strategies or currency precisions). Stop and document an unresolved decision.
   - **Dirty Release Baseline:** If the source tree or release directory contains uncommitted modifications, stray binaries, or unauthorized scripts, stop and report immediately.
   - **Production Code Boundary:** Documentation and governance tasks must never alter production application source, migrations, or project files.

4. **Command & Build Protocol:**
   - Execute builds using the canonical commands:
     - `dotnet build Clovent.BusinessOperatingSystem.slnx -c Debug`
     - `dotnet build Clovent.BusinessOperatingSystem.slnx -c Release`
   - When running asynchronous background tasks (such as solution builds or test suites), do NOT poll `status` in a loop; await reactive notification messages from the environment.
   - Always run targeted tests before running full regression suites.

5. **Structured Evidence Reports:**
   - Never provide trivial one-line responses ("Fixed", "Tests passed").
   - Always conclude tasks with the structured Final Report standard specified in `AGENTS.md` using the exact Runtime Evidence Vocabulary.
