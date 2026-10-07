# CBOS Threat Model & Security Risk Analysis

| Attribute | Details |
| :--- | :--- |
| **Area** | Security Risk Assessment & Vulnerability Analysis |
| **Audience** | Security Officers, Penetration Testers, Architects |
| **Last Reviewed Version** | CBOS 1.2.2 |
| **Status** | **ASSESSED & CURRENT** |
| **Classification** | **PUBLIC-SAFE** |

---

## 1. Threat Model Overview

This threat model analyzes Clovent Business Operating System (CBOS) across all attack surfaces using the Microsoft **STRIDE** methodology (Spoofing, Tampering, Repudiation, Information Disclosure, Denial of Service, Elevation of Privilege).

---

## 2. Protected Assets & Threat Actors

### 2.1 Protected Assets
1. **Financial Sales & Ledger Data:** Customer order records, payment balances, and customer accounts receivable.
2. **Database Credentials:** SQL Server user accounts and connection strings.
3. **Software Entitlements:** Commercial RSA license files and trial state files.
4. **Offline Resilience Stores:** Local operational cache and append-only emergency cash journal.
5. **Audit Trails:** Failed login attempts, shift variances, and authentication events.

### 2.2 Threat Actors
- **Rogue Cashier / Insider:** Motivated to steal cash, forge voids, or manipulate customer balances.
- **Untrusted Local User:** Unauthorized user on the local workstation attempting to extract passwords or modify data files.
- **LAN Attacker:** Attacker on the local restaurant network attempting SQL injection, network sniffing, or brute-force access.
- **Software Pirate:** Adversary attempting to bypass license expiration or counterfeit commercial licenses.

---

## 3. STRIDE Threat Analysis Matrix

| STRIDE Category | Threat Description | Attack Vector | Mitigations in CBOS 1.2.2 | Residual Risk / Status |
| :--- | :--- | :--- | :--- | :--- |
| **Spoofing** | Attacker impersonates an authorized cashier or manager. | Guessing short 4-digit cashier PINs. | 5-attempt brute-force lockout with 15-minute freeze (`[Authentication].[LoginAttempts]`). Monitored via audit log. | Low. Biometric authentication planned for future. |
| **Spoofing** | Attacker fakes commercial software license. | Crafting counterfeit license files. | Asymmetric RSA-2048 with SHA-256 signature verification. Private key held strictly offline by vendor. | Mitigated. |
| **Tampering** | Operator edits menu prices offline in the cache. | Hex editing `operational_cache.dat`. | Windows DPAPI encryption (`LocalMachine`) + HMAC-SHA256 signature verification. Tampered caches are rejected immediately. | Mitigated. |
| **Tampering** | Operator edits local cash sales in offline journal. | Modifying `continuity_journal.dat` to hide cash. | Encrypted via DPAPI with per-entry HMAC signatures. Signature failure halts replay and alerts management. | Mitigated. |
| **Tampering** | User rolls back system clock to extend trial/license. | Adjusting Windows Date & Time in Control Panel. | `LicenseTamperGuard` tracks monotonic timestamps in DPAPI-protected storage. Clock rollback halts operations. | Mitigated. |
| **Repudiation** | Cashier denies taking cash or voiding order lines. | Claiming another cashier performed action. | Mandatory binding of `UserId` to each `CashMovement`, `Payment`, and manager override. Monitored in audit logs. | Mitigated. |
| **Information Disclosure** | Local user steals SQL connection credentials. | Reading application config files on disk. | Credentials in `database.config.json` encrypted using Windows DPAPI. ProgramData DACLs restrict unauthenticated access. | Mitigated. |
| **Information Disclosure** | Network sniffing extracts cardholder data. | Capturing LAN packets between POS and server. | Zero cardholder PAN or CVV handled by CBOS (external merchant terminals only). TLS encryption recommended on port 1433. | Mitigated. |
| **Denial of Service** | Database server unreachable during dinner rush. | Network cable cut or server reboot. | Emergency Continuity Mode activates automatically, permitting cash-only sales against local cache. | Mitigated. |
| **Denial of Service** | Printer jam halts cashier checkout. | Thermal printer out of paper. | Transactional Outbox decouples printing from checkout; cashier checkout completes instantly without blocking. | Mitigated. |
| **Elevation of Privilege** | Cashier executes manager-level order voids or discounts. | Bypassing UI disabled buttons. | Manager authorization modal requires supervisor PIN. Pre-login config requires Windows UAC elevation. | Partial. Application-layer mediator pipeline authorization expanding. |

---

## 4. Known Unresolved Threats & Architectural Roadmap

1. **Local Admin Token Access:**
   - *Threat:* An attacker with local Windows Administrator privileges on the POS PC can execute code under the machine account, potentially allowing DPAPI decryption of local files.
   - *Mitigation:* POS workstations must run under locked-down standard user accounts (Kiosk/POS role), with Windows Defender and BitLocker full-disk encryption enabled.
2. **Plaintext Network TDS Traffic on Unconfigured SQL Instances:**
   - *Threat:* If a system administrator configures SQL Server without enabling forced TLS encryption, network traffic across LAN port 1433 is unencrypted.
   - *Mitigation:* Production deployment guides recommend enabling "Force Encryption = Yes" in SQL Server Configuration Manager.

---

## 5. Key Classes & Source Traceability

- **Tamper Guard:** `src/Clovent.Desktop/Licensing/LicenseTamperGuard.cs`
- **Cache Integrity:** `src/Clovent.Restaurant.Infrastructure/Continuity/ProtectedOperationalCacheStore.cs`
- **Journal Integrity:** `src/Clovent.Restaurant.Infrastructure/Continuity/ProtectedContinuityJournalStore.cs`
- **ACL Manager:** `src/Clovent.Desktop/Commissioning/Security/ProgramDataAclManager.cs`
- **Lockout Manager:** `src/Clovent.Authentication/LoginAttempts/`

---

## 6. Cross References
- [Security Architecture](security-architecture.md)
- [Authentication Architecture](authentication.md)
- [Software Licensing](licensing.md)
- [ReleaseGuard Documentation](../release/releaseguard.md)
