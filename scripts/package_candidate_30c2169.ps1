# D:\CBOS\Worktrees\cbos-release-integration\scripts\package_candidate_30c2169.ps1
$ErrorActionPreference = "Stop"

$repoRoot = "D:\CBOS\Worktrees\cbos-release-integration"
$candidateCommit = "30c2169a116c15503001394fcb9494d0b4fa38f8"
$candidateShort = "30c2169"
$candidateBranch = "integration/outbox-and-trusted-scope"

Write-Host "================================================================================"
Write-Host "CLOVENT BUSINESS OPERATING SYSTEM - CANDIDATE $candidateShort PACKAGING"
Write-Host "Target: .NET 10.0.13 Patched Runtime Pipeline"
Write-Host "================================================================================"

# Verify git commit
$currentHead = (git rev-parse HEAD).Trim()
if ($currentHead -ne $candidateCommit) {
    throw "HEAD commit mismatch! Expected $candidateCommit, found $currentHead"
}

# Directories
$candidateDir = Join-Path $repoRoot "artifacts\candidate_$candidateShort"
$desktopStaging = Join-Path $candidateDir "staging\Desktop"
$provisionerStaging = Join-Path $candidateDir "staging\Provisioner"
$installerDir = Join-Path $candidateDir "installer"
$evidenceDir = Join-Path $candidateDir "evidence"

if (Test-Path $desktopStaging) { Remove-Item $desktopStaging -Recurse -Force }
if (Test-Path $provisionerStaging) { Remove-Item $provisionerStaging -Recurse -Force }
New-Item -ItemType Directory -Path $desktopStaging -Force | Out-Null
New-Item -ItemType Directory -Path $provisionerStaging -Force | Out-Null
New-Item -ItemType Directory -Path $installerDir -Force | Out-Null
New-Item -ItemType Directory -Path $evidenceDir -Force | Out-Null

# 1. Publish Clovent.Desktop
Write-Host "`n=== 1. Publishing Clovent.Desktop (Release win-x64 self-contained) ==="
dotnet publish "$repoRoot\src\Clovent.Desktop\Clovent.Desktop.csproj" `
    -c Release -r win-x64 --self-contained true `
    -p:DebugType=None -p:DebugSymbols=false `
    -o $desktopStaging
if ($LASTEXITCODE -ne 0) { throw "Desktop publish failed with exit code $LASTEXITCODE" }

# Remove pdb, lic, and dev settings
Get-ChildItem -Path $desktopStaging -Include "*.pdb","*Development*.json","*.lic" -Recurse -File -ErrorAction SilentlyContinue | Remove-Item -Force
Write-Host "Desktop published to $desktopStaging"

# 2. Publish Clovent.Installer.Provisioner
Write-Host "`n=== 2. Publishing Clovent.Installer.Provisioner (Release win-x64 single-file) ==="
dotnet publish "$repoRoot\Tools\Clovent.Installer.Provisioner\Clovent.Installer.Provisioner.csproj" `
    -c Release -r win-x64 --self-contained true `
    -p:PublishSingleFile=true -p:DebugType=None -p:DebugSymbols=false `
    -o $provisionerStaging
if ($LASTEXITCODE -ne 0) { throw "Provisioner publish failed with exit code $LASTEXITCODE" }

Get-ChildItem -Path $provisionerStaging -Filter "*.pdb" -Recurse -File -ErrorAction SilentlyContinue | Remove-Item -Force
Write-Host "Provisioner published to $provisionerStaging"

# 3. Security Scan (ReleaseGuard)
Write-Host "`n=== 3. Security Scanning with ReleaseGuard ==="
$releaseGuard = "$repoRoot\Tools\ReleaseGuard\ScanReleasePackage.ps1"
powershell -NoProfile -ExecutionPolicy Bypass -File $releaseGuard -ReleaseDir $desktopStaging
if ($LASTEXITCODE -ne 0) { throw "ReleaseGuard failed for Desktop staging with exit code $LASTEXITCODE" }

powershell -NoProfile -ExecutionPolicy Bypass -File $releaseGuard -ReleaseDir $provisionerStaging
if ($LASTEXITCODE -ne 0) { throw "ReleaseGuard failed for Provisioner staging with exit code $LASTEXITCODE" }

powershell -NoProfile -ExecutionPolicy Bypass -File $releaseGuard -ReleaseDir "$repoRoot\Tools\installer\Templates"
if ($LASTEXITCODE -ne 0) { throw "ReleaseGuard failed for Templates with exit code $LASTEXITCODE" }

