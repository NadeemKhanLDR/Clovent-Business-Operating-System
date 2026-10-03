# Clovent Business Operating System - Production Release & Build Guide

## 1. Build & Release Targets
- **Target Framework:** .NET 10 (`net10.0-windows`)
- **Runtime Identifier:** `win-x64`
- **Output Mode:** Self-contained
- **Standard Generic Release Directory:**
  `artifacts\release\Clovent.BusinessOperatingSystem-win-x64\`
- **Versioned Distributable Directory:**
  `artifacts\release\Clovent.BusinessOperatingSystem-1.0.0-win-x64\`

---

## 2. Production Build & Publish Pipeline

To produce a certified, hardened production release:

```powershell
# 1. Clean previous release artifacts
if (Test-Path "artifacts\release") {
    Remove-Item -Recurse -Force "artifacts\release"
}

# 2. Build Debug configuration and execute test suites
dotnet build Clovent.BusinessOperatingSystem.slnx -c Debug
dotnet test src\Clovent.Desktop.Tests\Clovent.Desktop.Tests.csproj --filter "FullyQualifiedName~Security"

# 3. Build Release configuration
dotnet build Clovent.BusinessOperatingSystem.slnx -c Release

# 4. Publish self-contained win-x64 binary tree
dotnet publish "src\Clovent.Desktop\Clovent.Desktop.csproj" `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -o "artifacts\release\Clovent.BusinessOperatingSystem-win-x64"

# 5. Clean non-production artifacts
Remove-Item "artifacts\release\Clovent.BusinessOperatingSystem-win-x64\appsettings.Development.json" -ErrorAction SilentlyContinue
Remove-Item "artifacts\release\Clovent.BusinessOperatingSystem-win-x64\*.pdb" -ErrorAction SilentlyContinue

# 6. Execute Automated Security Guard Scan
powershell -ExecutionPolicy Bypass -File "tools\ReleaseGuard\ScanReleasePackage.ps1" `
    -ReleaseDir "artifacts\release\Clovent.BusinessOperatingSystem-win-x64"

# 7. Create Versioned Distributable Archive
$version = "1.0.2"
$versionedDir = "artifacts\release\Clovent.BusinessOperatingSystem-$version-win-x64"
Copy-Item -Recurse "artifacts\release\Clovent.BusinessOperatingSystem-win-x64" $versionedDir
Compress-Archive -Path "$versionedDir\*" -DestinationPath "artifacts\release\Clovent.BusinessOperatingSystem-$version-win-x64.zip" -Force
```

---

## 3. Automated Release Packaging Guard (`tools\ReleaseGuard\ScanReleasePackage.ps1`)
The automated release guard inspects the published directory and fails the pipeline if any of the following are detected:
- [x] **Private Signing Keys & Certificates:** `*private*`, `*.pfx`, `*.pem`, `*.key`, `*.cer`, `*.crt`, or RSA private key markers (`<D>`, `BEGIN PRIVATE KEY`).
- [x] **Development Configuration:** `appsettings.Development.json` or `*Development*.json`.
- [x] **Development & Test Licenses:** `*development*.lic`, `*test*.lic`.
- [x] **Universal Wildcard License:** `clovent.lic` is excluded from generic distributions.
- [x] **Source Code Files:** `*.cs`, `*.csproj`, `*.sln`, `*.slnx`.
- [x] **Debug Symbols:** `*.pdb`.
- [x] **Database Backups:** `*.bak`.
- [x] **Sensitive Strings & Passwords:** `Admin123!`, plain-text SQL connection string passwords.

---

## 4. Verification Checklist
Before shipping:
- [x] Generic release contains `Clovent.Desktop.exe`.
- [x] Generic release contains NO `clovent.lic`.
- [x] Zero private keys or developer signing certificates in release.
- [x] Zero source code or development settings files.
- [x] Target machine does not require Visual Studio, .NET SDK, or developer tools.
- [x] Automated release security scan exits with code 0 (`PASS`).
