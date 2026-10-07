# Support Runbook: Software License & Clock Tampering Issues

| Attribute | Details |
| :--- | :--- |
| **Area** | Support Runbook / Licensing & Anti-Tamper |
| **Audience** | Level 1–2 Support Technicians, Systems Administrators |
| **Last Reviewed Version** | CBOS 1.2.2 |
| **Status** | **PROCEDURAL RUNBOOK** |
| **Classification** | **PUBLIC-SAFE** |

---

## 1. Problem Description
On application launch, CBOS halts with one of the following notices:
- `"Digital signature verification failed. The license file has been modified or corrupted."`
- `"License is bound to machine '...', but current machine is '...'."`
- `"Clock rollback detected. Operating system clock was adjusted backwards."`
- `"The software evaluation/license has expired. Operating in Read-Only Mode."`

---

## 2. Step-by-Step Diagnostic & Resolution Procedure

### Scenario A: Clock Rollback Detected (`LicenseStatus.ClockTampered`)
1. **Root Cause:** The Windows system clock was manually changed to an earlier date/time, or the CMOS battery died, causing the BIOS clock to reset to default (e.g. year 2000).
2. **Resolution:**
   - Synchronize the Windows system clock with an authoritative network time protocol (NTP) server:
     ```powershell
     w32tm /resync /force
     ```
   - Verify current date and time are accurate in Windows Settings.
   - If the CMOS battery is depleted, replace the CR2032 motherboard battery.
   - Launch CBOS. `LicenseTamperGuard` will verify monotonicity and clear the flag once the clock matches or exceeds the recorded state.

### Scenario B: Machine Mismatch (`LicenseStatus.MachineMismatch`)
1. **Root Cause:** The hard drive was moved to a new PC, or the motherboard/CPU was replaced, altering the hardware fingerprint (`MachineFingerprint`).
2. **Resolution:**
   - Launch CBOS and open the **Software Registration** dialog (`SoftwareRegistrationForm.cs`).
   - Copy the displayed **Current Machine Fingerprint**.
   - Contact Clovent Licensing Support to generate an authorized replacement license bound to the new fingerprint.
   - Import the new `clovent.lic` file.

### Scenario C: Evaluation Trial Expired (`LicenseStatus.Expired`)
1. **Root Cause:** The 30-day evaluation period has concluded.
2. **Current Mode:** CBOS enters **Read-Only Historical Mode**. Store managers can still view reports, verify audits, and perform database backups, but checkout is blocked.
3. **Resolution:**
   - Procure a commercial license key from Clovent sales.
   - Click **Registration** -> **Import License File** -> select `clovent.lic`.
   - The license status immediately updates to `Authorized (Commercial)`, unlocking full operational capabilities without requiring data re-entry.
