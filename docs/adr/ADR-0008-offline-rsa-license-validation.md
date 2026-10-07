# ADR-0008: Offline Asymmetric RSA-2048 Cryptographic Licensing

| Attribute | Details |
| :--- | :--- |
| **Status** | **ACCEPTED** |
| **Date** | 2026-09-20 |
| **Deciders** | Security & Licensing Working Group |
| **Area** | Security, Licensing & Anti-Piracy |

---

## 1. Context & Problem Statement

Commercial retail and restaurant environments frequently operate in air-gapped locations, basement food courts, remote outlets, or areas with unreliable internet connectivity.

Online licensing architectures (such as cloud heartbeats or periodic token refresh servers) cause operational failures when internet connections drop. If a cloud licensing server experiences downtime, licensed customer stores are prevented from selling.

Conversely, simple symmetric keys, license strings, or unencrypted local registration flags are easily bypassed, reverse-engineered, or pirated.

---

## 2. Decision

CBOS establishes an **Offline Asymmetric Cryptographic Licensing Architecture**:
1. **Asymmetric Cryptography:** Licenses are signed using RSA-2048 with SHA-256 (`clovent-2026-v2`).
2. **Key Separation:**
   - The public verification key is embedded directly into the desktop client assembly (`LicenseService.cs`).
   - The private signing key is stored strictly offline by the vendor within secure key vaults and never ships with the application.
3. **Hardware Binding:** Commercial licenses bind to unique machine fingerprints (`MachineFingerprint.cs`) derived from motherboard UUID, CPU identity, and primary disk serial.
4. **Tamper Guard & Clock Monotonicity:** `LicenseTamperGuard` tracks monotonic timestamps in an encrypted file (`license_guard.dat`) protected with Windows DPAPI and HMAC-SHA256, detecting system clock rollbacks.
5. **Non-Destructive Expiration Policy:** When a commercial or evaluation license expires, CBOS transitions into **Read-Only Historical Mode**. Customers retain permanent access to historical financial reports and data exports, but new operational checkouts are blocked. Customer data is never deleted, locked, or held hostage.

---

## 3. Consequences

### Positive
- 100% offline license validation: customer stores never stop selling due to cloud server outages or internet cuts.
- Cryptographically secure: licenses cannot be forged without possession of the vendor's offline private key.
- Ethical and compliant: non-destructive expiration guarantees customer accounting records remain accessible.

### Negative / Trade-offs
- Seat additions or license renewals require importing a newly generated license file (`.lic`) provided by the vendor.
- Hardware motherboard replacements require an updated license key issued by support.
