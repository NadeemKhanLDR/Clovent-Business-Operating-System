# Clovent Business Operating System (CBOS) — Versioning Policy

> **Standard Header**
> - **Document ID:** REL-VER-001
> - **Category:** Release Engineering
> - **Target Audience:** Architects, Release Engineers, Developers, Operations
> - **Status:** VALIDATED
> - **Last Updated:** 2026-10-07

---

## 1. Executive Summary & Philosophy

Clovent Business Operating System (CBOS) follows strict **Semantic Versioning 2.0.0** (`MAJOR.MINOR.PATCH`) supplemented by build metadata and pre-release identifiers where applicable.

Because CBOS is an enterprise distributed retail/restaurant application comprising desktop clients, a transactional SQL Server database with 6 isolated schemas, and offline Continuity Mode, version numbers convey explicit architectural guarantees regarding database migration compatibility, API contracts, and client-server interoperability.

```
                  ┌────────────── MAJOR: Breaking changes (schema/arch)
                  │ ┌──────────── MINOR: Backwards-compatible features
                  │ │ ┌────────── PATCH: Backwards-compatible fixes
                  ▼ ▼ ▼
               v  1 . 2 . 2
```

---

## 2. Semantic Versioning Rules for CBOS

### 2.1 Major Version (`X.0.0`)
A Major version increment indicates substantial, breaking architectural changes that prevent direct backward compatibility without formal migration procedures or administrative intervention.

**Triggers for MAJOR bump:**
1. **Breaking Database Schema Changes:**
   - Removal or irreversible modification of tables, columns, or foreign key constraints.
   - Restructuring that prevents an older client from executing queries against a newer database schema.
2. **Architectural Redesigns:**
   - Overhaul of the transactional outbox messaging format, rendering pending unconsumed messages invalid.
   - Breaking changes to the Continuity Mode HMAC journal format or encryption envelope.
   - Changes to the cryptographic licensing scheme (e.g. migration from RSA-2048 to post-quantum signatures).
3. **Platform & Runtime Baseline Shifts:**
   - Upgrading the target runtime to a new major .NET generation (e.g. .NET 10 to .NET 11).
   - Dropping support for target operating systems (e.g., dropping Windows 10 support).

### 2.2 Minor Version (`1.X.0`)
A Minor version increment introduces new business features, aggregates, or non-breaking architectural enhancements while maintaining full backward compatibility with existing databases and operational data.

**Triggers for MINOR bump:**
1. **New Bounded Contexts or Aggregates:**
   - Adding a new module or feature set (e.g., table reservation system, integrated kitchen display controller).
2. **Additive Schema Migrations:**
   - Adding new nullable columns, default-valued columns, or entirely new tables within existing schemas.
   - Existing running clients of the same major version can continue operating without crashing.
3. **New Application Services & Outbox Handlers:**
   - Adding new MediatR commands, queries, or outbox message consumer handlers.
4. **Desktop UI Enhancements:**
   - Introducing new forms, back-office reporting tabs, or workflow views.

### 2.3 Patch Version (`1.2.X`)
A Patch version increment delivers backward-compatible bug fixes, UI styling refinements, security patches, performance optimizations, and non-breaking data adjustments.

**Triggers for PATCH bump:**
1. **Defect Resolutions:**
   - Fixing grid layout truncation, DPI scaling quirks, or button alignment.
   - Correcting calculation edge cases (e.g., rounding tolerances in cash drawer variance).
2. **Performance Optimizations:**
   - Adding indexes in schema migrations to speed up reporting queries.
   - Reducing memory allocation in background outbox dispatchers.
3. **Security Hotfixes:**
   - Updating dependencies to resolve third-party CVEs without breaking public contracts.
   - Strengthening input validation or DPAPI access controls.

---

## 3. Project & Assembly Versioning Hierarchy

In CBOS, versioning metadata is defined in project files (or centrally via `Directory.Build.props` when configured) and flows into compiled assemblies:

```xml
<PropertyGroup>
  <Version>1.2.2</Version>
  <AssemblyVersion>1.2.2.0</AssemblyVersion>
  <FileVersion>1.2.2.0</FileVersion>
  <InformationalVersion>1.2.2+commit-sha</InformationalVersion>
</PropertyGroup>
```

### 3.1 Metadata Meanings:
- **`Version` (Package / SemVer):**
  - Canonical 3-component version (`1.2.2`) used in NuGet packages, documentation, release notes, and user-facing dialogs (About box, Splash screen).