# 4. Detailed Dependency & Runtime Provenance Validation
Write-Host "`n=== 4. Validating Published Runtime and Dependency Provenance ==="
$pkcsDll = Join-Path $desktopStaging "System.Security.Cryptography.Pkcs.dll"
if (-not (Test-Path $pkcsDll)) { throw "Expected PKCS DLL missing from Desktop staging: $pkcsDll" }

$pkcsItem = Get-Item $pkcsDll
$pkcsFvi = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($pkcsDll)
$pkcsHash = (Get-FileHash -Path $pkcsDll -Algorithm SHA256).Hash

Write-Host "Published System.Security.Cryptography.Pkcs.dll in Desktop:"
Write-Host "   FileVersion:    $($pkcsFvi.FileVersion)"
Write-Host "   ProductVersion: $($pkcsFvi.ProductVersion)"
Write-Host "   Length:         $($pkcsItem.Length) bytes"
Write-Host "   SHA-256:        $pkcsHash"

if ($pkcsFvi.FileVersion -notmatch "^10\.0\.13") {
    throw "VULNERABILITY REMAINING! Pkcs FileVersion is '$($pkcsFvi.FileVersion)', expected 10.0.13.x"
}
if ($pkcsFvi.ProductVersion -notmatch "^10\.0\.13") {
    throw "VULNERABILITY REMAINING! Pkcs ProductVersion is '$($pkcsFvi.ProductVersion)', expected 10.0.13"
}

$desktopDeps = Join-Path $desktopStaging "Clovent.Desktop.deps.json"
$desktopDepsContent = Get-Content $desktopDeps -Raw
if ($desktopDepsContent -notmatch "runtimepack\.Microsoft\.WindowsDesktop\.App\.Runtime\.win-x64/10\.0\.13") {
    throw "Desktop deps.json does not resolve WindowsDesktop runtimepack 10.0.13!"
}
if ($desktopDepsContent -notmatch "runtimepack\.Microsoft\.NETCore\.App\.Runtime\.win-x64/10\.0\.13") {
    throw "Desktop deps.json does not resolve NETCore runtimepack 10.0.13!"
}
Write-Host "Desktop runtime pack 10.0.13 resolution verified."

$provDeps = Join-Path $repoRoot "Tools\Clovent.Installer.Provisioner\bin\Release\net10.0-windows\win-x64\Clovent.Installer.Provisioner.deps.json"
if (Test-Path $provDeps) {
    $provDepsContent = Get-Content $provDeps -Raw
    if ($provDepsContent -notmatch "runtimepack\.Microsoft\.NETCore\.App\.Runtime\.win-x64/10\.0\.13") {
        throw "Provisioner deps.json does not resolve NETCore runtimepack 10.0.13!"
    }
    Write-Host "Provisioner runtime pack 10.0.13 resolution verified."
}

# 5. Compile Inno Setup Installer
Write-Host "`n=== 5. Compiling Inno Setup Installer ==="
$iscc = "C:\Users\NexGen\AppData\Local\Programs\Inno Setup 6\ISCC.exe"
if (-not (Test-Path $iscc)) { $iscc = "$repoRoot\Tools\InnoSetup\ISCC.exe" }
$issScript = "$repoRoot\Tools\installer\cbos-setup.iss"

$outputBaseName = "Clovent.BusinessOperatingSystem-1.3.0-Setup-$candidateShort"
& $iscc `
    "/DPublishDir=$desktopStaging" `
    "/DProvisionerDir=$provisionerStaging" `
    "/DOutputDir=$installerDir" `
    "/F$outputBaseName" `
    "$issScript"
if ($LASTEXITCODE -ne 0) { throw "ISCC compilation failed with exit code $LASTEXITCODE" }

$installerPath = Join-Path $installerDir "$outputBaseName.exe"
if (-not (Test-Path $installerPath)) {
    throw "Installer binary was not created at: $installerPath"
}
$installerHash = (Get-FileHash -Path $installerPath -Algorithm SHA256).Hash
$installerItem = Get-Item $installerPath

Write-Host "Compiled installer: $installerPath"
Write-Host "   Size:    $($installerItem.Length) bytes"
Write-Host "   SHA-256: $installerHash"

# 6. Generate Provenance Manifest
Write-Host "`n=== 6. Generating Accurate Package Provenance Manifest ==="
$desktopExe = Join-Path $desktopStaging "Clovent.Desktop.exe"
$provisionerExe = Join-Path $provisionerStaging "Clovent.Installer.Provisioner.exe"

