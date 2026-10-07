# Clovent Business Operating System (CBOS) — ReleaseGuard Security Scanner

> **Standard Header**
> - **Document ID:** REL-SEC-002
> - **Category:** Release Engineering & Automated Security Verification
> - **Target Audience:** Release Engineers, DevSecOps, QA Engineers, Security Auditors
> - **Status:** VALIDATED
> - **Last Updated:** 2026-10-07

---

## 1. Overview & Purpose

**ReleaseGuard** is an automated pre-distribution security scanner implemented in `tools/ReleaseGuard/ScanReleasePackage.ps1`. It acts as the final gatekeeper before any compiled CBOS build is packaged into an installer or distributed to clients.

The mission of ReleaseGuard is to ensure zero credential leakage, zero proprietary source leakage, zero debug artifacts, and strict separation between developer evaluation environments and clean production distributions.

```
┌──────────────────┐     publish     ┌──────────────────┐     ScanReleasePackage.ps1     ┌──────────────────┐
│  Source Control  │ ──────────────> │ Published Bundle │ ─────────────────────────────> │  PASS: Code 0    │
│  (C#, Tests)     │                 │ (win-x64)        │                                │  FAIL: Code 1    │
└──────────────────┘                 └──────────────────┘                                └──────────────────┘
```

---

## 2. Command Invocation

### 2.1 Default Invocation
By default, the script scans `artifacts\release\Clovent.BusinessOperatingSystem-win-x64`:

```powershell
powershell -ExecutionPolicy Bypass -File tools\ReleaseGuard\ScanReleasePackage.ps1
```

### 2.2 Explicit Directory Parameter
To scan an arbitrary target folder or custom staging directory:

```powershell
powershell -ExecutionPolicy Bypass -File tools\ReleaseGuard\ScanReleasePackage.ps1 -ReleaseDir "artifacts\release\Clovent.BusinessOperatingSystem-win-x64"
```

### 2.3 Exit Code Semantics
- **Exit Code `0`:** **PASS** — Zero violations detected; release is clean and approved for packaging.
- **Exit Code `1`:** **FAIL** — One or more critical security violations discovered; process halts and packaging is blocked.

---

## 3. Scanned Rules & Detection Heuristics

ReleaseGuard recursively inspects every file in the target directory across 8 distinct verification categories:

| Rule Category | Pattern / Heuristic | Rationale & Risk |
| :--- | :--- | :--- |
| **1. Private Signing Keys** | `*.privatekey`, `*.pfx`, `*.pem`, `*.key`, `*private_key*.xml`, `*vendor_private_key*` *(Excludes `System.Private.*`)* | Vendor private RSA keys must never leave the build server. Leaking a private key compromises license signature integrity. |
| **2. Development Settings** | `*Development*.json` | Prevents shipping relaxed local developer configurations or insecure local debug endpoints. |
| **3. Development / Test Licenses** | `*development*.lic`, `*test*.lic` | Internal bypass or developer licenses must not be included in production distributions. |
| **4. Shipped Generic Licenses** | `clovent.lic` | Generic production releases must ship **without** an active license. Terminals must be commissioned and licensed independently on-site. |
| **5. Source Code Leakage** | `*.cs`, `*.csproj`, `*.sln`, `*.slnx`, `*.vb`, `*.fs` | Protects intellectual property and prevents accidental inclusion of uncompiled code in published client trees. |
| **6. PDB Debug Symbols** | `*.pdb` | Production releases must not include debug symbols to reduce payload size and protect internal symbol names. |
| **7. Database Backups / Stores** | `*.bak`, `*.mdf`, `*.ldf`, `*.ndf`, `*.sqlite`, `*.db` | Prevents shipping dirty developer test databases or sample customer data. |
| **8. Sensitive Content Inspection** | Text search across `.json`, `.config`, `.xml`, `.txt`, `.lic`:<br>• `Admin123!`<br>• `BEGIN RSA PRIVATE KEY`, `<D>` inside `<RSAKeyValue>`<br>• `Password=...` / `"Password": "..."` | Catches hardcoded test passwords, raw private keys embedded in text, and plaintext SQL database passwords. |

---

## 4. Integration in CI/CD and Local Pre-Release Workflows

### 4.1 Local Release Verification
Before running Inno Setup compiler (`ISCC.exe`), release engineers must execute:

```powershell
# 1. Publish client
dotnet publish src\Clovent.Desktop\Clovent.Desktop.csproj -c Release -r win-x64 --self-contained true -o artifacts\release\Clovent.BusinessOperatingSystem-win-x64

# 2. Run ReleaseGuard
powershell -ExecutionPolicy Bypass -File tools\ReleaseGuard\ScanReleasePackage.ps1 -ReleaseDir artifacts\release\Clovent.BusinessOperatingSystem-win-x64

# 3. Check exit code
if ($LASTEXITCODE -ne 0) {
    Write-Error "ReleaseGuard failed! Blocking packaging."
    exit 1
}
```

### 4.2 GitHub Actions / Azure Pipelines Integration
In continuous integration pipelines, ReleaseGuard runs immediately after the publishing task:

```yaml
- name: Run ReleaseGuard Security Gate
  shell: pwsh
  run: |
    tools/ReleaseGuard/ScanReleasePackage.ps1 -ReleaseDir artifacts/release/Clovent.BusinessOperatingSystem-win-x64
```

---

## 5. Violation Remediation Runbook

When ReleaseGuard fails with exit code `1`, consult the following resolution table:

| Reported Violation | Root Cause | Remediation Step |
| :--- | :--- | :--- |
| `FORBIDDEN KEY FILE: ...` | A developer private key was copied into `src/` or `output/`. | Delete the key immediately. Store private signing keys only in `%USERPROFILE%\.clovent\keys\`. |
| `PDB DEBUG SYMBOL FOUND: ...` | `<DebugType>full</DebugType>` or `<DebugType>pdbonly</DebugType>` emitted symbols. | Update publish command or project file to `<DebugType>none</DebugType>` in Release mode, or add an exclusion target. |
| `SOURCE CODE FILE FOUND: ...` | A `.cs` file was copied as content or marked `<CopyToOutputDirectory>Always</CopyToOutputDirectory>`. | Inspect `.csproj` for erroneous `<Content>` or `<None Include="**\*.cs">` tags and remove them. |
| `UNIVERSAL/UNAUTHORIZED LICENSE ...` | `clovent.lic` was placed in the publish directory. | Remove `clovent.lic` from the release payload. Install licenses post-installation via the Commissioning Wizard. |
| `DEFAULT TEST PASSWORD (Admin123!) ...` | Hardcoded development credential remained in sample JSON or config. | Remove password. Ensure passwords are configured interactively during first-run commissioning. |
| `POTENTIAL PLAINTEXT SQL PASSWORD ...` | Database connection string contains raw password. | Use Windows DPAPI-encrypted credentials (`database.config.json`) or Integrated Windows Authentication. |
