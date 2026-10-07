# CBOS Installation & Deployment Guide

| Attribute | Details |
| :--- | :--- |
| **Area** | Workstation & Server Deployment |
| **Audience** | Field Service Engineers, System Administrators, DevOps |
| **Last Reviewed Version** | CBOS 1.2.2 |
| **Status** | **PROCEDURAL STANDARD** |
| **Classification** | **PUBLIC-SAFE** |

---

## 1. Supported Operating Systems & Prerequisites

### 1.1 Supported Operating Systems
- **POS & Back-Office Workstations:**
  - Windows 11 Pro / Enterprise (64-bit, Version 22H2 or newer) — **Recommended**
  - Windows 10 Pro / Enterprise (64-bit, Version 21H2 or newer)
  - Windows 10 IoT Enterprise LTSC 2021 (64-bit)
- **Database Server (Multi-Terminal Setups):**
  - Windows Server 2022 (Standard / Datacenter)
  - Windows Server 2019 (Standard / Datacenter)
  - Windows 10/11 Pro (for small outlets running SQL Server Express on the main register)

### 1.2 Hardware Prerequisites
- **Processor:** 64-bit dual-core Intel/AMD processor (2.4 GHz minimum, quad-core recommended).
- **Memory (RAM):**
  - Dedicated POS Terminal: 4 GB minimum (8 GB recommended).
  - Standalone Single-Machine (App + SQL Express): 8 GB minimum (16 GB recommended).
  - Dedicated Database Server: 16 GB minimum.
- **Storage:** Minimum 10 GB available solid-state storage (NVMe/SATA SSD).
- **Display Resolution:** Minimum 1366x768; Baseline tested at **1920x1080 @ 100%–250% scaling**.
- **Software Dependencies:**
  - Microsoft SQL Server 2019 / 2022 (Express, Standard, or Enterprise).
  - **No .NET SDK required on client machines:** CBOS publishes as a self-contained `win-x64` application with all runtime libraries bundled.

---

## 2. Inno Setup Production Installer

Production installations utilize the native 64-bit Windows installer:
$$\mathbf{Clovent.BusinessOperatingSystem\text{-}1.2.2\text{-}Setup.exe}$$

### 2.1 Interactive GUI Installation Flow
1. **Windows UAC Elevation:** The installer requests administrative privileges to write to `%ProgramFiles%` and configure `%ProgramData%` ACLs.
2. **License Agreement:** Review and accept the software evaluation / end-user agreement.
3. **Installation Directory:**
   - Default: `C:\Program Files\Clovent\Business Operating System\`
4. **Data & State Directory:**
   - Default: `C:\ProgramData\Clovent\BusinessOperatingSystem\`
5. **Component Deployment:** Copies self-contained runtime binaries, DevExpress presentation engines, reporting libraries, and `Clovent.Installer.Provisioner.exe`.
6. **Shortcut Creation:** Installs desktop icon and Windows Start Menu entry for "Clovent Business Operating System".

### 2.2 Unattended / Silent Installation (Automated Deployment)
For bulk workstation rollouts across multi-store chains, execute the setup executable with silent flags:

```powershell
.\Clovent.BusinessOperatingSystem-1.2.2-Setup.exe /VERYSILENT /SUPPRESSMSGBOXES /NORESTART /SP-
```

---

## 3. Post-Installation Commissioning

Upon first launch after installation:
1. `ProgramDataAclManager` configures restrictive folder permissions.
2. The **First-Run Commissioning Wizard** (`FirstRunWizardForm`) automatically appears.
3. The technician tests the SQL Server connection, executes initial schema provisioning via EF Core migrations, establishes the company/branch identity, binds the terminal, and creates the primary administrator.

---

## 4. Current Installer (1.2.2) vs. Planned Optimizations

| Aspect | Current Release (CBOS 1.2.2) | Planned Future Release |
| :--- | :--- | :--- |
| **Prerequisite Chaining** | Requires pre-installed SQL Server (or manual SQL Express installation). | Bundled offline SQL Server Express bootstrapper. |
| **Topology Profiles** | Single universal client package for all workstation types. | Installer selection profiles: "Database Server", "POS Terminal", "Back Office Only". |
| **Silent Seed Injection** | Configuration configured interactively post-install. | Unattended XML/JSON response file for zero-touch mass provisioning. |

---

## 5. Cross References
- [Deployment Topologies](topologies.md)
- [SQL Server Deployment Guide](sql-server.md)
- [First-Run Commissioning](commissioning.md)
- [Upgrade Guide](upgrades.md)
- [Uninstallation Procedures](uninstallation.md)