- **`AssemblyVersion`:**
  - 4-component integer format (`1.2.2.0`) required by the .NET runtime for CLR assembly identity and binding. The fourth digit (revision) is reserved for emergency hotfix builds.
- **`FileVersion`:**
  - 4-component integer format (`1.2.2.0`) embedded in the Windows Win32 PE header (`FILEVERSION`), visible in Windows File Explorer properties and installer metadata.
- **`InformationalVersion`:**
  - Full product release identifier, typically formatted as `Version+CommitSha` (e.g., `1.2.2+a1b2c3d4e5`). Used in crash telemetry, logs, and diagnostic reports to identify the exact source state.

---

## 4. Database Schema & Migration Versioning

Because CBOS uses 6 schema-scoped migration histories in a single physical database (`Clovent_BusinessOperatingSystem`), migration versioning must follow rigid discipline:

### 4.1 Migration Naming Standard
Every EF Core migration class must follow the standard timestamped naming format:
```
{YYYYMMDDHHMMSS}_{Context}_{Description}
```
*Examples:*
- `20261005120000_Restaurant_AddRushOrderFlag`
- `20261006093000_Inventory_AddReorderThresholdIndex`

### 4.2 Migration Rules Across Versions:
1. **Immutable History:** Once a migration has been released in a published version (Minor or Patch), its migration code and designer snapshot (`.Designer.cs`) are **frozen**. They must NEVER be modified or re-generated.
2. **Forward-Only Corrections:** If a migration introduced an issue in production, a new corrective migration must be added in the subsequent patch.
3. **Schema Compatibility Validator:** During application startup, `DatabaseSchemaCompatibilityValidator` inspects the applied migration IDs in `[<Schema>].[__EFMigrationsHistory]` against the embedded migrations in the executing assembly:
   - If the database contains migrations newer than the client: The client warns or halts if breaking differences are detected.
   - If the database is missing migrations required by the client: The client halts and prompts for the Database Provisioner / Upgrade utility.

---

## 5. Continuity Journal & Cache Compatibility

When deploying client updates, local offline storage structures must be version-aware:

1. **HMAC Journal Schema:**
   - Local journal files stored in `%ProgramData%\Clovent\BusinessOperatingSystem\Continuity\` include a `JournalVersion` header.
   - Upgrading client binaries must retain backward compatibility to replay pending journals recorded by prior patch/minor versions before updating internal formats.
2. **Operational Cache:**
   - Operational Cache files (`operational-cache.dat`) include a cache version tag. If the cache schema changes between versions, the cache is treated as stale, invalidated, and re-fetched from the database on next online connection.

---

## 6. Pre-Release & Build Identifiers

For development, testing, and release candidates, SemVer pre-release strings are appended:

| Tag | Usage | Example |
| :--- | :--- | :--- |
| `-alpha.N` | Internal development branches and experimental builds | `1.3.0-alpha.1` |
| `-beta.N` | Feature-complete builds undergoing integration testing | `1.3.0-beta.2` |
| `-rc.N` | Release Candidates undergoing final Windows Sandbox acceptance | `1.2.3-rc.1` |
| *(None)* | Production GA (General Availability) release | `1.3.0` |

---

## 7. Versioning Matrix (Current State)

| Component | Current Version | Target Baseline / Status |
| :--- | :--- | :--- |
| **Product Release** | `1.2.2` | Frozen Internal Acceptance Baseline (Not GA / Not Paid-Pilot Ready) |
| **Clovent.Desktop** | `1.2.2.0` | net10.0-windows |
| **Domain & Application Contexts** | `1.2.2.0` | net10.0 |
| **Database Schemas** | Up-to-date with 1.2.2 migrations (Zero migrations for 1.2.3; concurrency tokens belong to 1.3.0) | SQL Server 2019/2022 |
| **License Schema** | `clovent-2026-v2` | RSA-2048 / SHA-256 |
| **Installer** | `1.2.2` | Inno Setup 6 |

> **Note on Maturity & Readiness:** CBOS 1.2.2 is strictly a frozen internal acceptance baseline (clean-machine acceptance remains PENDING unless actual runtime acceptance evidence is supplied). Version numbers (such as 1.2.3 or 1.3.0) represent target release milestones, not automatic grants of operational or commercial readiness. No version number automatically confers pilot readiness or GA status; readiness is earned strictly through qualification gates. Paid pilot operations are targeted for CBOS 1.2.3 (with zero database schema migrations), and commercial GA is targeted for CBOS 1.3.0. See [Engineering Roadmap](../roadmap/engineering-roadmap.md) and [Product Readiness Levels](../development/readiness-levels.md).
