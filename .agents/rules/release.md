# Packaging & Release Rules

**Scope:** `artifacts/**`, `tools/ReleaseGuard/**`, publishing, deployment  
**Authoritative Reference:** [AGENTS.md](../../AGENTS.md)

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

## 2. Source Freeze & Exact Accepted Artifact Rule

1. **Source Freeze:** Prior to release qualification, the release branch enters a strict source freeze. No new features, speculative refactors, or unapproved commits are accepted.
2. **Exact Accepted Artifact Rule:** The exact binary bits tested and accepted during Windows Sandbox qualification **ARE** the final released bits. Rebuilding, repacking, or altering binaries after acceptance testing invalidates certification and requires full re-testing.
3. **No Artifact Replacement:** Once an artifact is certified and accepted, it must never be silently replaced or overwritten. Any subsequent change requires a new version identifier.
4. **Tagging Policy:** Git release tags (e.g., `v1.2.3`) must be applied **only** after exact final artifact acceptance and sign-off. Never tag speculative or unverified commits.

---

## 3. Cryptographic Signing & Integrity Verification

1. **Authenticode Signing Bounded to First-Party Deliverables:**
   - Compile binaries in Release mode.
   - Code signing is strictly bounded to approved first-party deliverables (explicitly including `Clovent.Installer.Provisioner.exe`, `Clovent.Desktop.exe`, and approved first-party `Clovent.*.dll`) using Authenticode with RFC 3161 timestamping.
   - Third-party binaries and vendor signatures (e.g., DevExpress, Microsoft, SQLite) must be preserved; never re-sign or modify third-party components.
   - Verify digital signatures on compiled first-party binaries.
   - Package the installer/distribution payload.
   - Sign the final installer executable (`Setup-Clovent.BusinessOperatingSystem-<version>.exe`) with RFC 3161 timestamping.
   - Retain verification, compute final installer SHA-256 hashing in `SHA256SUMS.txt`, and perform clean-machine acceptance testing on the exact signed artifact intended for distribution.
2. **External Signing Material:** Signing private keys and hardware tokens remain strictly outside the repository in protected enterprise key storage.
3. **SHA-256 Integrity Manifest:** Every release package must produce an accompanying SHA-256 manifest (`SHA256SUMS.txt`) documenting cryptographic hashes of all distributable files, including the final signed installer.

---

## 4. Production Build & Publish Pipeline

Certified production releases must be built and published using the standard commands:

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

## 5. Automated ReleaseGuard Validation

The automated scanner (`tools\ReleaseGuard\ScanReleasePackage.ps1`) verifies package hygiene and fails the pipeline if forbidden files or patterns are detected.

### Strictly Excluded From Production Distributions:
1. **Source Code & Solution Files:** `*.cs`, `*.csproj`, `*.sln`, `*.slnx`.
2. **Debug Symbols:** `*.pdb`.
3. **Development Configurations:** `appsettings.Development.json` or any file matching `*Development*.json`.
4. **Development & Test Licenses:** `*development*.lic`, `*test*.lic`.
5. **Universal / Customer Licenses:** Generic client distributions ship **without** `clovent.lic`. Licenses are issued separately per client via licensing tooling.
6. **Private Cryptographic Keys:** `*.pem`, `*.key`, `*.pfx`, `*.cer`, `*.crt`, or RSA private key markers (`<D>`, `BEGIN PRIVATE KEY`).
7. **Database Backups:** `*.bak`, `*.mdf`, `*.ldf`.
8. **Sensitive Plaintext Strings:** `Admin123!`, plaintext SQL connection passwords.

---

## 6. Release Verification Checklist

Before certifying any build for client deployment:
- [ ] `Clovent.Desktop.exe` is present in the release folder.
- [ ] No `clovent.lic` exists in the generic distribution.
- [ ] Zero private keys or developer signing material present.
- [ ] Zero source code files or development JSON files present.
- [ ] Automated ReleaseGuard scan exits with return code 0 (`PASS`).
- [ ] SHA-256 checksum manifest is generated for all distributable files including the installer.
- [ ] Clean Windows Sandbox install test completed successfully on final signed package. (Note: CBOS 1.2.2 clean-machine acceptance is marked as PENDING unless actual runtime acceptance evidence is supplied; a frozen baseline designation is not a passed gate).
