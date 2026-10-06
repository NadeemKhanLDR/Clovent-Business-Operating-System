# Clovent Business Operating System - First-Run Commissioning & Setup Guide

This document details the multi-step First-Run Commissioning Wizard, commissioning state detection logic, and clean-machine onboarding workflow in Clovent Business Operating System (CBOS).

---

## 1. Commissioning State Detection Architecture

CBOS does not rely on a simple editable boolean flag such as `FirstRun=true` to determine whether an installation is ready for operational use. A reliable, multi-signal heuristic is evaluated on every application launch by `CommissioningStateService`:

1. **Signal 1: Database Configuration Exists & Is Reachable**  
   `database.config.json` is located in `%ProgramData%\Clovent\BusinessOperatingSystem\Config\`, contains valid server/credentials, and successfully connects.
2. **Signal 2: Database Schema Initialized**  
   The target physical database exists and all EF Core migrations across all 6 bounded contexts are applied.
3. **Signal 3: Foundational Master Data Exists**  
   At least one valid Organization, Company, and Branch record exist in the database.
4. **Signal 4: Active Administrator Account Exists**  
   At least one active user exists with the `Administrator` role assigned in `IdentityDbContext`.
5. **Signal 5: Local Commissioning Marker Present**  
   A cryptographic / protected commissioning record exists in `%ProgramData%\Clovent\BusinessOperatingSystem\Config\commissioning.json`.

If **ALL 5 signals** are satisfied, the system is deemed **Commissioned** and launches directly to the Sign-In screen. If **ANY signal is missing**, the First-Run Setup Wizard is presented.

---

## 2. Eight-Step Commissioning Wizard Workflow

```
[ Step 1: Welcome & Prerequisites ]
                 │
                 ▼
[ Step 2: Database Server Connection & Credentials ]
                 │
                 ▼
[ Step 3: Database Creation & Migration Execution ]
                 │
                 ▼
[ Step 4: Organization, Company & Branch Hierarchy ]
                 │
                 ▼
[ Step 5: First Administrator Account Provisioning ]
                 │
                 ▼
[ Step 6: Business & Regional Settings Initialization ]
                 │
                 ▼
[ Step 7: Software Registration & License Import ]
                 │
                 ▼
[ Step 8: Summary Review & Commissioning Finalization ]
                 │
                 ▼
         [ Normal Sign-In Screen ]
```

### Step 1: Welcome & Prerequisites
- Displays CBOS product overview, architecture notes, and deployment checklist.
- Confirms local operating system prerequisites (.NET 10 Desktop Runtime, minimum display resolution 1024x768).
- High-DPI Readability Standard:
  - Prerequisite checklist and detected environment items use `DesktopStyle.BodyFont` (`Segoe UI 10pt`) with `Appearance.Options.UseFont = true`.
  - Group card height scaled to 164 logical px (`DesktopDpi.Scale(164, this)`), row spacing scaled at 30 logical px intervals (`Scale(34, 64, 94, 124)`), eliminating text clipping, truncation, and overlap from 100% (96 DPI) up to 250% scaling (240 DPI).
  - Elevation status text uses explicit `Appearance.Options.UseForeColor = true` to preserve green status readability across all DevExpress skins.

### Step 2: Database Server Connection
- Server / Instance name (default: `.` or `localhost`).
- Database name (default: `Clovent_BusinessOperatingSystem`).
- Authentication Mode:
  - Windows Authentication (integrated security).
  - SQL Server Authentication (Username and masked password, encrypted via Windows DPAPI).
- **Test Connection** button must be executed and succeed before proceeding to Step 3.

### Step 3: Database Creation & Migration Execution
- Checks if `Clovent_BusinessOperatingSystem` already exists.
- If missing, prompts to create the database (transient installer credentials supported for DBA separation).
- Applies EF Core migrations across all 6 contexts with an interactive progress bar.
- Executes restaurant schema constraints and foundational tables.

### Step 4: Organization, Company & Branch Hierarchy
- Gathers core enterprise identity:
  - **Organization Name:** The legal parent enterprise (e.g. `Acme Hospitality Group`).
  - **Company Name:** The operating company entity (e.g. `Acme Dining LLC`).
  - **Branch Name:** The physical store location (e.g. `Downtown Flagship`).
- Created via domain aggregate factories (`Organization.Create`, `Company.Create`, `Branch.Create`). Idempotent to support resumption after partial setup.

### Step 5: First Administrator Account Provisioning
- Creates the foundational system administrator:
  - **Username:** e.g. `admin` or custom corporate administrator handle.
  - **Full Name:** Operator's display name.
  - **Email & Mobile:** Contact details for notifications and password recovery.
  - **Password & Confirm Password:** Enforces enterprise password policy (min 8 chars, uppercase, lowercase, digit, special character).
- **CRITICAL SECURITY RULE:** No default passwords like `Admin123!` are ever accepted. Once an administrator exists, this bootstrap flow permanently disables itself to prevent backdoor elevation.

### Step 6: Business & Regional Settings
- Initializes operational settings:
  - **Time Zone:** Selection from standard IANA / Windows timezones.
  - **Date Format:** e.g., `dd-MMM-yyyy` or `MM/dd/yyyy`.
  - **Time Format:** Choice of `12-hour` (`06:15 PM`) or `24-hour` (`18:15`).
  - **Currency Code & Precision:** e.g., `USD`, `EUR`, `GBP`, 2 decimal places.
  - **Terminal Identifier:** Registers this specific POS station (e.g., `POS-01`).

### Step 7: Software Registration & Licensing
- Displays current workstation `Hardware ID` (SHA-256 machine fingerprint).
- Offers **Copy Hardware ID** button.
- **Path A: 30-Day Evaluation / Trial Mode:**
  - Selecting *"Continue in Evaluation / Trial Mode (30-day evaluation period)"* enables full evaluation functionality without requiring a `.lic` file.
  - Automatically initializes cryptographically protected, DPAPI-secured trial state (`trial.state`) bound to the workstation.
  - Trial start timestamp is anchored to the cryptographic commissioning marker, preventing trial resets upon reinstall or restart.
- **Path B: Commercial / Paid License Import:**
  - Allows importing a vendor-signed `.lic` file generated via `tools\LicenseIssuer`.
  - Validates license signature, date, and hardware binding in-memory before copying to `%ProgramData%\Clovent\BusinessOperatingSystem\License\clovent.lic`.
  - Commercial license installation permanently supersedes evaluation mode.

### Step 8: Review & Finish
- Displays a complete summary of configured parameters (Server, DB, Organization, Branch, Admin username, Terminal, License).
- Sensitive fields (SQL password, admin password, license private data) are **NEVER** displayed.
- On clicking **Finish**, writes protected `commissioning.json` marker and transitions directly to Sign-In.

---

## 3. Resumability & Error Recovery

If the wizard is interrupted midway (e.g., power loss, database connection interruption):
- Completed steps remain stored in their respective tables (e.g., Database created, Organization created).
- Upon relaunch, the wizard queries existing master data and resumes seamlessly without attempting duplicate INSERTs or raising primary key conflicts.
