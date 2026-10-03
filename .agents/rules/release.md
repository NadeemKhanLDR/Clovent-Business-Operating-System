# Packaging & Release Rules

**Scope:** `artifacts/**`, `tools/ReleaseGuard/**`, publishing, deployment  
**Authoritative Reference:** [AGENTS.md](file:///d:/Clovent%20Business%20Operating%20System/AGENTS.md)

---

## 1. Release Targets & Runtime Environment

- **Target Framework:** .NET 10 (`net10.0-windows`)
- **Runtime Identifier:** `win-x64`
- **Output Mode:** Self-contained executable package (`--self-contained true`)
- **Client Prerequisite Policy:** Client workstations must **never** require Visual Studio, the .NET SDK, or developer tools to execute CBOS.
- **Canonical Release Output Directory:**
  `artifacts\release\Clovent.BusinessOperatingSystem-win-x64\`
- **Versioned Distributable Directory:**
  `artifacts\release\Clovent.BusinessOperatingSystem-<version>-win-x64\`

---

## 2. Production Build & Publish Pipeline

Certified production releases must be built and published using the following standard commands:

```powershell
# 1. Build Release configuration across all projects
dotnet build Clovent.BusinessOperatingSystem.slnx -c Release

# 2. Publish self-contained win-x64 package
dotnet publish "src\Clovent.Desktop\Clovent.Desktop.csproj" `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -o "artifacts\release\Clovent.BusinessOperatingSystem-win-x64"

# 3. Clean non-production artifacts
Remove-Item "artifacts\release\Clovent.BusinessOperatingSystem-win-x64\appsettings.Development.json" -ErrorAction SilentlyContinue
Remove-Item "artifacts\release\Clovent.BusinessOperatingSystem-win-x64\*.pdb" -ErrorAction SilentlyContinue

# 4. Execute Automated Security Guard Scan
powershell -ExecutionPolicy Bypass -File "tools\ReleaseGuard\ScanReleasePackage.ps1" `
    -ReleaseDir "artifacts\release\Clovent.BusinessOperatingSystem-win-x64"
```

---

## 3. Automated ReleaseGuard Validation

The automated scanner (`tools\ReleaseGuard\ScanReleasePackage.ps1`) verifies package hygiene and fails the pipeline if forbidden files or patterns are detected.

### Strictly Excluded From Production Distributions:
1. **Source Code & Solution Files:** `*.cs`, `*.csproj`, `*.sln`, `*.slnx`.
2. **Debug Symbols:** `*.pdb`.
3. **Development Configurations:** `appsettings.Development.json` or any file matching `*Development*.json`.
4. **Development & Test Licenses:** `*development*.lic`, `*test*.lic`.
5. **Universal / Customer Licenses:** Generic client distributions ship **without** `clovent.lic`. Licenses are issued separately per client via `tools\LicenseIssuer\`.
6. **Private Cryptographic Keys:** `*.pem`, `*.key`, `*.pfx`, `*.cer`, `*.crt`, or RSA private key markers (`<D>`, `BEGIN PRIVATE KEY`).
7. **Database Backups:** `*.bak`, `*.mdf`, `*.ldf`.
8. **Sensitive Plaintext Strings:** `Admin123!`, plaintext SQL connection passwords.

---

## 4. Release Verification Checklist

Before certifying any build for client deployment:
- [ ] `Clovent.Desktop.exe` is present in the release folder.
- [ ] No `clovent.lic` exists in the generic distribution.
- [ ] Zero private keys or developer signing material present.
- [ ] Zero source code files or development JSON files present.
- [ ] Automated scan exits with return code 0 (`PASS`).
