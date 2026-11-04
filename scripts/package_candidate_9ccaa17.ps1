# D:\CBOS\Worktrees\cbos-unified-candidate\scripts\package_candidate_9ccaa17.ps1
$ErrorActionPreference = "Stop"

$repoRoot = "D:\CBOS\Worktrees\cbos-unified-candidate"
Set-Location $repoRoot

$gitCommit = "9ccaa17ebd35edd58a3daa280df64d5ed16464ce"
$gitShort  = "9ccaa17"
$gitBranch = "integration/cbos-1.3.0-unified-candidate"

# 1. Create unique run directory structure
$runDir       = Join-Path $repoRoot "artifacts\acceptance_run_candidate_$gitShort"
$installerDir = Join-Path $runDir "installer"
$payloadDir   = Join-Path $runDir "payload"
$evidenceDir  = Join-Path $runDir "evidence"

New-Item -ItemType Directory -Path $installerDir -Force | Out-Null
New-Item -ItemType Directory -Path $payloadDir -Force | Out-Null
New-Item -ItemType Directory -Path $evidenceDir -Force | Out-Null

$relDir  = "$repoRoot\artifacts\release\Clovent.BusinessOperatingSystem-win-x64"
$provDir = "$repoRoot\tools\Clovent.Installer.Provisioner\bin\publish"

if (Test-Path $relDir) { Remove-Item $relDir -Recurse -Force }
if (Test-Path $provDir) { Remove-Item $provDir -Recurse -Force }

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

Write-Host "=== 4. Verifying Shipped Runtime Pack Resolution & Security Remediation ==="
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

$desktopExe = "$relDir\Clovent.Desktop.exe"
$desktopHash = (Get-FileHash -LiteralPath $desktopExe -Algorithm SHA256).Hash.ToUpperInvariant()
$desktopFvi = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($desktopExe)

$provisionerExe = "$provDir\Clovent.Installer.Provisioner.exe"
$provisionerHash = if (Test-Path $provisionerExe) { (Get-FileHash -LiteralPath $provisionerExe -Algorithm SHA256).Hash.ToUpperInvariant() } else { "N/A" }
$provFvi = if (Test-Path $provisionerExe) { [System.Diagnostics.FileVersionInfo]::GetVersionInfo($provisionerExe) } else { $null }

$pkcsDll = Join-Path $relDir "System.Security.Cryptography.Pkcs.dll"
$pkcsFvi = if (Test-Path $pkcsDll) { [System.Diagnostics.FileVersionInfo]::GetVersionInfo($pkcsDll) } else { $null }
$pkcsHash = if (Test-Path $pkcsDll) { (Get-FileHash -LiteralPath $pkcsDll -Algorithm SHA256).Hash.ToUpperInvariant() } else { "N/A" }

Write-Host "=== 5. Compiling Inno Setup Installer (cbos-setup.iss) ==="
$iscc = "C:\Users\NexGen\AppData\Local\Programs\Inno Setup 6\ISCC.exe"
if (-not (Test-Path $iscc)) { $iscc = "$repoRoot\Tools\InnoSetup\ISCC.exe" }
if (-not (Test-Path $iscc)) { throw "ISCC.exe not found at $iscc" }

$installerBaseName = "Clovent.BusinessOperatingSystem-1.3.0-Setup-$gitShort"
& $iscc /O"$installerDir" /F"$installerBaseName" "$repoRoot\tools\installer\cbos-setup.iss"
if ($LASTEXITCODE -ne 0) { throw "ISCC compilation failed with exit code $LASTEXITCODE" }

$builtInstaller = Join-Path $installerDir "$installerBaseName.exe"
if (-not (Test-Path $builtInstaller)) { throw "Installer not generated at $builtInstaller" }

$installerHash = (Get-FileHash -LiteralPath $builtInstaller -Algorithm SHA256).Hash.ToUpperInvariant()
$installerSize = (Get-Item $builtInstaller).Length

# Preserve into global installer directory as unique candidate file
$globalInstallerDir = "$repoRoot\artifacts\installer"
if (Test-Path $globalInstallerDir) {
    Copy-Item -LiteralPath $builtInstaller -Destination (Join-Path $globalInstallerDir "$installerBaseName.exe") -Force
}

