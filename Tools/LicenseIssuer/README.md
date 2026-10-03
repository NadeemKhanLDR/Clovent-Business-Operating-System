# Clovent Vendor License Issuer Tool (`Clovent.LicenseIssuer`)

> **INTERNAL VENDOR TOOL ONLY**  
> This tool is strictly maintained for Clovent vendor operations, key management, and cryptographic software license generation. It **must never** be distributed to end clients, published in client releases, or referenced by `Clovent.Desktop`.

---

## 1. Architectural Overview

Clovent Business Operating System employs an offline, asymmetric cryptographic licensing system:
- **Algorithm:** RSA-2048 with SHA-256 digital signature (`RSASignaturePadding.Pkcs1`).
- **Signing Key (Private):** Held strictly by the software vendor and stored in secure, external storage outside the Git repository (`%USERPROFILE%\.clovent\keys\`).
- **Verification Key (Public):** Embedded within `Clovent.Desktop.Licensing.LicenseKeys` (`PublicKeyXml` / `PublicKeyPem`) for zero-network offline verification.
- **Key Identifiers:**
  - `clovent-2026-v2`: Active production key pair.
  - `clovent-2026-v1`: Revoked legacy key pair.

---

## 2. Security & Guardrails

The License Issuer tool enforces strict guardrails to prevent accidental repository pollution or private key leakage:
1. **Repository Boundary Enforcement:** The tool inspects file paths and actively rejects any command attempting to generate, read, or load a private key residing inside the Git repository tree.
2. **External Key Directory:** By default, all generated private and public key files are stored in the user profile directory:
   - Windows: `%USERPROFILE%\.clovent\keys\` (e.g. `C:\Users\<User>\.clovent\keys\`)
3. **Repository `.gitignore` Hardening:** The repository root `.gitignore` ignores all private key extensions (`*.privatekey`, `*.pem`, `*.pfx`, `*.key`, `*vendor_private_key*`, `*private_key*`, `tools/LicenseIssuer/keys/`, `licenses/`, and `*.lic`).

---

## 3. Prerequisites & Build

- **SDK:** .NET 10.0 SDK or later.
- **Build Command:**
  ```powershell
  dotnet build tools\LicenseIssuer\Clovent.LicenseIssuer.csproj
  ```

---

## 4. Key Management: Generating Key Pairs

To generate a new RSA-2048 key pair:

```powershell
dotnet run --project tools\LicenseIssuer -- generate-keys
```

### Options:
- `--out <path>` *(optional)*: Custom directory to output the generated keys. Must reside **outside** the Git repository. If omitted, defaults to `%USERPROFILE%\.clovent\keys\`.

### Generated Artifacts:
| File | Format | Description |
|---|---|---|
| `clovent_vendor_private_key.pem` | PKCS#8 PEM | Vendor private signing key. |
| `clovent_vendor_private_key.xml` | XML (`<RSAKeyValue>`) | Vendor private signing key (XML). |
| `clovent_public_key.pem` | SubjectPublicKeyInfo PEM | Public key for offline verification. |
| `clovent_public_key.xml` | XML (`<RSAKeyValue>`) | Public key formatted for `LicenseKeys.PublicKeyXml`. |
| `key_metadata.json` | JSON | Key metadata with Key ID (`clovent-2026-v2`) and revoked keys. |

---

## 5. Private Key Loading Precedence

When issuing licenses, the tool searches for the private signing key in the following priority order:

1. **Explicit `--key-file <path>`**:
   Direct path specified via CLI argument.
2. **Environment Variable `CLOVENT_LICENSE_SIGNING_KEY`**:
   Can be an external file path or raw inline PEM/XML key string (useful in secure CI/CD pipelines).
3. **Standard External Path**:
   `%USERPROFILE%\.clovent\keys\clovent_vendor_private_key.pem` (fallback to `.xml`).

> **Security Note:** If any detected key path resides inside the Git repository directory, the tool immediately aborts execution with a `[SECURITY VIOLATION]` error.

---

## 6. Issuing Licenses (`issue`)

### Command Syntax:
```powershell
dotnet run --project tools\LicenseIssuer -- issue \
    --customer <name> \
    --company <name> \
    --type <Trial|Subscription|Perpetual> \
    --days <n> \
    --modules <m1,m2,...> \
    --terminals <n> \
    --branches <n> \
    [--machine-id <id>] \
    [--maintenance-days <n>] \
    [--out <output.lic>] \
    [--key-file <path>]
```

### Parameter Reference:
| Option | Type | Required | Description |
|---|---|---|---|
| `--customer` | String | **Yes** | Primary customer / licensee contact name. |
| `--company` | String | **Yes** | Legal business / company name. |
| `--type` | String | **Yes** | License tier: `Trial`, `Subscription`, or `Perpetual`. |
| `--days` | Integer | Conditional | Days valid. For `Perpetual`, defaults to 36500 (100 years). |
| `--modules` | String | **Yes** | Comma-separated module names (e.g. `POS,BackOffice,Inventory,Catalog,Reporting,Restaurant`). |
| `--terminals` | Integer | **Yes** | Max terminal limit (`0` = unlimited). |
| `--branches` | Integer | **Yes** | Max branch limit (`0` = unlimited). |
| `--machine-id` | String | No | Hardware machine fingerprint (node-locking). Leave blank for floating licenses. |
| `--maintenance-days`| Integer | No | Number of days maintenance & updates are included. |
| `--out` | String | No | Output `.lic` file path (default: `clovent.lic`). |
| `--key-file` | String | No | Custom path to external private key file. |

---

## 7. Examples

### Example A: Commercial Annual Subscription
```powershell
dotnet run --project tools\LicenseIssuer -- issue `
    --customer "John Smith" `
    --company "Highland Retailers Ltd" `
    --type Subscription `
    --days 365 `
    --modules POS,BackOffice,Inventory,Catalog,Reporting,Restaurant `
    --terminals 5 `
    --branches 2 `
    --out "licenses\highland_retailers.lic"
```

### Example B: Perpetual License Bound to Workstation
```powershell
dotnet run --project tools\LicenseIssuer -- issue `
    --customer "Alice Johnson" `
    --company "Grand Hotel & Suites" `
    --type Perpetual `
    --modules POS,BackOffice,Inventory,Reporting,Restaurant `
    --terminals 10 `
    --branches 1 `
    --machine-id "A3F1-90B2-88C4-E102" `
    --maintenance-days 365 `
    --out "licenses\grand_hotel.lic"
```

### Example C: 30-Day Evaluation / Trial
```powershell
dotnet run --project tools\LicenseIssuer -- issue `
    --customer "Demo User" `
    --company "Apex Solutions" `
    --type Trial `
    --days 30 `
    --modules POS,Restaurant `
    --terminals 1 `
    --branches 1 `
    --out "licenses\apex_trial.lic"
```

---

## 8. Verifying Licenses (`verify`)

To verify the signature and validity of any issued `.lic` file:

```powershell
# Using default external public key (%USERPROFILE%\.clovent\keys\clovent_public_key.pem)
dotnet run --project tools\LicenseIssuer -- verify --license "licenses\highland_retailers.lic"

# Using explicit public key file
dotnet run --project tools\LicenseIssuer -- verify `
    --license "licenses\highland_retailers.lic" `
    --public-key "%USERPROFILE%\.clovent\keys\clovent_public_key.pem"
```

---

## 9. Canonical Payload Specification

The canonical signing payload is generated in UTF-8 bytes using the format:
```
{LicenseId:D}|{CustomerName.Trim()}|{CompanyName.Trim()}|{LicenseType.Trim()}|{IssueDate:O}|{ExpiryDate:O}|{TerminalLimit}|{modules}|{machine}
```
Where:
- `modules`: Comma-separated list of `AllowedModules` sorted alphabetically (`string.Join(",", AllowedModules.OrderBy(x => x))`).
- `machine`: `MachineId?.Trim() ?? string.Empty`.
- Date formatting: Round-trip ISO 8601 (`:O`), whole-second aligned.
