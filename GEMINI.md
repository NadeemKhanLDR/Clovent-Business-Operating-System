# Clovent Business Operating System (CBOS) — Gemini / Antigravity Context

This repository's authoritative coding-agent instructions are defined in [AGENTS.md](file:///d:/Clovent%20Business%20Operating%20System/AGENTS.md). **Read and obey AGENTS.md before planning or making changes.**

Specialized domain rule files in `.agents/rules/` contain deep engineering specifications:
- [winforms-ui.md](file:///d:/Clovent%20Business%20Operating%20System/.agents/rules/winforms-ui.md) — WinForms, DevExpress 26.1, High-DPI PerMonitorV2, Designer safety.
- [database.md](file:///d:/Clovent%20Business%20Operating%20System/.agents/rules/database.md) — Single DB `Clovent_BusinessOperatingSystem`, schemas, isolated migrations, least privilege.
- [security.md](file:///d:/Clovent%20Business%20Operating%20System/.agents/rules/security.md) — DPAPI, ProgramData ACLs, UAC elevation, offline RSA-2048 licensing.
- [testing.md](file:///d:/Clovent%20Business%20Operating%20System/.agents/rules/testing.md) — xUnit, targeted test execution, live UI vs automated verification claims.
- [release.md](file:///d:/Clovent%20Business%20Operating%20System/.agents/rules/release.md) — Self-contained win-x64 publishing, ReleaseGuard verification.

---

## Antigravity Operational Directives

1. **Automatic Discovery & Hierarchy:**
   - Antigravity automatically discovers `AGENTS.md`, `GEMINI.md`, and `.agents/rules/*.md` by walking directory trees.
   - `AGENTS.md` is the primary project-wide authority.
   - `.agents/rules/*.md` provide deeper domain-specific constraints.
   - If instructions conflict with repository code, investigate code and tests before altering architecture.

2. **Command & Build Protocol:**
   - Execute builds using the canonical commands:
     - `dotnet build Clovent.BusinessOperatingSystem.slnx -c Debug`
     - `dotnet build Clovent.BusinessOperatingSystem.slnx -c Release`
   - When running asynchronous background tasks (such as solution builds or test suites), do NOT poll `status` in a loop; await reactive notification messages from the environment.
   - Always run targeted tests before running full regression suites.

3. **Subagent Delegation:**
   - For multi-disciplinary tasks involving independent bounded contexts (e.g. database schema changes alongside WinForms UI updates), use subagents (`invoke_subagent`).
   - The primary agent must reconcile all code changes before final build verification.

4. **Task Responses:**
   - Never provide trivial one-line responses ("Fixed", "Tests passed").
   - Always format task completion using the structured Final Report standard specified in `AGENTS.md`.
