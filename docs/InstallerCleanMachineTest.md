# Clovent Business Operating System - Clean Machine & Windows Sandbox Acceptance Guide

**Release Version:** 1.0.8 (Current) | 1.0.7 (Frozen Baseline Preserved)  
**Installer Artifact:** `artifacts\installer\Clovent.BusinessOperatingSystem-1.0.8-Setup.exe`  
**Test Objective:** Validate full automated onboarding, SQL Server Express chaining, database provisioning, responsive High-DPI first-run commissioning, license issuance, POS transactions, restart persistence, and uninstall safety on a completely clean Windows workstation.

---

## 1. Prerequisites for Sandbox Testing

- **Host Operating System:** Windows 10 Pro / Enterprise (build 1809+) or Windows 11 Pro / Enterprise.
- **Windows Sandbox Feature:** Enabled via Windows Features (`WindowsSandbox.exe`).
- **Network Access:** Enabled in Windows Sandbox by default (required for downloading SQL Server Express if not pre-staged).
- **Offline Alternative:** If testing in an air-gapped / offline environment, copy `SQLEXPR_x64_ENU.exe` into the same folder as `Setup.exe`.

---

## 2. Step-by-Step Clean Machine Test Procedure

```
[ Step 1: Launch Sandbox ]
          │
          ▼
[ Step 2: Copy Setup Executable (1.0.8) ]
          │
          ▼
[ Step 3: Run Setup & Accept UAC ]
          │
          ▼
[ Step 4: Automated Setup Execution ]
  - SQL Server detection & install (CLOVENT)
  - Application binary extraction (1.0.8)
  - Database provisioning & schema migrations
  - Payment method seeding (Cash, Card, On Account)
  - Security hardening & DPAPI configuration
          │
          ▼
[ Step 5: First-Run Commissioning Wizard & High-DPI Check ]
  - Verify responsive window dimensions (65–80% of screen)
  - Verify unclipped header, sidebar, and footer
  - Enterprise hierarchy (Org / Company / Branch)
  - First administrator account
  - Regional & terminal settings
  - Dynamic sandbox license issuance & import
          │
          ▼
[ Step 6: Sign-In & POS Acceptance ]
  - Cash order & settlement
  - Card order & settlement
          │
          ▼
[ Step 7: Restart & Persistence Validation ]
          │
          ▼
[ Step 8: Safe Uninstallation Verification ]
```

---

### Step 1: Open Windows Sandbox
1. Press `Win + S`, type `Windows Sandbox`, and press Enter.
2. A completely fresh, pristine Windows virtual environment will boot in seconds.

---

### Step 2: Copy the Installer Package
1. On your host machine, navigate to:
   ```text
   d:\Clovent Business Operating System\artifacts\installer\
   ```
2. Copy `Clovent.BusinessOperatingSystem-1.0.8-Setup.exe` (Ctrl+C).
3. Switch into the Windows Sandbox desktop and paste it (Ctrl+V).
4. *(Optional for Offline Testing)*: Also copy `SQLEXPR_x64_ENU.exe` into the same folder.

---

### Step 3: Run Setup & Elevation
1. Double-click `Clovent.BusinessOperatingSystem-1.0.8-Setup.exe`.
2. When prompted by Windows User Account Control (UAC), click **Yes** to allow administrative elevation.

---

