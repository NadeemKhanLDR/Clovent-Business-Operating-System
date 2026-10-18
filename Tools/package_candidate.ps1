# tools\package_candidate.ps1
$ErrorActionPreference = "Stop"

$repoRoot = $PSScriptRoot | Split-Path -Parent
Set-Location $repoRoot

$relDir = Join-Path $repoRoot "artifacts\release\Clovent.BusinessOperatingSystem-win-x64"
$provDir = Join-Path $repoRoot "tools\Clovent.Installer.Provisioner\bin\publish"
$installerDir = Join-Path $repoRoot "artifacts\installer"

if (Test-Path $relDir) { Remove-Item $relDir -Recurse -Force }
if (Test-Path $provDir) { Remove-Item $provDir -Recurse -Force }
if (-not (Test-Path $installerDir)) { New-Item -ItemType Directory -Path $installerDir -Force | Out-Null }

Write-Host "=== 1. Publishing Clovent.Desktop (Release win-x64 self-contained) ==="
dotnet publish "$repoRoot\src\Clovent.Desktop\Clovent.Desktop.csproj" -c Release -r win-x64 --self-contained true -p:DebugType=None -p:DebugSymbols=false -o $relDir
if ($LASTEXITCODE -ne 0) { throw "Desktop publish failed with exit code $LASTEXITCODE" }
Get-ChildItem -Path $relDir -Include "*.pdb","*Development*.json","*.lic" -Recurse -File -ErrorAction SilentlyContinue | Remove-Item -Force

Write-Host "=== 2. Publishing Clovent.Installer.Provisioner (Release win-x64 single-file) ==="
dotnet publish "$repoRoot\tools\Clovent.Installer.Provisioner\Clovent.Installer.Provisioner.csproj" -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:DebugType=None -p:DebugSymbols=false -o $provDir
if ($LASTEXITCODE -ne 0) { throw "Provisioner publish failed with exit code $LASTEXITCODE" }
Get-ChildItem -Path $provDir -Filter "*.pdb" -Recurse -File -ErrorAction SilentlyContinue | Remove-Item -Force

Write-Host "=== 3. Running ReleaseGuard ScanReleasePackage.ps1 ==="
powershell -NoProfile -ExecutionPolicy Bypass -File "$repoRoot\tools\ReleaseGuard\ScanReleasePackage.ps1" -ReleaseDir $relDir
if ($LASTEXITCODE -ne 0) { throw "ReleaseGuard failed with exit code $LASTEXITCODE" }

Write-Host "=== 4. Compiling Inno Setup Installer (cbos-setup.iss) ==="
$iscc = "C:\Users\NexGen\AppData\Local\Programs\Inno Setup 6\ISCC.exe"
if (-not (Test-Path $iscc)) { throw "ISCC.exe not found at $iscc" }
& $iscc "$repoRoot\tools\installer\cbos-setup.iss"
if ($LASTEXITCODE -ne 0) { throw "ISCC compilation failed with exit code $LASTEXITCODE" }

$builtInstaller = "$repoRoot\artifacts\installer\Clovent.BusinessOperatingSystem-1.3.0-Setup.exe"
if (-not (Test-Path $builtInstaller)) { throw "Installer not generated at $builtInstaller" }

$installerHash = (Get-FileHash $builtInstaller -Algorithm SHA256).Hash
$installerSize = (Get-Item $builtInstaller).Length
$desktopExe = "$relDir\Clovent.Desktop.exe"
$desktopHash = (Get-FileHash $desktopExe -Algorithm SHA256).Hash
$provisionerExe = "$provDir\Clovent.Installer.Provisioner.exe"
$provisionerHash = if (Test-Path $provisionerExe) { (Get-FileHash $provisionerExe -Algorithm SHA256).Hash } else { "N/A" }

