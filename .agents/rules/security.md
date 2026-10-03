# Security & Licensing Rules

**Scope:** Global (Authentication, Identity, Licensing, Commissioning, Protected Storage)  
**Authoritative Reference:** [AGENTS.md](file:///d:/Clovent%20Business%20Operating%20System/AGENTS.md)

---

## 1. Secrets & Credential Management

- **Zero Hardcoded Secrets:** Never commit, log, or hardcode passwords, API keys, or connection strings.
- **Forbidden Test Secrets:** Phrases like `Admin123!`, default cashier PINs, or test credentials must never appear in production configurations or unmasked log entries.
- **Logging Hygiene:**
  - Mask all database passwords (`***`), authorization tokens, and personal PINs in diagnostic logs.
  - PCI-DSS: Cardholder data (PAN, CVV, PIN block, magnetic stripe track data) must **never** be captured, stored, or logged under any circumstances.
  - Local diagnostic logs in `%LocalAppData%\Clovent\Clovent.BusinessOperatingSystem\Logs\` are limited to a 31-day rolling retention.

---

## 2. Protected Storage & Directory Access Control Lists (ACLs)

Shared terminal assets must be secured against unauthorized cashier tampering in `%ProgramData%\Clovent\BusinessOperatingSystem\`:
- `Config\` (`database.config.json`, `commissioning.json`):
  - `Administrators` & `SYSTEM`: Full Control (Read, Write, Delete).
  - `Users` (Standard Cashiers): Read-Only.
- `License\` (`clovent.lic`):
  - `Administrators` & `SYSTEM`: Full Control.
  - `Users`: Read-Only.
- `Logs\` (Rolling diagnostic logs):
  - `Users`: Read/Write/Append, enabling unprivileged cashier sessions to record error diagnostics.

---

## 3. Administrative Authorization & Feature Gates

- **Pre-Login Elevation (Windows UAC):** Before an application user logs in (e.g. at initial setup or connection failure screen), accessing or modifying database connection settings or license files requires Windows Administrator UAC Elevation (`runas`). Standard non-admin Windows users cannot modify machine configuration before login.
- **Post-Login Role Enforcement:** After login, modifying database parameters, server configurations, or software licenses requires that the authenticated user possesses the `Administrator` application role (`AdministrativePrivilegeChecker`).
- **One-Time Admin Bootstrap:** The initial administrator provisioning service permanently disables itself once an active administrator account exists in the database, preventing backdoor privilege escalation.

---

## 4. Cryptographic Software Licensing

- **Cryptographic Model:** Offline asymmetric RSA-2048 with SHA-256 digital signatures (`RSASignaturePadding.Pkcs1`).
- **Cryptographic Key Separation:**
  - **Embedded Verification Public Key:** Embedded within `Clovent.Desktop.Licensing.LicenseKeys` assembly for zero-network offline verification. Active key identifier: `clovent-2026-v2`. Legacy revoked key: `clovent-2026-v1`.
  - **Vendor Private Signing Key:** Kept strictly outside the Git repository in `%USERPROFILE%\.clovent\keys\clovent_vendor_private_key.pem`. **Never committed to version control, never copied to client machines, and never embedded in release binaries.**
- **Customer-Specific Licensing:**
  - Generic releases ship **without** an active license file (`clovent.lic` excluded).
  - Licenses are issued per customer/company using `tools\LicenseIssuer\`.
- **Pre-Copy Import Validation:** When importing a new license (`LicenseService.ImportLicense`), the candidate file is cryptographically validated **before** copying to protected storage. If invalid or tampered, the active valid license is preserved.
- **Non-Destructive Expiry Guarantee:**
  - CBOS **never** deletes customer data, encrypts existing records, destroys databases, or blocks database backups upon license expiration.
  - Expired licenses permit read-only operations (financial reports, customer receivables, order history, inventory lookups, data export, backups).
  - Creation of new commercial sales orders, payments, or inventory adjustments is gated until a renewed license is imported (`LicenseService.CanCreateTransactions()` returns `false`).
