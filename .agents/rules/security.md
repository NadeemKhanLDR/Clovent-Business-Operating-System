# Security & Licensing Rules

**Scope:** Global (Authentication, Identity, Licensing, Commissioning, Protected Storage)  
**Authoritative Reference:** [AGENTS.md](../../AGENTS.md)

---

## 1. Secrets & Credential Management

- **Zero Hardcoded Secrets:** Never commit, log, or hardcode passwords, API keys, or connection strings.
- **Forbidden Test Secrets:** Phrases like `Admin123!`, default cashier PINs, or test credentials must never appear in production configurations, databases, or unmasked log entries.
- **No Production Development Seeds:** Development seed startup tasks (e.g. sample data, default accounts, test credentials) must be strictly gated and disabled in production environments.
- **Logging Hygiene:**
  - Mask all database passwords (`***`), authorization tokens, and personal PINs in diagnostic logs.
  - PCI-DSS: Cardholder data (Primary Account Number [PAN], CVV/CVC, PIN block, magnetic stripe track data) must **never** be captured, stored, or logged under any circumstances.
  - Local diagnostic logs in `%LocalAppData%\Clovent\Clovent.BusinessOperatingSystem\Logs\` are limited to a 31-day rolling retention.
- **No Private Keys in Repository:** Private signing keys (`*.pem`, `*.key`, `*.pfx`) must never be placed inside the repository, client builds, or distribution installers.

---

## 2. Authentication & Brute-Force Protection

- **Bounded Brute-Force Protection:** Interactive authentication mechanisms (passwords and PINs) must implement bounded brute-force defense, including rate limiting, exponential backoff, or temporary account/terminal lockout upon repeated failed attempts.
- **Generic Authentication Failure Responses:** Authentication errors must return generic failure messages without revealing whether a username or PIN exists.
- **Credential Storage:** All stored passwords and PINs must be cryptographically hashed using industry-standard, salted algorithms (e.g., PBKDF2 with SHA-256 or BCrypt).

---

## 3. Authorization & Privilege Governance

- **Authorization Fails Closed:** If an authorization check encounters an error, a missing service, an unmapped permission, or an unauthenticated session, access must be denied immediately. Systems must never fail open. Missing services or failed authorization checks deny access.
- **Administrative Operation Protections:**
  - **Pre-Login Sensitive Changes:** Before application login (e.g. initial commissioning, pre-login database configuration, or license setup), sensitive database configuration and software license changes require Windows administrator elevation (UAC).
  - **Post-Login Sensitive Changes:** After application login, sensitive configuration changes require authenticated application Administrator authorization (`AdministrativePrivilegeChecker` evaluating authenticated application roles and permissions).
  - **OS Access Control Invariant:** Application role checks do not bypass OS or filesystem access controls (NTFS ACLs). Both application-layer authorization and operating system ACLs must be satisfied.
  - **Missing Services Fail Closed:** Missing services, unconfigured authorization providers, or failed authorization checks deny access immediately.
- **No Hardcoded Administrative Usernames:** Authorization decisions must evaluate authenticated user roles and granular permissions. Fast-path elevation based on literal usernames (such as `"admin"` or `"administrator"`) is strictly forbidden.
- **UI Visibility Is Not Authorization:** Hiding or disabling UI controls (ribbon buttons, menu items, views) is a usability feature, not security enforcement. Authoritative authorization must be enforced independently at the application layer (`MediatR` pipeline or service boundary).
- **Action-Specific Manager Elevation:** Managerial overrides (price overrides, discount limits, credit limit overrides, voids) must challenge the operator for distinct managerial credentials and evaluate permissions specific to that action. If the authorization service is unavailable, the challenge must fail closed.
- **One-Time Admin Bootstrap:** The initial administrator provisioning endpoint permanently disables itself once an active administrator account exists in the database.

---

## 4. Protected Storage & Directory Access Control Lists (ACLs)

Shared terminal assets must be secured against unauthorized tampering in `%ProgramData%\Clovent\BusinessOperatingSystem\`:
- `Config\` (`database.config.json`, `commissioning.json`):
  - `Administrators` & `SYSTEM`: Full Control (Read, Write, Delete).
  - `Users` (Standard Cashiers): Read-Only.
- `License\` (`clovent.lic`):
  - `Administrators` & `SYSTEM`: Full Control.
  - `Users`: Read-Only.
- `Logs\` (Rolling diagnostic logs):
  - `Users`: Read/Write/Append, enabling unprivileged cashier sessions to record error diagnostics.

---

## 5. Cryptographic Software Licensing

- **Cryptographic Model:** Offline asymmetric RSA-2048 with SHA-256 digital signatures (`RSASignaturePadding.Pkcs1`).
- **Cryptographic Key Separation:**
  - **Embedded Verification Public Key:** Embedded within client assemblies for zero-network offline verification.
  - **Vendor Private Signing Key:** Kept strictly outside the Git repository in `%USERPROFILE%\.clovent\keys\`. **Never committed to version control, never copied to client machines, and never embedded in release binaries.**
- **Customer-Specific Licensing:**
  - Generic releases ship **without** an active license file (`clovent.lic` excluded).
  - Licenses are issued per customer/company using dedicated licensing tooling.
- **Pre-Copy Import Validation:** When importing a new license, the candidate file must be cryptographically validated **before** copying to protected storage. If invalid or tampered, the active valid license is preserved.
- **Non-Destructive Expiry Policy & RBAC Enforcement:**
  - Expiry must never destroy data or remove authorized historical access. CBOS never deletes customer data, encrypts existing records, destroys databases, or blocks database backups upon license expiration.
  - **Authentication and RBAC Remain Strictly Enforced:** Authentication and role-based access control (RBAC) remain enforced regardless of license expiration status. Users must authenticate with valid credentials, and permissions continue to restrict administrative actions and historical report views.
  - **Separation of Accepted Policy from Source-Verified Expiry Behavior:**
    - *Accepted Policy (PDR-0002):* Expired licenses restrict operations to read-only historical inspection (financial reports, customer receivables, order history, inventory lookups, data export, backups), while gating the creation of new commercial sales orders, payments, or inventory movements.
    - *Source-Verified Expiry Behavior (1.2.2 Baseline):* In the 1.2.2 baseline source code (`Program.cs`), an expired license issues an informational warning dialog alerting the user to read-only mode and proceeds to normal authentication without destroying data. An expiry warning alone does not establish read-only transaction gating; this is recorded as a known gap without implementing or assigning new release scope.
