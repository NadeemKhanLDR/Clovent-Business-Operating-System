# ADR-0006: Microsoft SQL Server as Enterprise Database Platform

| Attribute | Details |
| :--- | :--- |
| **Status** | **ACCEPTED** |
| **Date** | 2026-09-01 |
| **Deciders** | Architecture & Infrastructure Working Group |
| **Area** | Database Platform & Storage |

---

## 1. Context & Problem Statement

Commercial retail and hospitality systems require a relational database platform that satisfies:
- Strict ACID transactional guarantees for financial settlements and inventory ledgers.
- Seamless compatibility with Windows desktop and server environments.
- Support for row-level locking, concurrent multi-terminal network read/write operations, and robust backup/restore tooling.
- Availability of a zero-cost, freely redistributable edition for entry-level deployments (SQL Server Express).
- First-class tooling, mature ORM support via Entity Framework Core, and simple DBA management.

---

## 2. Decision

CBOS standardizes on **Microsoft SQL Server (2019 / 2022)** as its sole enterprise relational database engine:
1. Supported Editions: SQL Server Express, Standard, and Enterprise.
2. The runtime application connects via `Microsoft.EntityFrameworkCore.SqlServer`.
3. Application access is restricted to a dedicated non-administrator login (`cbos_app`) with `db_datareader`, `db_datawriter`, and `EXECUTE` privileges only.
4. Schema updates and initial provisioning are executed via EF Core migrations managed by a dedicated command-line provisioner (`Clovent.Installer.Provisioner.exe`).

---

## 3. Consequences

### Positive
- Proven enterprise reliability and ACID consistency for financial ledgers.
- Transparent scale-up path: customers can begin on free SQL Server Express and upgrade seamlessly to SQL Server Standard or Enterprise without changing application code.
- Native integration with Windows authentication and DPAPI credential protection.

### Negative / Trade-offs
- Deployment on SQL Server Express requires operating within Express resource constraints (10 GB max database file size, 1.4 GB buffer pool RAM limit, no SQL Server Agent).
- Requires Windows host environment (or Linux container in future server topologies).
