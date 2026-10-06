# Clovent Business Operating System - Software Licensing & Registration Guide

## 1. Architectural Overview & Cryptographic Model
Clovent Business Operating System (CBOS) utilizes an **offline, asymmetric cryptographically signed licensing architecture**:
- **Algorithm:** Asymmetric RSA-2048 with SHA-256 digital signature (`RSASignaturePadding.Pkcs1`).
- **Cryptographic Key Separation:**
  - **Vendor Private Signing Key:** Held strictly by the software vendor and maintained in an external secure location (`%USERPROFILE%\.clovent\keys\clovent_vendor_private_key.pem` or external HSM/secret vault). **Never committed to Git, never stored inside the source repository, never copied into build artifacts, never present on client machines, and never embedded in application binaries.**
  - **Embedded Verification Public Key:** Embedded within `Clovent.Desktop.Licensing.LicenseKeys` assembly for zero-network offline verification.
- **Key Versioning & Rotation Support:**
  - `clovent-2026-v2`: Current active production key pair.
  - `clovent-2026-v1`: Revoked legacy key pair (marked revoked and permanently rejected).
  - Rotation is facilitated by the `KeyId` attribute in each license, verified against a trusted key ring in `LicenseKeys.cs`.

---

## 2. Customer-Specific License Schema (`clovent.lic`)
Licenses are distributed as JSON files containing customer identity and cryptographic signature:

```json
{
  "KeyId": "clovent-2026-v2",
  "Product": "Clovent Business Operating System",
  "LicenseId": "e27765b7-ddd9-4ca6-b572-171e11f7a1b3",
  "CustomerName": "Highland Restaurant Group",
  "CompanyName": "Highland Dining LLC",
  "LicenseType": "Subscription",
  "IssueDate": "2026-10-01T15:00:00.0000000+00:00",
  "ValidFrom": "2026-10-01T15:00:00.0000000+00:00",
  "ExpiryDate": "2027-10-01T15:00:00.0000000+00:00",
  "MaxBranches": 2,
  "MaxTerminals": 5,
  "MaintenanceExpiry": "2027-10-01T15:00:00.0000000+00:00",
  "AllowedModules": [
    "BackOffice",
    "Catalog",
    "Inventory",
    "POS",
    "Reporting",
    "Restaurant"
  ],
  "MachineId": "A3F1-90B2-88C4-E102",
  "Signature": "BASE64_RSA_SHA256_SIGNATURE"
}
```

### Supported License Tiers:
1. **Trial:** Time-limited evaluation licenses (e.g. 14 to 30 days).
2. **Subscription:** Renewable term licenses (e.g. 1 year). Expire after term + 14-day grace period.
3. **Perpetual:** Software execution remains valid indefinitely. `MaintenanceExpiry` controls update and version upgrade eligibility without ever expiring core software execution.

---

## 3. Licensing Deployment Modes
CBOS supports three licensing topologies:
1. **Terminal License (Node-Locked):** Bound to the workstation's hardware fingerprint (`MachineId` matching `MachineFingerprint.GetCurrentMachineId()`). Prevents unauthorized cloning across terminals.
2. **Site / Company License:** `MachineId` is omitted (`null`), allowing floating use across terminals up to `MaxTerminals` within the licensed company.
3. **Enterprise License:** Unbound floating license deliberately issued to a specific enterprise customer with designated branch and terminal limits. Universal wildcard licenses without customer identity are strictly prohibited in production builds.

---

## 4. Protected Storage & License Import Security
To prevent unauthorized modification or replacement:
- **Storage Locations:**
  1. **Machine-Wide Protected Storage (Preferred):**  
     `%ProgramData%\Clovent\BusinessOperatingSystem\License\clovent.lic`  
     Accessible to all local workstation operators, but writable only by Administrators.
  2. **User-Level Storage (Fallback):**  
     `%LocalAppData%\Clovent\Clovent.BusinessOperatingSystem\clovent.lic`
- **Pre-Copy Validation:** When importing a new license via `LicenseService.ImportLicense(filePath)`:
  1. The candidate license is parsed and verified for cryptographic signature, product identity, key validity, machine match, and date validity **BEFORE** copying.
  2. If the candidate license is invalid or tampered, the active valid license is **NOT overwritten**.
  3. License import requires administrative authorization (`AdministrativePrivilegeChecker`).

---

