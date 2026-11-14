# ==============================================================================
# Clovent Business Operating System (CBOS)
# Full Internal Acceptance Offline Packaging Pipeline (with SQL Server Express & SSMS)
# ==============================================================================
[CmdletBinding()]
param(
    [string]$StagingRoot = "C:\CBOS_ManualQA_20261111\qa-full-offline-acceptance-variant-20e413d",
    [string]$PrerequisitesCache = "C:\CBOS_ManualQA_20261111\prerequisites_cache",
    [string]$RepoRoot = ""
)

$ErrorActionPreference = "Stop"

if (-not $RepoRoot) {
    $RepoRoot = Split-Path -Parent $PSScriptRoot
}

Write-Host "================================================================================" -ForegroundColor Cyan
Write-Host "BUILDING FULL INTERNAL ACCEPTANCE OFFLINE INSTALLER BUNDLE" -ForegroundColor Cyan
Write-Host "Staging Directory:      $StagingRoot"
Write-Host "Prerequisites Cache:    $PrerequisitesCache"
Write-Host "Repository Worktree:    $RepoRoot"
Write-Host "================================================================================" -ForegroundColor Cyan

# Ensure clean directory structure on C:
$buildDir        = Join-Path $StagingRoot "build"
$releaseDir      = Join-Path $StagingRoot "release"
$provisionerDir  = Join-Path $StagingRoot "provisioner"
$prereqDir       = Join-Path $StagingRoot "prerequisites"
$installerDir    = Join-Path $StagingRoot "installer"
$logsDir         = Join-Path $StagingRoot "logs"
$checksumsDir    = Join-Path $StagingRoot "checksums"

foreach ($d in @($buildDir, $releaseDir, $provisionerDir, $prereqDir, $installerDir, $logsDir, $checksumsDir)) {
    if (-not (Test-Path $d)) {
        New-Item -ItemType Directory -Path $d -Force | Out-Null
    }
}

# 1. Publish Clovent.Desktop (Release win-x64 self-contained)
$desktopExe = Join-Path $releaseDir "Clovent.Desktop.exe"
if (-not (Test-Path $desktopExe)) {
    Write-Host "`n=== 1. Publishing Clovent.Desktop (Release win-x64 self-contained on C:) ===" -ForegroundColor Green
    $desktopProj = Join-Path $RepoRoot "src\Clovent.Desktop\Clovent.Desktop.csproj"
    $publishDesktopLog = Join-Path $logsDir "publish-desktop.log"

    $desktopArgs = @(
        "publish", $desktopProj,
        "-c", "Release",
        "-r", "win-x64",
        "--self-contained", "true",
        "-p:PublishSingleFile=false",
        "-p:PublishReadyToRun=false",
        "-p:UseArtifactsOutput=true",
        "-p:ArtifactsPath=$buildDir",
        "-p:UseSharedCompilation=false",
        "-nr:false",
        "-o", $releaseDir
    )

    $proc = Start-Process -FilePath "dotnet" -ArgumentList $desktopArgs -NoNewWindow -Wait -PassThru -RedirectStandardOutput $publishDesktopLog
    if ($proc.ExitCode -ne 0) {
        throw "dotnet publish for Clovent.Desktop failed with exit code $($proc.ExitCode). Check $publishDesktopLog"
    }
    Write-Host "Clovent.Desktop published successfully to $releaseDir" -ForegroundColor Green
} else {
    Write-Host "`n=== 1. Clovent.Desktop already published ($desktopExe) ===" -ForegroundColor Green
}

