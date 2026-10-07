# CBOS Field Troubleshooting Guide

| Attribute | Details |
| :--- | :--- |
| **Area** | Field Support & Issue Resolution |
| **Audience** | Level 1–3 Support Technicians, Helpdesk Engineers |
| **Last Reviewed Version** | CBOS 1.2.2 |
| **Status** | **PROCEDURAL STANDARD** |
| **Classification** | **PUBLIC-SAFE** |

---

## 1. Principles of Safe Support

> [!CAUTION]
> **STRICT SUPPORT SAFETY RULES:**
> When diagnosing and repairing customer installations, support personnel must **NEVER**:
> - Manually delete or modify rows in `[Restaurant].[Orders]` or `[Identity].[Users]`.
> - Blindly truncate or delete rows from `[Restaurant].[OutboxMessages]`.
> - Delete `%ProgramData%\Clovent\BusinessOperatingSystem\continuity_journal.dat` (this permanently destroys un-replayed cash sales!).
> - Disable Windows Firewall or grant `sa` / `sysadmin` roles to application logins to "fix" connection issues.

---

## 2. Common Issues & Resolution Matrix

| Symptom / Error Message | Root Cause | Safe Resolution Procedure | Detailed Runbook |
| :--- | :--- | :--- | :--- |
| **"Unable to connect to database"** | SQL service stopped, port 1433 blocked, or DPAPI credentials altered. | 1. Verify `MSSQL$SQLEXPRESS` service is running.<br/>2. Test port 1433 via `Test-NetConnection`.<br/>3. Re-save credentials in `DatabaseConnectionDialog`. | [database-unavailable.md](runbooks/database-unavailable.md) |
| **"Database Schema Update Required"** | Client assembly is newer than SQL Server schema migrations. | Run `Clovent.Installer.Provisioner.exe --migrate` as Administrator to apply pending migrations. | [schema-mismatch.md](runbooks/schema-mismatch.md) |
| **"License Invalid" / "Clock Tampered"** | Clock rolled backward or machine hardware UUID changed. | 1. Verify system clock against NTP.<br/>2. Launch `SoftwareRegistrationForm` to re-import replacement license. | [license-issue.md](runbooks/license-issue.md) |
| **"Multiple active terminals found..."** | Register workstation has no explicit terminal binding. | Configure `TerminalId` in Master Data > Terminals or set `CBOS_TERMINAL_ID` environment variable. | [terminal-identity.md](../configuration/terminal-identity.md) |
| **Thermal Printer Unavailable / Queue Stalled** | Printer out of paper, USB disconnected, or spooler jammed. | 1. Refill paper and clear Windows print queue.<br/>2. Outbox will automatically resume without dropping sales. | [printer-failure.md](runbooks/printer-failure.md) |
| **Outbox Queue Growing / Circuit Open** | External integration service offline (e.g. QuickBooks). | Inspect **Operations Health Center**; verify remote network endpoint. Handlers will retry automatically. | [outbox-stalled.md](runbooks/outbox-stalled.md) |
| **POS Stuck in Continuity Mode** | Database connection remains unreachable from POS register. | Check LAN switch, verify server IP, and inspect `%LOCALAPPDATA%\Clovent\Logs\`. Reconnection triggers auto-replay. | [continuity-mode.md](runbooks/continuity-mode.md) |
| **"Operational Cache Stale / Expired"** | Cache file exceeded 24-hour TTL during extended outage. | Re-connect POS to database briefly to refresh cache via `OperationalCacheSynchronizer`. | [operational-cache.md](../resilience/operational-cache.md) |
| **SQL Server Setup Slow / Hanging** | Windows Defender scanning SQL installer or missing VC++ redist. | Temporarily exclude setup directory from real-time scan; verify Windows Updates are not pending reboot. | [sql-installation-slow.md](runbooks/sql-installation-slow.md) |
| **Application Fails on Startup** | Corrupted ProgramData DACLs or missing .NET runtime dependencies. | Launch via `--diagnostics` switch to inspect log messages and verify directory security. | [SupportDiagnostics.md](../SupportDiagnostics.md) |

---

## 3. Support Runbooks Library

For step-by-step procedures, consult the dedicated support runbooks:
1. **[Database Unavailable](runbooks/database-unavailable.md)** — Diagnosing SQL services, TCP/IP, and firewall connectivity.
2. **[Continuity Mode Troubleshooting](runbooks/continuity-mode.md)** — Managing emergency cash selling and resolving journal replay issues.
3. **[Outbox Stalled](runbooks/outbox-stalled.md)** — Diagnosing dead-letter backlogs and resetting circuit breakers.
4. **[Printer Failure](runbooks/printer-failure.md)** — Resolving thermal spooler jams and paper-out errors safely.
5. **[Licensing & Clock Issues](runbooks/license-issue.md)** — Resolving anti-tamper clock flags and re-activating commercial licenses.
6. **[Schema Mismatch](runbooks/schema-mismatch.md)** — Running the database provisioner to reconcile migration histories.
7. **[Installer Failure](runbooks/installer-failure.md)** — Resolving Inno Setup permission blocks and rollback conditions.
8. **[SQL Installation Slow](runbooks/sql-installation-slow.md)** — Resolving hanging SQL Server Express unattended deployments.

---

## 4. Cross References
- [Operations Health Monitoring](../operations/operations-health.md)
- [Logging & Diagnostics](../operations/logging-and-diagnostics.md)
- [Backup & Restore](../operations/backup-and-restore.md)