$manifest = [ordered]@{
    ReleaseDesignation = "INTERNAL TEST ONLY / UNSIGNED"
    Product = "Clovent Business Operating System"
    Version = "1.3.0"
    CandidateCommit = $candidateCommit
    CandidateShortCommit = $candidateShort
    CandidateBranch = $candidateBranch
    GeneratedAtUtc = [DateTimeOffset]::UtcNow.ToString("o")
    AuthenticodeSignature = [ordered]@{
        Status = "NotSigned (INTERNAL TEST ONLY)"
        SigningCertificateAvailable = $false
        WaiverScope = "Internal test qualification only; DOES NOT authorize customer deployment, commercial GA, or customer-release signing waiver."
    }
    SecurityScan = [ordered]@{
        Tool = "Tools\ReleaseGuard\ScanReleasePackage.ps1"
        DesktopStagingStatus = "PASS"
        ProvisionerStagingStatus = "PASS"
        TemplatesStatus = "PASS"
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
            FileName = "$outputBaseName.exe"
            Path = $installerPath
            Sha256 = $installerHash
            SizeBytes = $installerItem.Length
        }
        DesktopExecutable = [ordered]@{
            FileName = "Clovent.Desktop.exe"
            Sha256 = (Get-FileHash -Path $desktopExe -Algorithm SHA256).Hash
            SizeBytes = (Get-Item $desktopExe).Length
        }
        ProvisionerExecutable = [ordered]@{
            FileName = "Clovent.Installer.Provisioner.exe"
            Sha256 = (Get-FileHash -Path $provisionerExe -Algorithm SHA256).Hash
            SizeBytes = (Get-Item $provisionerExe).Length
        }
        PkcsAssembly = [ordered]@{
            FileName = "System.Security.Cryptography.Pkcs.dll"
            FileVersion = $pkcsFvi.FileVersion
            ProductVersion = $pkcsFvi.ProductVersion
            Sha256 = $pkcsHash
            SizeBytes = $pkcsItem.Length
            Origin = "runtimepack.Microsoft.WindowsDesktop.App.Runtime.win-x64/10.0.13"
        }
    }
}

$provJsonPath = Join-Path $candidateDir "package_provenance_$candidateShort.json"
$manifest | ConvertTo-Json -Depth 5 | Set-Content -Path $provJsonPath -Encoding UTF8
Write-Host "Wrote provenance JSON to $provJsonPath"

$unsignedManifestText = @"
================================================================================
CLOVENT BUSINESS OPERATING SYSTEM - INTERNAL TEST RELEASE MANIFEST
================================================================================
RELEASE DESIGNATION : INTERNAL TEST ONLY / UNSIGNED
CANDIDATE COMMIT    : $candidateCommit ($candidateShort)
BRANCH              : $candidateBranch
BUILD DATE (UTC)    : $([DateTimeOffset]::UtcNow.ToString("o"))
INTENDED ENVIRONMENT: Windows Sandbox Acceptance Qualification

VULNERABILITY REMEDIATION:
- Advisories Remediated : GHSA-846f-f764-76mx (CVE-2026-84008), GHSA-2fmv-rv2c-59x7 (CVE-2026-70162)
- Runtime Framework Ver : 10.0.13
- WindowsDesktop Runtime: 10.0.13
- NETCore Runtime       : 10.0.13
- PKCS Assembly Ver     : $($pkcsFvi.FileVersion) ($($pkcsFvi.ProductVersion))
- PKCS SHA-256          : $pkcsHash

AUTHENTICODE STATUS:
- Signature: UNSIGNED
- Certificate: NONE AVAILABLE
- Restriction: STRICTLY FOR INTERNAL TEST QUALIFICATION. NOT FOR CUSTOMER RELEASE.

PACKAGED ARTIFACTS:
1. Installer:
   File:   $outputBaseName.exe
   Size:   $($installerItem.Length) bytes
   SHA256: $installerHash

2. Desktop Executable:
   File:   Clovent.Desktop.exe
   Size:   $((Get-Item $desktopExe).Length) bytes
   SHA256: $((Get-FileHash -Path $desktopExe -Algorithm SHA256).Hash)

3. Provisioner Executable:
   File:   Clovent.Installer.Provisioner.exe
   Size:   $((Get-Item $provisionerExe).Length) bytes
   SHA256: $((Get-FileHash -Path $provisionerExe -Algorithm SHA256).Hash)

4. Cryptography Assembly:
   File:   System.Security.Cryptography.Pkcs.dll
   Size:   $($pkcsItem.Length) bytes
   SHA256: $pkcsHash
================================================================================
"@
$manifestTxtPath = Join-Path $candidateDir "unsigned_manifest_$candidateShort.txt"
$unsignedManifestText | Set-Content -Path $manifestTxtPath -Encoding UTF8
Write-Host "Wrote manifest text to $manifestTxtPath"

Write-Host "`n================================================================================"
Write-Host "PACKAGING COMPLETE: Candidate $candidateShort successfully built and verified."
Write-Host "================================================================================"