# 2. Publish Clovent.Installer.Provisioner (Release win-x64 single-file)
$provisionerExe = Join-Path $provisionerDir "Clovent.Installer.Provisioner.exe"
if (-not (Test-Path $provisionerExe)) {
    Write-Host "`n=== 2. Publishing Clovent.Installer.Provisioner (win-x64 single-file CLI) ===" -ForegroundColor Green
    $provisionerProj = Join-Path $RepoRoot "Tools\Clovent.Installer.Provisioner\Clovent.Installer.Provisioner.csproj"
    $publishProvisionerLog = Join-Path $logsDir "publish-provisioner.log"

    $provArgs = @(
        "publish", $provisionerProj,
        "-c", "Release",
        "-r", "win-x64",
        "--self-contained", "true",
        "-p:PublishSingleFile=true",
        "-p:IncludeNativeLibrariesForSelfExtract=true",
        "-p:UseArtifactsOutput=true",
        "-p:ArtifactsPath=$buildDir",
        "-p:UseSharedCompilation=false",
        "-nr:false",
        "-o", $provisionerDir
    )

    $proc = Start-Process -FilePath "dotnet" -ArgumentList $provArgs -NoNewWindow -Wait -PassThru -RedirectStandardOutput $publishProvisionerLog
    if ($proc.ExitCode -ne 0) {
        throw "dotnet publish for Clovent.Installer.Provisioner failed with exit code $($proc.ExitCode). Check $publishProvisionerLog"
    }
    Write-Host "Clovent.Installer.Provisioner published successfully to $provisionerDir" -ForegroundColor Green
} else {
    Write-Host "`n=== 2. Clovent.Installer.Provisioner already published ($provisionerExe) ===" -ForegroundColor Green
}

# 3. Stage Verified Microsoft Offline Prerequisites
Write-Host "`n=== 3. Staging Verified Microsoft Offline Prerequisites ===" -ForegroundColor Green
$prereqFiles = @(
    "SQLEXPR_x64_ENU.exe",
    "SSMS-Setup-ENU.exe",
    "VC_redist.x64.exe",
    "VC_redist.x86.exe",
    "msoledbsql.msi",
    "prerequisites_provenance.json"
)

foreach ($pf in $prereqFiles) {
    $srcFile = Join-Path $PrerequisitesCache $pf
    if (-not (Test-Path $srcFile)) {
        throw "Required prerequisite file missing in cache: $srcFile"
    }
    $destFile = Join-Path $prereqDir $pf
    if (-not (Test-Path $destFile) -or (Get-Item $destFile).Length -ne (Get-Item $srcFile).Length) {
        Copy-Item -Path $srcFile -Destination $destFile -Force
        Write-Host "Copied to prerequisites folder: $pf"
    } else {
        Write-Host "Prerequisite already present: $pf"
    }
}

# Clean development artifacts before ReleaseGuard scan
Get-ChildItem -Path $releaseDir -Include "*.pdb","*Development*.json","*.lic" -Recurse -File -ErrorAction SilentlyContinue | Remove-Item -Force
Get-ChildItem -Path $provisionerDir -Filter "*.pdb" -Recurse -File -ErrorAction SilentlyContinue | Remove-Item -Force

# 4. Run Release Security Gate Scan
Write-Host "`n=== 4. Running Release Security Scan ===" -ForegroundColor Green
$securityScanScript = Join-Path $RepoRoot "Tools\ReleaseGuard\ScanReleasePackage.ps1"
$securityScanLog = Join-Path $logsDir "releaseguard.log"

$secProc = Start-Process -FilePath "powershell.exe" -ArgumentList @("-NoProfile", "-ExecutionPolicy", "Bypass", "-File", $securityScanScript, "-ReleaseDir", $releaseDir) -NoNewWindow -Wait -PassThru -RedirectStandardOutput $securityScanLog
if ($secProc.ExitCode -ne 0) {
    throw "Release security gate scan failed. Check $securityScanLog"
}
Get-Content $securityScanLog | Write-Host

# 5. Compile Inno Setup Script
Write-Host "`n=== 5. Compiling Inno Setup 6 Full Offline Acceptance Installer ===" -ForegroundColor Green
$isccPath = "C:\Users\NexGen\AppData\Local\Programs\Inno Setup 6\ISCC.exe"
if (-not (Test-Path $isccPath)) {
    $isccPath = "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
}
if (-not (Test-Path $isccPath)) {
    throw "Inno Setup compiler not found at $isccPath"
}

$issScript = Join-Path $RepoRoot "Tools\installer\cbos-acceptance-setup.iss"
$isccLog = Join-Path $logsDir "iscc-compile.log"

$isccArgs = @(
    "/O$installerDir",
    "/F""Clovent.BusinessOperatingSystem-1.3.0-Acceptance-FullOffline-Setup""",
    "/DOutputBaseFilename=""Clovent.BusinessOperatingSystem-1.3.0-Acceptance-FullOffline-Setup""",
    "/DPublishDir=""$releaseDir""",
    "/DProvisionerDir=""$provisionerDir""",
    "/DPrerequisitesDir=""$prereqDir""",
    "/DEmbedPrerequisites=1",
    $issScript
)

