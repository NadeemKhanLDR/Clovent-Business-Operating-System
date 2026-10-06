# Clovent Business Operating System - Vendor License Issuer Operations Guide

This operational manual outlines vendor-side procedures for cryptographic key custody, key rotation, and license file generation for Clovent Business Operating System (CBOS).

---

## 1. Cryptographic Key Custody & Security Policy

The security of the entire CBOS licensing subsystem depends on the absolute confidentiality of the **Vendor Private RSA Signing Key**:

1. **Air-Gapped / Offline Custody:**  
   The production signing private key (`clovent-2026-v2`) must be maintained **EXCLUSIVELY** on an offline, air-gapped vendor workstation or inside a dedicated Hardware Security Module (HSM) / cloud secret vault (e.g. Azure Key Vault / AWS KMS).
2. **Strict Developer Isolation:**  
   Standard developer workstations **NEVER** possess or require the production private key. Everyday development and CI/CD builds use the embedded public key for verification only.
3. **Repository Exclusion Guarantee:**  
   The private signing key is **NEVER** placed in the source repository, never committed to Git, and never packaged in client releases. `ScanReleasePackage.ps1` actively blocks releases if private key markers are detected.

---

## 2. Key Ring & Versioning Architecture

CBOS supports multi-key verification through the `KeyId` attribute in each license. `LicenseKeys.cs` defines trusted public verification keys:

| Key ID | Status | Effective Date | Notes |
|---|---|---|---|
| **`clovent-2026-v2`** | **Active Production Key** | 01-Oct-2026 | Standard RSA-2048 signing key used for all client deployments. |
| **`clovent-2026-v1`** | **Revoked Legacy Key** | 01-Jan-2026 | Deprecated and marked explicitly revoked. All licenses signed with this key are rejected. |

---

## 3. Key Rotation Procedure

When rotating to a new vendor signing key (e.g. `clovent-2027-v1`):

1. **Step 1: Generate New Key Pair:**
   On the offline vendor signing workstation:
   ```powershell
   dotnet run --project Tools\LicenseIssuer -- generate-keys --keyid "clovent-2027-v1" --out "C:\SecureVault\Keys"
   ```
2. **Step 2: Update Trusted Key Ring in CBOS Source:**
   Add the new public XML key to `LicenseKeys.cs`:
   ```csharp
   private static readonly Dictionary<string, (string PublicKeyXml, bool IsRevoked)> TrustedKeyRing = new()
   {
       ["clovent-2026-v1"] = (PublicKeyV1, true),   // Revoked
       ["clovent-2026-v2"] = (PublicKeyV2, false),  // Active
       ["clovent-2027-v1"] = (PublicKeyV3, false),  // New active
   };
   ```
3. **Step 3: Phased Re-signing:**
   - Existing active client licenses signed with `clovent-2026-v2` continue to function until their natural renewal.
   - All newly issued or renewed licenses are signed using `clovent-2027-v1`.
4. **Step 4: Deprecation of Old Key:**
   Once all clients have upgraded and renewed, mark `clovent-2026-v2` as `IsRevoked = true` in the subsequent release.

---

## 4. Issuing Customer Licenses

To issue a license file for a customer:

### Authoritative Windows Sandbox Acceptance Command:
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

### Terminal-Locked Commercial Subscription:
```powershell
dotnet run --project tools\LicenseIssuer -- issue `
    --customer "Grandview Hospitality LLC" `
    --company "Grandview Flagship" `
    --type Subscription `
    --days 365 `
    --terminals 5 `
    --branches 1 `
    --machine-id "D7E2-90FA-B841-33C0" `
    --modules POS,Catalog,Inventory,Restaurant,Reporting,BackOffice `
    --out "C:\Licenses\Grandview_clovent.lic"
```

### Floating Enterprise Perpetual License:
```powershell
dotnet run --project tools\LicenseIssuer -- issue `
    --customer "Acme Enterprises" `
    --company "Acme Dining" `
    --type Perpetual `
    --days 36500 `
    --terminals 20 `
    --branches 5 `
    --modules POS,BackOffice,Inventory,Catalog,Reporting,Restaurant `
    --out "C:\Licenses\Acme_clovent.lic"
```
*(Omitting `--machine-id` creates a floating site license valid across terminals).*
