# CBOS Microsoft SQL Server Deployment Guide

| Attribute | Details |
| :--- | :--- |
| **Area** | Relational Database Platform & Administration |
| **Audience** | Database Administrators, Systems Engineers, DevOps |
| **Last Reviewed Version** | CBOS 1.2.2 |
| **Status** | **PROCEDURAL STANDARD** |
| **Classification** | **PUBLIC-SAFE** |

---

## 1. Supported Editions & Architectural Scope

CBOS targets **Microsoft SQL Server (2019 / 2022)** as its sole enterprise relational database engine:
- **Microsoft SQL Server Express:** Supported for single-workstation standalone stores and small multi-terminal outlets (up to 4 registers).
- **Microsoft SQL Server Standard / Enterprise:** Recommended for high-volume restaurants, multi-lane retail chains, and enterprise deployments requiring automatic high availability (Always On Availability Groups).
- **Physical Database Name:** Strictly **`Clovent_BusinessOperatingSystem`**.
- **Collation:** `SQL_Latin1_General_CP1_CI_AS`.

---

## 2. Technical Considerations for SQL Server Express

When deploying CBOS on Microsoft SQL Server Express, system administrators must design operations within the hard technical constraints enforced by the Express engine:

| SQL Server Express Constraint | Technical Limit | Operational Impact & Mitigation in CBOS |
| :--- | :--- | :--- |
| **Maximum Relational Data Size** | **10 GB per database** | More than sufficient for 3–5 years of typical restaurant order volume (~500,000 transactions). Automated archival strategies are recommended for high-volume stores. |
| **Maximum Buffer Pool Memory** | **1.4 GB RAM per instance** | Express will not utilize more than 1.4 GB of host RAM regardless of how much physical memory the server possesses. |
| **Maximum Compute Capacity** | **Lesser of 1 socket or 4 cores** | Adequate for store-level POS lane operations; high-concurrency reporting queries should be scheduled outside peak rush hours. |
| **SQL Server Agent** | **NOT AVAILABLE** | Automated scheduled maintenance jobs cannot run through SQL Agent. **Scheduled backups must be orchestrated via Windows Task Scheduler** using PowerShell or `sqlcmd`. |
| **Native Backup Compression** | **NOT SUPPORTED in Express** | Backups produced by `BACKUP DATABASE` are uncompressed. Administrators should compress `.bak` files using 7-Zip or PowerShell post-backup. |
| **Transparent Data Encryption (TDE)** | **NOT SUPPORTED in Express** | Physical database encryption at rest must rely on host Windows BitLocker full-disk encryption. |

---

## 3. Instance Configuration & Remote Terminal Access

When SQL Server is deployed on a dedicated server (Topology B), remote POS terminals must communicate over the local area network:

### 3.1 Enabling TCP/IP Protocol
By default, SQL Server Express disables network TCP/IP connections. To enable:
1. Open **SQL Server Configuration Manager**.
2. Navigate to **SQL Server Network Configuration** -> **Protocols for SQLEXPRESS** (or your instance).
3. Right-click **TCP/IP** and select **Enable**.
4. Double-click **TCP/IP**, open the **IP Addresses** tab, scroll to **IPAll**:
   - Set **TCP Dynamic Ports** to blank.
   - Set **TCP Port** to `1433`.
5. Restart the **SQL Server** service.

### 3.2 SQL Server Browser Service
If using a named instance (e.g. `SERVER\SQLEXPRESS`) without a fixed port:
- Start the **SQL Server Browser** service and set its startup type to **Automatic**.
- Ensure UDP Port `1434` is open on the server firewall.

### 3.3 Windows Firewall Inbound Rules
Run PowerShell as Administrator on the SQL Server machine to permit inbound traffic:

```powershell
New-NetFirewallRule -DisplayName "CBOS SQL Server (TCP 1433)" `
    -Direction Inbound -LocalPort 1433 -Protocol TCP -Action Allow

New-NetFirewallRule -DisplayName "CBOS SQL Browser (UDP 1434)" `
    -Direction Inbound -LocalPort 1434 -Protocol UDP -Action Allow
```

---

## 4. Runtime Least Privilege Security Model

To protect financial records against SQL injection or malware compromise, CBOS strictly enforces least privilege access:

### 4.1 Application Runtime User (`cbos_app`)
Create the dedicated application runtime login in SQL Server:

```sql
USE [master];
CREATE LOGIN [cbos_app] WITH PASSWORD = '[SECURE_GENERATED_PASSWORD]', CHECK_POLICY = ON;

USE [Clovent_BusinessOperatingSystem];
CREATE USER [cbos_app] FOR LOGIN [cbos_app];

-- Grant minimal required roles:
ALTER ROLE [db_datareader] ADD MEMBER [cbos_app];
ALTER ROLE [db_datawriter] ADD MEMBER [cbos_app];
GRANT EXECUTE TO [cbos_app];
```

### 4.2 Prohibited Runtime Privileges
The `cbos_app` user must **never** be granted:
- `sysadmin`, `serveradmin`, `securityadmin`
- `db_owner`, `db_ddladmin`, `db_accessadmin`
- `ALTER`, `DROP`, or `CREATE TABLE` permissions

DDL operations (such as applying schema migrations during upgrades) are executed by the administrator or `Clovent.Installer.Provisioner.exe` using temporary elevated setup credentials.

---

## 5. Cross References
- [Database Architecture](../database/database-architecture.md)
- [Deployment Topologies](topologies.md)
- [Backup & Restore Procedures](../operations/backup-and-restore.md)
- [Troubleshooting Runbook: Database Unavailable](../support/runbooks/database-unavailable.md)