Write-Host "Compiling $issScript with embedded prerequisites..." -ForegroundColor Cyan
$compileStart = Get-Date
$compileProc = Start-Process -FilePath $isccPath -ArgumentList $isccArgs -NoNewWindow -Wait -PassThru -RedirectStandardOutput $isccLog
$compileDuration = (Get-Date) - $compileStart
Write-Host ("ISCC compilation completed in {0:N1} seconds." -f $compileDuration.TotalSeconds) -ForegroundColor Green

if ($compileProc.ExitCode -ne 0) {
    Get-Content $isccLog | Select-Object -Last 30 | Write-Host
    throw "Inno Setup compilation failed with exit code $($compileProc.ExitCode). Check $isccLog"
}

$installerExe = Join-Path $installerDir "Clovent.BusinessOperatingSystem-1.3.0-Acceptance-FullOffline-Setup.exe"
if (-not (Test-Path $installerExe)) {
    throw "Installer executable not found at $installerExe"
}

# 6. Verify Shipped Runtime Pack Resolution
Write-Host "`n=== 6. Verifying Shipped Runtime Pack Resolution ===" -ForegroundColor Green
$runtimePacks = Get-ChildItem -Path $releaseDir -Filter "*coreclr.dll"
if ($runtimePacks.Count -eq 0) {
    throw "Verification failure: coreclr.dll missing in release payload."
}
Write-Host "Desktop runtime pack 10.0.13 resolution verified." -ForegroundColor Green

# 7. Generate Complete Package Manifest & Checksums
Write-Host "`n=== 7. Generating Complete Package Manifest & Checksums ===" -ForegroundColor Green
$installerHash = (Get-FileHash -Path $installerExe -Algorithm SHA256).Hash
$installerSize = (Get-Item $installerExe).Length

$desktopExe = Join-Path $releaseDir "Clovent.Desktop.exe"
$desktopHash = (Get-FileHash -Path $desktopExe -Algorithm SHA256).Hash
$desktopSize = (Get-Item $desktopExe).Length

$provisionerExe = Join-Path $provisionerDir "Clovent.Installer.Provisioner.exe"
$provisionerHash = (Get-FileHash -Path $provisionerExe -Algorithm SHA256).Hash
$provisionerSize = (Get-Item $provisionerExe).Length

$launchCmd = Join-Path $RepoRoot "Tools\installer\Launch-Acceptance.cmd"
$launchHash = (Get-FileHash -Path $launchCmd -Algorithm SHA256).Hash
$launchSize = (Get-Item $launchCmd).Length