$rawGitStatus = git status --porcelain
$isClean = ($null -eq $rawGitStatus -or [string]::IsNullOrWhiteSpace(($rawGitStatus | Out-String)))

Write-Host "`n=== 6. Generating Package Provenance & Manifests ==="
$manifest = [ordered]@{
    ReleaseDesignation = "INTERNAL TEST ONLY / UNSIGNED"
    Product = "Clovent Business Operating System"
    Version = "1.3.0"
    Milestone = "1.3.0-rc1 (Acceptance Candidate $gitShort)"
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
            FileName = "$installerBaseName.exe"
            Path = $builtInstaller
            Sha256 = $installerHash
            SizeBytes = $installerSize
        }
        DesktopExecutable = [ordered]@{
            FileName = "Clovent.Desktop.exe"
            Path = $desktopExe
            Sha256 = $desktopHash
            SizeBytes = (Get-Item $desktopExe).Length
            ProductVersion = if ($desktopFvi) { $desktopFvi.ProductVersion } else { "N/A" }
            FileVersion = if ($desktopFvi) { $desktopFvi.FileVersion } else { "N/A" }
        }
        ProvisionerExecutable = [ordered]@{
            FileName = "Clovent.Installer.Provisioner.exe"
            Path = $provisionerExe
            Sha256 = $provisionerHash
            SizeBytes = if (Test-Path $provisionerExe) { (Get-Item $provisionerExe).Length } else { 0 }
            ProductVersion = if ($provFvi) { $provFvi.ProductVersion } else { "N/A" }
            FileVersion = if ($provFvi) { $provFvi.FileVersion } else { "N/A" }
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

$provJsonPath = Join-Path $installerDir "package_provenance_$gitShort.json"
$manifest | ConvertTo-Json -Depth 5 | Set-Content -Path $provJsonPath -Encoding UTF8
Write-Host "Wrote provenance JSON to $provJsonPath"

$unsignedManifestText = @"
================================================================================
CLOVENT BUSINESS OPERATING SYSTEM - INTERNAL TEST RELEASE MANIFEST
================================================================================
RELEASE DESIGNATION : INTERNAL TEST ONLY / UNSIGNED
MILESTONE           : 1.3.0-rc1 (Acceptance Candidate $gitShort)
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
   File:   $installerBaseName.exe
   Size:   $installerSize bytes
   SHA256: $installerHash

2. Desktop Executable:
   File:   Clovent.Desktop.exe
   Size:   $((Get-Item $desktopExe).Length) bytes
   SHA256: $desktopHash
   ProductVersion: $($desktopFvi.ProductVersion)

3. Provisioner Executable:
   File:   Clovent.Installer.Provisioner.exe
   Size:   $(if (Test-Path $provisionerExe) { (Get-Item $provisionerExe).Length } else { 0 }) bytes
   SHA256: $provisionerHash
================================================================================
"@
$manifestTxtPath = Join-Path $installerDir "unsigned_manifest_$gitShort.txt"
$unsignedManifestText | Set-Content -Path $manifestTxtPath -Encoding UTF8
Write-Host "Wrote manifest text to $manifestTxtPath"

$sha256SumsPath = Join-Path $installerDir "SHA256SUMS.txt"
$shaSums = @"
$installerHash  $installerBaseName.exe
$desktopHash  Clovent.Desktop.exe
$provisionerHash  Clovent.Installer.Provisioner.exe
$pkcsHash  System.Security.Cryptography.Pkcs.dll
"@
$shaSums | Set-Content -Path $sha256SumsPath -Encoding UTF8
Write-Host "Wrote checksums to $sha256SumsPath"

Write-Host "`n=== PACKAGING SUCCESSFUL ==="
Write-Host "Installer: $builtInstaller"
Write-Host "SHA-256:   $installerHash"
Write-Host "Size:      $installerSize bytes"
Write-Host "Desktop:   $desktopExe"
Write-Host "SHA-256:   $desktopHash"
Write-Host "ProvExe:   $provisionerExe"
Write-Host "SHA-256:   $provisionerHash"
