# ADR-0001: Single Physical Database with Schema-Isolated Contexts

| Attribute | Details |
| :--- | :--- |
| **Status** | **ACCEPTED** |
| **Date** | 2026-10-01 (Baseline 1.2.0+) |
| **Deciders** | Architecture Working Group |
| **Area** | Database Architecture & Persistence |

---

## 1. Context & Problem Statement

CBOS is composed of multiple autonomous bounded contexts (Authentication, Identity, MasterData, Catalog, Inventory, Restaurant). In classical distributed microservice architectures, each context owns its own physically separate database server or catalog file.

However, retail and hospitality installations operate predominantly on single physical workstations or local servers (often using SQL Server Express). Maintaining six physically separate databases on a single client machine presents severe operational challenges:
- High resource overhead on SQL Server Express memory limits (1.4 GB buffer pool shared across separate database instances).
- Complex multi-database backup, recovery, and point-in-time restore procedures.
- Inability to perform atomic cross-context reporting or transactional data seeding without distributed transactions (MSDTC), which are notoriously brittle on Windows desktops.

Conversely, merging all contexts into a single monolithic schema (`dbo`) and a single giant `DbContext` would destroy domain boundaries, encourage tight coupling, and cause severe migration conflicts.

---

## 2. Decision

CBOS adopts a **Single Physical Database with Dedicated SQL Schemas**:
1. Exactly **one** physical SQL Server database: `Clovent_BusinessOperatingSystem`.
2. Dedicated SQL Schemas for each bounded context:
   - `[Authentication]`
   - `[Identity]`
   - `[MasterData]`
   - `[Catalog]`
   - `[Inventory]`
   - `[Restaurant]`
3. Dedicated EF Core `DbContext` per context targeting its own schema.
4. Schema-scoped migration history tables: `[<Schema>].[__EFMigrationsHistory]` (e.g., `[Restaurant].[__EFMigrationsHistory]`).
5. Zero foreign keys across SQL schemas. Cross-context references are logical identifiers only (e.g., `Order.TerminalId`).

---

## 3. Consequences

### Positive
- **Single-File Backup & Restore:** Administrators take a single standard `.bak` backup that captures the entire business operating system in atomic consistency.
- **Strict Domain Boundaries:** EF Core models remain completely autonomous; developers cannot accidentally navigate relational entities across context boundaries.
- **Isolated Migration Cycles:** Migrations can be generated and applied independently per context without schema locks across the entire system.
- **Resource Efficiency:** Minimal memory footprint on SQL Server Express instances.

### Negative / Trade-offs
- Developers must explicitly configure schema-scoped migration history tables in `DbContextOptionsBuilder`.
- Relational integrity across contexts cannot rely on SQL Server `ON DELETE CASCADE` and must be enforced by application logic.

---

## 4. Compliance & Verification
- Verified by `SingleDatabaseConsolidationTests` and `WorkstationStartupVerificationTests`.
- All `*DbContextFactory` classes target `Clovent_BusinessOperatingSystem` with `.MigrationsHistoryTable("__EFMigrationsHistory", schema)`.