# 5. Verify Shipped Runtime Versions & Security Remediation
Write-Host "`n=== 5. Verifying Shipped Runtime Pack Resolution & Security Remediation ==="
$desktopDeps = Join-Path $relDir "Clovent.Desktop.deps.json"
if (-not (Test-Path $desktopDeps)) { throw "Desktop deps.json not found in release staging!" }
$desktopDepsContent = Get-Content $desktopDeps -Raw
if ($desktopDepsContent -notmatch "runtimepack\.Microsoft\.NETCore\.App\.Runtime\.win-x64/10\.0\.13") {
    throw "Desktop deps.json does not resolve NETCore runtimepack 10.0.13!"
}
if ($desktopDepsContent -notmatch "runtimepack\.Microsoft\.WindowsDesktop\.App\.Runtime\.win-x64/10\.0\.13") {
    throw "Desktop deps.json does not resolve WindowsDesktop runtimepack 10.0.13!"
}
Write-Host "Desktop runtime pack 10.0.13 resolution verified."

$pkcsDll = Join-Path $relDir "System.Security.Cryptography.Pkcs.dll"
$pkcsFvi = if (Test-Path $pkcsDll) { [System.Diagnostics.FileVersionInfo]::GetVersionInfo($pkcsDll) } else { $null }
$pkcsHash = if (Test-Path $pkcsDll) { (Get-FileHash $pkcsDll -Algorithm SHA256).Hash } else { "N/A" }

$gitCommit = (git rev-parse HEAD).Trim()
$gitShort = (git rev-parse --short HEAD).Trim()
$gitBranch = (git branch --show-current).Trim()
$gitStatus = (git status --porcelain).Trim()
$isClean = [string]::IsNullOrEmpty($gitStatus)

# 6. Generate Package Provenance Manifest & Checksums
Write-Host "`n=== 6. Generating Package Provenance Manifest ==="
$manifest = [ordered]@{
    ReleaseDesignation = "INTERNAL TEST ONLY / UNSIGNED"
    Product = "Clovent Business Operating System"
    Version = "1.3.0"
    Milestone = "1.3.0-rc1 (Acceptance Candidate)"
    CandidateCommit = $gitCommit
    CandidateShortCommit = $gitShort
    CandidateBranch = $gitBranch
    SourceTreeClean = $isClean
    GeneratedAtUtc = [DateTimeOffset]::UtcNow.ToString("o")
    AuthenticodeSignature = [ordered]@{
        Status = "NotSigned (INTERNAL TEST ONLY)"
        SigningCertificateAvailable = $false
        WaiverScope = "Internal test qualification only; DOES NOT authorize customer deployment, commercial GA, or customer-release signing waiver."
    }
    SecurityScan = [ordered]@{
        Tool = "Tools\ReleaseGuard\ScanReleasePackage.ps1"
        DesktopStagingStatus = "PASS"
    }
    VulnerabilityRemediation = [ordered]@{
        Advisories = @("GHSA-846f-f764-76mx (CVE-2026-84008)", "GHSA-2fmv-rv2c-59x7 (CVE-2026-70162)")
        RuntimeFrameworkVersion = "10.0.13"
        ResolvedWindowsDesktopRuntime = "10.0.13"
        ResolvedNETCoreRuntime = "10.0.13"
        VulnerabilityGateStatus = "PASS"
    }
    Artifacts = [ordered]@{
        Installer = [ordered]@{
            FileName = "Clovent.BusinessOperatingSystem-1.3.0-Setup.exe"
            Path = $builtInstaller
            Sha256 = $installerHash
            SizeBytes = $installerSize
        }
        DesktopExecutable = [ordered]@{
            FileName = "Clovent.Desktop.exe"
            Sha256 = $desktopHash
            SizeBytes = (Get-Item $desktopExe).Length
        }
        ProvisionerExecutable = [ordered]@{
            FileName = "Clovent.Installer.Provisioner.exe"
            Sha256 = $provisionerHash
            SizeBytes = if (Test-Path $provisionerExe) { (Get-Item $provisionerExe).Length } else { 0 }
        }
        PkcsAssembly = [ordered]@{
            FileName = "System.Security.Cryptography.Pkcs.dll"
            FileVersion = if ($pkcsFvi) { $pkcsFvi.FileVersion } else { "N/A" }
            ProductVersion = if ($pkcsFvi) { $pkcsFvi.ProductVersion } else { "N/A" }
            Sha256 = $pkcsHash
            Origin = "runtimepack.Microsoft.WindowsDesktop.App.Runtime.win-x64/10.0.13"
        }
    }
}