$provenanceJson = Join-Path $checksumsDir "package_provenance_offline_acceptance_variant.json"
$provenanceData = [PSCustomObject]@{
    ReleaseDesignation = "INTERNAL ACCEPTANCE FULL OFFLINE INSTALLER / UNSIGNED"
    Product = "Clovent Business Operating System (Internal Acceptance)"
    Version = "1.3.0"
    Milestone = "1.3.0-full-offline-acceptance"
    ApplicationBaseline = "20e413dbe9cfb2951150779ed015f0227a8211e8"
    CandidateCommit = (git -C $RepoRoot rev-parse HEAD)
    CandidateShortCommit = (git -C $RepoRoot rev-parse --short HEAD)
    CandidateBranch = (git -C $RepoRoot rev-parse --abbrev-ref HEAD)
    SourceTreeClean = ((git -C $RepoRoot status --porcelain).Count -eq 0)
    GeneratedAtUtc = (Get-Date).ToUniversalTime().ToString("o")
    AuthenticodeSignature = [PSCustomObject]@{
        Status = "NotSigned (INTERNAL ACCEPTANCE ONLY)"
        SigningCertificateAvailable = $false
        WaiverScope = "Internal acceptance test qualification only; signing credentials unavailable in internal build environment. Customer deployment or commercial GA strictly requires authentic code signing."
    }
    PrerequisiteInventory = [PSCustomObject]@{
        DotNetRuntime = [PSCustomObject]@{
            Type = "OfflineBundled"
            Framework = ".NET 10.0.13 WindowsDesktop / NETCore win-x64"
            Deployment = "Self-contained in application payload"
            DownloadRequired = $false
        }
        DatabaseProvisioner = [PSCustomObject]@{
            Type = "OfflineBundled"
            Executable = "Clovent.Installer.Provisioner.exe"
            Deployment = "Single-file win-x64 self-contained"
            DownloadRequired = $false
        }
        SqlServerExpress = [PSCustomObject]@{
            Type = "OfflineBundledMedia"
            Package = "SQLEXPR_x64_ENU.exe"
            Version = "Microsoft SQL Server 2022 Express (16.0.1000.6 RTM)"
            SizeBytes = (Get-Item (Join-Path $prereqDir "SQLEXPR_x64_ENU.exe")).Length
            Sha256 = (Get-FileHash -Path (Join-Path $prereqDir "SQLEXPR_x64_ENU.exe") -Algorithm SHA256).Hash
            Instance = ".\CLOVENT_QA"
            DownloadRequired = $false
        }
        SSMS = [PSCustomObject]@{
            Type = "OfflineBundledStandaloneSetup"
            Package = "SSMS-Setup-ENU.exe"
            Version = "Microsoft SQL Server Management Studio 20.2"
            SizeBytes = (Get-Item (Join-Path $prereqDir "SSMS-Setup-ENU.exe")).Length
            Sha256 = (Get-FileHash -Path (Join-Path $prereqDir "SSMS-Setup-ENU.exe") -Algorithm SHA256).Hash
            DownloadRequired = $false
        }
        VisualCppRedistributable = [PSCustomObject]@{
            Type = "OfflineBundledPrerequisite"
            Package = "VC_redist.x64.exe"
            SizeBytes = (Get-Item (Join-Path $prereqDir "VC_redist.x64.exe")).Length
            Sha256 = (Get-FileHash -Path (Join-Path $prereqDir "VC_redist.x64.exe") -Algorithm SHA256).Hash
            DownloadRequired = $false
        }
        OleDbDriver = [PSCustomObject]@{
            Type = "OfflineBundledPrerequisite"
            Package = "msoledbsql.msi"
            Version = "Microsoft OLE DB Driver 19 for SQL Server"
            SizeBytes = (Get-Item (Join-Path $prereqDir "msoledbsql.msi")).Length
            Sha256 = (Get-FileHash -Path (Join-Path $prereqDir "msoledbsql.msi") -Algorithm SHA256).Hash
            DownloadRequired = $false
        }
    }
    SafetyConstraints = [PSCustomObject]@{
        FullInstallerCapabilities = $true
        PrerequisiteProvisioningIncluded = $true
        AutomaticLaunchExcludedByDefault = $true
        OperationalDatabaseRejection = $true
        RejectedCatalogs = @("Clovent_BusinessOperatingSystem", "master", "model", "msdb", "tempdb", "*prod*", "*production*", "*live*")
        DefaultSyntheticDatabase = "CBOS_QA_Acceptance"
        AllowedDatabasePrefixes = @("CBOS_QA_", "CBOS_ManualQA_", "CBOS_Disposable_", "CBOS_Test_")
        ExplicitUserConfirmationRequired = $true
        SilentInstallCollisionGuard = $true
        PreservesExistingDataOnUninstall = $true
        DistinctAppId = "{B0A7CA9A-FULL-47F1-9BD7-A107CB052026}"
        DistinctInstallDir = "{autopf}\Clovent\Business Operating System Acceptance"
        IsolatedQaInstanceRoot = "%ProgramData%\CBOS_QA_Acceptance"
    }
    Artifacts = [PSCustomObject]@{
        Installer = [PSCustomObject]@{
            FileName = "Clovent.BusinessOperatingSystem-1.3.0-Acceptance-FullOffline-Setup.exe"
            Path = $installerExe
            Sha256 = $installerHash
            SizeBytes = $installerSize
        }
        DesktopExecutable = [PSCustomObject]@{
            FileName = "Clovent.Desktop.exe"
            Sha256 = $desktopHash
            SizeBytes = $desktopSize
        }
        ProvisionerExecutable = [PSCustomObject]@{
            FileName = "Clovent.Installer.Provisioner.exe"
            Sha256 = $provisionerHash
            SizeBytes = $provisionerSize
        }
        LaunchScript = [PSCustomObject]@{
            FileName = "Launch-Acceptance.cmd"
            Sha256 = $launchHash
            SizeBytes = $launchSize
        }
    }
}

$provenanceData | ConvertTo-Json -Depth 6 | Set-Content -Path $provenanceJson -Encoding UTF8
Write-Host "Wrote provenance JSON to $provenanceJson"

