# Clovent Business Operating System - Client Deployment Guide

## 1. Overview
Clovent Business Operating System (CBOS) is an enterprise restaurant Point of Sale and Back Office management system built on .NET 10, DevExpress WinForms (v26.1), and Microsoft SQL Server.

The production release is built as a self-contained application for `win-x64`, meaning client workstations do not require Visual Studio, the .NET SDK, or developer runtimes.

---

## 2. Release Package Distribution Models
CBOS supports two distribution packages:

### A. Generic Release Package (Standard Distributable)
- **Content:** Self-contained application binaries, runtime assets, and static default configurations.
- **Licensing:** Ships **WITHOUT an active license file**.
- **Target Audience:** General releases, updates, and multi-tenant installer packages.
- **Workflow:** On first launch, the application prompts the operator with the Registration window to copy the Hardware ID and import a customer-specific signed license file.

### B. Customer-Specific Deployment Package (Pre-Provisioned)
- **Content:** Generic release binaries bundled with a specifically issued customer license (`clovent.lic`).
- **Requirements:** Must contain the customer's legal name, company name, designated branch/terminal quotas, and valid expiration dates. Universal wildcard licensing (e.g. valid to 2035 with null machine ID and unlimited terminals) is strictly prohibited.

---

## 3. Workstation Prerequisites
- **Operating System:** Windows 10 (version 1809 or higher), Windows 11, or Windows Server 2019/2022 (64-bit).
- **Processor:** 2.0 GHz multi-core x64 processor (Intel Core i3/i5/i7 or AMD equivalent).
- **Memory (RAM):** 8 GB minimum (16 GB recommended for back-office workstations).
- **Disk Space:** 2 GB free disk space for application files, local caches, and logs.
- **Display Resolution:** 1366×768 minimum, 1920×1080 recommended. Supports 96 DPI through High-DPI scaling (up to 250% / 240 DPI).
- **Peripherals:** Touchscreen display, ESC/POS receipt printer (OPOS/USB/Network), cash drawer, barcode scanner.

---

## 4. Client Installation Steps
1. **Deploy Binaries:**
   Extract the release package into the client directory (e.g. `C:\Program Files\Clovent\BusinessOperatingSystem\` or `D:\Clovent\CBOS\`).
2. **Database Connectivity:**
   Confirm network connectivity to the centralized Microsoft SQL Server host. Ensure the database user (`cbos_app`) is provisioned with least-privilege rights (`db_datareader`, `db_datawriter`, `EXECUTE`; no `db_ddladmin`).
3. **Desktop Shortcut:**
   Create a shortcut to `Clovent.Desktop.exe` on the desktop.

---

## 5. First-Run Commissioning & Setup Wizard Workflow
On a brand-new client workstation without existing commissioning data:
1. **Initial Launch & Commissioning Detection:**  
   `Clovent.Desktop.exe` checks for all 5 commissioning signals (DB connection, applied migrations, core master data, administrator account, and commissioning marker).
2. **First-Run Setup Wizard:**  
   If uncommissioned, the wizard opens:
   - **Step 1 (Welcome):** System overview and prerequisites.
   - **Step 2 (Database Connection):** Server name, database name, authentication mode (Windows Auth or SQL Auth with DPAPI encryption). Connection test required to advance.
   - **Step 3 (Database Initialization):** Fresh database creation or validation of existing DBA database, followed by automated EF Core migrations across all 6 contexts.
   - **Step 4 (Organization Hierarchy):** Enter legal Organization name, Company name, and Branch name.
   - **Step 5 (First Administrator):** Provision initial administrative account with unique username, full name, email/mobile, and strong password. (No default `Admin123!`). One-time bootstrap only.
   - **Step 6 (Regional & Business Settings):** Timezone, date format, 12h/24h time format, currency, and terminal registration name.
   - **Step 7 (Software Licensing):** Workstation Hardware ID display, copy button, and `.lic` file import with cryptographic validation.
   - **Step 8 (Review & Finish):** Parameter summary (zero secrets displayed). Writes protected commissioning marker and transitions directly to Sign-In.
3. **Subsequent Launches:**  
   Commissioned installations bypass the setup wizard and boot directly to Sign-In.

---

## 6. Directory Layout & Storage Architecture
- **Application Binaries:** `C:\Program Files\Clovent\BusinessOperatingSystem\` (Read-only for operators).
- **Machine-Wide Operational Storage:** `%ProgramData%\Clovent\BusinessOperatingSystem\`
  - `Config\`: `database.config.json` (DPAPI LocalMachine encrypted configuration), `commissioning.json` (commissioning marker). Protected by ACLs (Admins: Full Control; Users: Read-Only).
  - `License\`: `clovent.lic` (Signed software license), `license_tamper.dat`. Protected by ACLs (Admins: Full Control; Users: Read-Only).
  - `Logs\`: Rolling diagnostic logs (Admins: Full Control; Users: Read/Write/Append).
  - `State\`: Protected machine integrity and state records.
- **User-Specific State (Fallback):** `%LocalAppData%\Clovent\Clovent.BusinessOperatingSystem\`
- **Database:** Microsoft SQL Server (`Clovent_BusinessOperatingSystem`).