### Step 4: Automated Installation Execution
1. **Welcome Screen:** Click **Next**.
2. **Installation Directory:** Default is `C:\Program Files\Clovent\Business Operating System`. Click **Next**.
3. **Shortcuts:** Keep **Create a desktop shortcut** checked. Click **Next**.
4. **Ready to Install:** Click **Install**.
5. **Automated Pipeline Runs:**
   - Detects that no SQL Server exists.
   - Automatically acquires and installs Microsoft SQL Server 2022 Express under named instance `CLOVENT`.
   - Starts and verifies Windows Service `MSSQL$CLOVENT`.
   - Deploys the self-contained CBOS 1.0.8 application payload.
   - Invokes the production database provisioner:
     - Creates database `Clovent_BusinessOperatingSystem`.
     - Applies EF Core migrations across all 6 contexts (`Authentication`, `Identity`, `MasterData`, `Catalog`, `Inventory`, `Restaurant`).
     - Idempotently seeds active core payment methods: `Cash`, `Card`, `On Account`.
     - Enforces `%ProgramData%\Clovent\BusinessOperatingSystem\` directory ACLs.
     - Encrypts and writes machine database configuration via DPAPI.
     - Validates final schema compatibility.
6. **Finish Screen:** Ensure **Launch Clovent Business Operating System** is checked and click **Finish**.

---

### Step 5: First-Run Commissioning Wizard & Display Scaling Acceptance
When `Clovent.Desktop.exe` launches, it detects that the technical database is ready, but business setup is needed. The First-Run Wizard appears:

#### High-DPI & Display Scaling Verification Checklist:
Before filling fields, verify the visual layout integrity of `FirstRunWizardForm`:
- [ ] **Window Geometry:** Form opens at a comfortable, large size occupying approximately 65%–80% of the usable screen area (centered). It must **not** open as a tiny, undersized dialog.
- [ ] **Window Sizability & Maximize:** The window has resizable borders (`Sizable`) and a maximize button (`MaximizeBox = true`). Test maximizing and resizing the window to confirm responsive expansion.
- [ ] **Header:** Title *"First-Run Setup & Commissioning"* and subtitle are crisp, fully readable, and not clipped vertically or horizontally.
- [ ] **Sidebar (Left):** All 8 steps (1. Overview, 2. Database, 3. Schemas, 4. Organization, 5. Administrator, 6. Regional, 7. Licensing, 8. Finalize) are fully visible without text clipping or vertical bunching.
- [ ] **Footer (Bottom):** Navigation buttons (**Cancel**, **Back**, **Next**, **Finish**) are clearly visible and docked to the right. The status indicator label (`lblFooterStatus`) never collides with or overlaps any button.
- [ ] **Display Scaling Tests:** Test at target display scalings (100%, 125%, 150%, 175%, 200%, 225%, 250%) and resolutions (1366x768, 1600x900, 1920x1080). Auto-scrollbars appear smoothly on content panels if the viewport is constrained.

#### Wizard Step Execution:
1. **Step 1 (Welcome & Detection):**
   - Verify OS, 64-bit runtime, DPI scaling, and elevation status. Click **Next**.
2. **Step 2 (Database Connection):**
   - Server name defaults to `.\CLOVENT` (or `(local)`).
   - Click **Test Connection**. Status displays: *"Connection successful!"* (Green). Click **Next**.
3. **Step 3 (Database Initialization):**
   - Wizard detects all migrations are already applied: *"All database schemas are initialized and up-to-date."*
   - Click **Next**.
4. **Step 4 (Organization Hierarchy):**
   - Enter **Organization Name:** `Grandview Hospitality Group`
   - Enter **Company Name:** `Grandview Dining LLC`
   - Enter **Branch Name:** `Downtown Flagship`
   - Click **Next**.
5. **Step 5 (First Administrator):**
   - Enter **Username:** `admin_pilot`
   - Enter **Full Name:** `Operations Director`
   - Enter **Email:** `director@grandview.com`
   - Enter **Password:** `Pilot@2026!Secure` (Must satisfy enterprise complexity policy: min 8 chars, uppercase, lowercase, number, symbol).
   - Enter **Confirm Password:** `Pilot@2026!Secure`
   - Click **Next**.
6. **Step 6 (Regional & Terminal Settings):**
   - Select Timezone, Date Format (`dd-MMM-yyyy`), Time Format (`12-hour`), and Currency (`Rs.` or `USD`).
   - Enter **Terminal Name:** `POS-FRONT-01`.
   - Click **Next**.
7. **Step 7 (Software Licensing):**
   - The wizard displays the workstation's unique **Hardware ID** (e.g. `F4A8-11BC-99E2-7D01`).
   - Click **Copy Hardware ID**.
   - **Issuing the License on the Host Workstation:**
     Open PowerShell on the host repository workstation and execute:
     ```powershell
     cd "d:\Clovent Business Operating System"
     dotnet run --project tools\LicenseIssuer -- --issue `
         --customer "Grandview Dining LLC" `
         --machine-id "<PASTED-HARDWARE-ID>" `
         --days 30 `
         --terminals 5 `
         --out "d:\clovent.lic"
     ```
   - Copy `d:\clovent.lic` from the host and paste it into Windows Sandbox (e.g. on Desktop).
   - In the Sandbox Commissioning Wizard, click **Import License File**, browse to `clovent.lic`, and click Open.
   - Verify status displays: *"License Valid: Grandview Dining LLC (30 days remaining)"*.
   - Click **Next**.
8. **Step 8 (Review & Finish):**
   - Review summary of configured enterprise settings. Click **Finish**.

---

### Step 6: Sign-In & POS Acceptance Testing
1. The CBOS **Sign-In** screen appears automatically.
2. Enter username: `admin_pilot` and password: `Pilot@2026!Secure`.
3. Click **Sign In**.
4. Select **Restaurant POS** module.
5. If prompted, confirm opening shift float (e.g. `1000.00`).
6. **Test Cash Transaction:**
   - Select menu items (e.g., standard dish).
   - Click **Tender / Settle**.
   - Select payment method: **Cash**.
   - Enter tendered amount and complete order.
   - Verify sale is recorded.
7. **Test Card Transaction:**
   - Select menu items.
   - Click **Tender / Settle**.
   - Select payment method: **Card**.
   - Enter reference code and complete order.
   - Verify sale is recorded.

---

### Step 7: Restart & Persistence Validation
1. Exit CBOS.
2. Launch CBOS from the **Desktop shortcut** (`Clovent Business Operating System`).
3. **Verify:** The First-Run Wizard is **bypassed completely**. The Sign-In screen appears immediately.
4. Sign in with `admin_pilot` / `Pilot@2026!Secure`.
5. Open **End of Day / Shift Summary** or **Orders History**:
   - Confirm that the previously placed Cash and Card orders are present and persisted.

---

### Step 8: Safe Uninstallation Verification
1. Close CBOS.
2. Open Windows **Settings** -> **Apps** -> **Installed Apps**.
3. Locate **Clovent Business Operating System** and click **Uninstall**.
4. Complete the uninstaller wizard.
5. **Verify:**
   - `C:\Program Files\Clovent\Business Operating System\` is removed.
   - Start Menu and Desktop shortcuts are removed.
   - The SQL Server instance `MSSQL$CLOVENT` is **still running**.
   - The database `Clovent_BusinessOperatingSystem` remains **intact and uncorrupted** in SQL Server.
   - Machine configurations in `%ProgramData%\Clovent\BusinessOperatingSystem\` remain preserved for potential reinstallation or recovery.