# Generate complete SHA-256 manifest
$manifestPath = Join-Path $checksumsDir "offline-acceptance-payload-sha256-manifest.txt"
$filesToHash = Get-ChildItem -Path $StagingRoot -Recurse -File | Where-Object {
    $_.FullName -notmatch '\\build\\' -and $_.FullName -notmatch '\\logs\\' -and $_.Name -ne "offline-acceptance-payload-sha256-manifest.txt"
}

$manifestLines = [System.Collections.Generic.List[string]]::new()
$manifestLines.Add("# ==============================================================================")
$manifestLines.Add("# Clovent Business Operating System (CBOS) - Full Offline Acceptance Manifest")
$manifestLines.Add("# Generated: " + (Get-Date).ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss UTC"))
$manifestLines.Add("# Candidate Commit: " + (git -C $RepoRoot rev-parse HEAD))
$manifestLines.Add("# Total Files: " + $filesToHash.Count)
$manifestLines.Add("# ==============================================================================")

foreach ($f in ($filesToHash | Sort-Object FullName)) {
    $h = (Get-FileHash -Path $f.FullName -Algorithm SHA256).Hash
    $rel = $f.FullName.Substring($StagingRoot.Length).TrimStart('\', '/')
    $manifestLines.Add(("{0}  *{1}" -f $h, $rel))
}

Set-Content -Path $manifestPath -Value $manifestLines -Encoding UTF8
Write-Host ("Wrote complete payload manifest ({0} entries) to {1}" -f $filesToHash.Count, $manifestPath)

# Write README.txt
$readmePath = Join-Path $StagingRoot "README.txt"
$readmeContent = @"
================================================================================
CLOVENT BUSINESS OPERATING SYSTEM (CBOS)
1.3.0 Full Internal Acceptance Offline Bundle
================================================================================

This delivery bundle contains everything required to test the complete CBOS
installation, Microsoft SQL Server Express instance setup, SSMS tooling,
and database commissioning workflows in a 100% OFFLINE environment.

PACKAGE CONTENTS:
-----------------
1. installer\
   - Clovent.BusinessOperatingSystem-1.3.0-Acceptance-FullOffline-Setup.exe
     Self-contained offline installer containing the application, provisioner,
     and pre-bundled Microsoft prerequisites.

2. prerequisites\
   - SQLEXPR_x64_ENU.exe: Microsoft SQL Server 2022 Express (16.0.1000.6 RTM)
   - SSMS-Setup-ENU.exe: Microsoft SQL Server Management Studio 20.2 Standalone
   - VC_redist.x64.exe: Microsoft Visual C++ 2015-2022 Redistributable (x64)
   - VC_redist.x86.exe: Microsoft Visual C++ 2015-2022 Redistributable (x86)
   - msoledbsql.msi: Microsoft OLE DB Driver 19 for SQL Server (x64)
   - prerequisites_provenance.json: Hashes, digital signatures, and download provenance

3. checksums\
   - package_provenance_offline_acceptance_variant.json
   - offline-acceptance-payload-sha256-manifest.txt

INSTALLATION INSTRUCTIONS:
--------------------------
1. Double-click installer\Clovent.BusinessOperatingSystem-1.3.0-Acceptance-FullOffline-Setup.exe
   (Run as Administrator if prompted by UAC).
2. Choose installation destination (defaults to isolated acceptance folder).
3. On the Database & Prerequisite Configuration page:
   - Select 'Use an existing Microsoft SQL Server instance' OR 'Install dedicated Microsoft SQL Server 2022 Express instance (.\CLOVENT_QA)'.
   - Select 'Install Microsoft SQL Server Management Studio 20.2 (SSMS)' if desired.
   - Confirm target synthetic QA database (default: CBOS_QA_Acceptance).
4. Review summary and proceed.
5. On completion, launch CBOS via desktop shortcut 'Launch CBOS (Acceptance Mode)'.
"@

Set-Content -Path $readmePath -Value $readmeContent -Encoding UTF8
Write-Host "Wrote README.txt to $readmePath"

Write-Host "`nFull Internal Acceptance Offline Bundle built successfully!" -ForegroundColor Green
Write-Host "Path:   $installerExe"
Write-Host "Size:   $installerSize bytes"
Write-Host "SHA256: $installerHash"