$provJsonPath = Join-Path $installerDir "package_provenance_1.3.0-rc1.json"
$manifest | ConvertTo-Json -Depth 5 | Set-Content -Path $provJsonPath -Encoding UTF8
Write-Host "Wrote provenance JSON to $provJsonPath"

$unsignedManifestText = @"
================================================================================
CLOVENT BUSINESS OPERATING SYSTEM - INTERNAL TEST RELEASE MANIFEST
================================================================================
RELEASE DESIGNATION : INTERNAL TEST ONLY / UNSIGNED
MILESTONE           : 1.3.0-rc1 (Acceptance Candidate)
CANDIDATE COMMIT    : $gitCommit ($gitShort)
BRANCH              : $gitBranch
SOURCE TREE CLEAN   : $isClean
BUILD DATE (UTC)    : $([DateTimeOffset]::UtcNow.ToString("o"))
INTENDED ENVIRONMENT: Windows Sandbox Acceptance Qualification

VULNERABILITY REMEDIATION:
- Advisories Remediated : GHSA-846f-f764-76mx (CVE-2026-84008), GHSA-2fmv-rv2c-59x7 (CVE-2026-70162)
- Runtime Framework Ver : 10.0.13
- WindowsDesktop Runtime: 10.0.13
- NETCore Runtime       : 10.0.13
- PKCS Assembly Ver     : $(if ($pkcsFvi) { $pkcsFvi.FileVersion } else { 'N/A' })
- PKCS SHA-256          : $pkcsHash

AUTHENTICODE STATUS:
- Signature: UNSIGNED
- Certificate: NONE AVAILABLE
- Restriction: STRICTLY FOR INTERNAL TEST QUALIFICATION. NOT FOR CUSTOMER RELEASE.

PACKAGED ARTIFACTS:
1. Installer:
   File:   Clovent.BusinessOperatingSystem-1.3.0-Setup.exe
   Size:   $installerSize bytes
   SHA256: $installerHash

2. Desktop Executable:
   File:   Clovent.Desktop.exe
   Size:   $((Get-Item $desktopExe).Length) bytes
   SHA256: $desktopHash

3. Provisioner Executable:
   File:   Clovent.Installer.Provisioner.exe
   Size:   $(if (Test-Path $provisionerExe) { (Get-Item $provisionerExe).Length } else { 0 }) bytes
   SHA256: $provisionerHash
================================================================================
"@
$manifestTxtPath = Join-Path $installerDir "unsigned_manifest_1.3.0-rc1.txt"
$unsignedManifestText | Set-Content -Path $manifestTxtPath -Encoding UTF8
Write-Host "Wrote manifest text to $manifestTxtPath"

$sha256SumsPath = Join-Path $installerDir "SHA256SUMS.txt"
$shaSums = @"
$installerHash  Clovent.BusinessOperatingSystem-1.3.0-Setup.exe
$desktopHash  Clovent.Desktop.exe
$provisionerHash  Clovent.Installer.Provisioner.exe
"@
$shaSums | Set-Content -Path $sha256SumsPath -Encoding UTF8
Write-Host "Wrote checksums to $sha256SumsPath"

Write-Host "`n=== PACKAGING SUCCESSFUL ==="
Write-Host "Installer: $builtInstaller"
Write-Host "SHA-256:   $installerHash"
Write-Host "Size:      $installerSize bytes"
Write-Host "Desktop:   $desktopExe"
Write-Host "SHA-256:   $desktopHash"