## 5. Expiry Lifecycle & Non-Destructive Commercial Policy
CBOS enforces strict data-safety rules:
- **Data Protection Guarantee:** CBOS **never** deletes customer data, encrypts existing data, destroys databases, or blocks database backups upon license expiration.
- **In-Flight Transactions:** In-progress financial transactions are never interrupted midway.
- **Lifecycle States:**
  - `Valid`: All operations permitted.
  - `GracePeriod` (14 days post-expiry): Displays warning banner; all operations permitted.
  - `Expired` / `Unlicensed` / `MachineMismatch` / `ClockTampered`:
    - **Read-Only Access Allowed:** Reports, customer receivables, order history, inventory lookup, data export, backups, and software registration.
    - **Transaction Gate:** Creation of new sales orders, payments, or stock adjustments is blocked until a valid license is imported (`LicenseService.CanCreateTransactions()` returns `false`).

### 5.1 30-Day Evaluation / Trial Mode Lifecycle
When a customer chooses **"Continue in Evaluation / Trial Mode (30-day evaluation period)"** during First-Run Commissioning (Step 7):
1. **Zero License File Requirement:** No physical `clovent.lic` file is required during the active 30-day period.
2. **Persistent Anti-Tamper State:**
   - Trial state is persisted in `%ProgramData%\Clovent\BusinessOperatingSystem\License\trial.state` with fallback to `%LocalAppData%`.
   - The file is encrypted using Windows DPAPI (`DataProtectionScope.LocalMachine`) and protected by an HMAC-SHA256 signature binding the machine ID and commissioning start date.
   - Secondary recovery anchor: if `trial.state` is deleted, `TrialStateManager` automatically reconstructs the state using the immutable, signed `commissioning.json` marker.
3. **Immutability & Monotonicity:**
   - Reinstalling, repairing, or restarting CBOS preserves the original trial start date.
   - Monotonic time tracking detects system clock rollbacks (`TrialStateStatus.ClockRollback`).
4. **Commercial License Superseding:**
   - When a valid commercial license is imported, `HasCommercialLicenseEverBeenInstalled` is permanently recorded.
   - An expired commercial license cannot revert the machine back to evaluation mode.
5. **Non-Destructive Post-Expiry:**
   - After Day 30, CBOS notifies the operator with an expired evaluation message.
   - Historical sales, customer records, accounting data, inventory reports, and database backups remain 100% accessible.
   - Only new operational transactions are paused until a valid software license is registered.

---

## 6. Vendor License Issuance Tool (`tools/LicenseIssuer/`)
The vendor license issuer is a standalone CLI tool located in `tools\LicenseIssuer\` and excluded from client distributions.

### Generating a New Production Key Pair:
```powershell
dotnet run --project tools\LicenseIssuer -- generate-keys --out "$env:USERPROFILE\.clovent\keys"
```

### Authoritative Windows Sandbox License Command:
```powershell
dotnet run --project tools\LicenseIssuer -- issue `
    --customer "CBOS Sandbox Test" `
    --company "CBOS Sandbox Test" `
    --type Trial `
    --days 35 `
    --modules POS,BackOffice,Inventory,Catalog,Reporting,Restaurant `
    --terminals 5 `
    --branches 1 `
    --machine-id "<SANDBOX-HARDWARE-ID>" `
    --out "D:\clovent-sandbox.lic"
```

### Issuing a Customer-Specific License:
```powershell
dotnet run --project tools\LicenseIssuer -- issue `
    --customer "Highland Restaurant Group" `
    --company "Highland Dining LLC" `
    --type Subscription `
    --days 365 `
    --modules POS,BackOffice,Inventory,Catalog,Reporting,Restaurant `
    --terminals 5 `
    --branches 2 `
    --machine-id "A3F1-90B2-88C4-E102" `
    --out "licenses\highland_dining.lic"
```

### Private Key Resolution Order:
1. `--key-file <path>` CLI parameter.
2. `CLOVENT_LICENSE_SIGNING_KEY` environment variable.
3. `%USERPROFILE%\.clovent\keys\clovent_vendor_private_key.pem`.
*Any key file path located inside the Git repository is rejected automatically.*

---

## 7. License Renewal & Machine Replacement Workflows
- **License Renewal:** Vendor issues a new `.lic` file with updated `ExpiryDate`. Administrator clicks `Registration & License` in the ribbon, clicks `Import License File...`, and selects the file. The new license takes effect immediately without restarting.
- **Machine Replacement:** Administrator on the new terminal copies the local Hardware ID from `SoftwareRegistrationForm`, submits it to Clovent vendor operations, and imports the newly bound license.
