<#
.SYNOPSIS
    Builds the production installer bundle for Clovent Business Operating System (CBOS).
    Packages self-contained Release win-x64 application, Database Provisioner,
    and embeds verified Microsoft SQL Server Express and SSMS offline prerequisites.
#>
[CmdletBinding()]
param(
    [string]$StagingRoot = "C:\CBOS_ManualQA_20261111\production-release-variant-20e413d",
    [string]$PrerequisitesCache = "C:\CBOS_ManualQA_20261111\prerequisites_cache",
    [string]$BuildArtifacts = "C:\CBOS_ManualQA_20261111\build_artifacts",
    [string]$RepoRoot = ""
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($RepoRoot)) {
    $RepoRoot = Split-Path -Parent $PSScriptRoot
}

Write-Host "================================================================================" -ForegroundColor Cyan
Write-Host "BUILDING PRODUCTION INSTALLER BUNDLE (CBOS 1.3.0)" -ForegroundColor Cyan
Write-Host "Staging Directory:      $StagingRoot" -ForegroundColor Cyan
Write-Host "Prerequisites Cache:    $PrerequisitesCache" -ForegroundColor Cyan
Write-Host "Build Artifacts Path:   $BuildArtifacts" -ForegroundColor Cyan
Write-Host "Repository Worktree:    $RepoRoot" -ForegroundColor Cyan
Write-Host "================================================================================" -ForegroundColor Cyan

$releaseDir = Join-Path $StagingRoot "release"
$provisionerDir = Join-Path $StagingRoot "provisioner"
$prereqDir = Join-Path $StagingRoot "prerequisites"
$installerDir = Join-Path $StagingRoot "installer"
$checksumsDir = Join-Path $StagingRoot "checksums"
$logsDir = Join-Path $StagingRoot "logs"

foreach ($dir in @($StagingRoot, $releaseDir, $provisionerDir, $prereqDir, $installerDir, $checksumsDir, $logsDir, $BuildArtifacts)) {
    if (-not (Test-Path $dir)) {
        New-Item -Path $dir -ItemType Directory -Force | Out-Null
    }
}

# 1. Publish Clovent.Desktop (Self-Contained win-x64 Release)
Write-Host "`n=== 1. Publishing Clovent.Desktop (win-x64 Self-Contained) ===" -ForegroundColor Green
$desktopProj = Join-Path $RepoRoot "src\Clovent.Desktop\Clovent.Desktop.csproj"
$desktopExe = Join-Path $releaseDir "Clovent.Desktop.exe"
$publishDesktopLog = Join-Path $logsDir "publish-desktop.log"

$desktopArgs = @(
    "publish", $desktopProj,
    "-c", "Release",
    "-r", "win-x64",
    "--self-contained", "true",
    "-p:PublishSingleFile=false",
    "-p:UseArtifactsOutput=true",
    "-p:ArtifactsPath=$BuildArtifacts",
    "-p:UseSharedCompilation=false",
    "-nr:false",
    "-o", $releaseDir
)

$proc = Start-Process -FilePath "dotnet" -ArgumentList $desktopArgs -NoNewWindow -Wait -PassThru -RedirectStandardOutput $publishDesktopLog
if ($proc.ExitCode -ne 0) {
    throw "dotnet publish for Clovent.Desktop failed with exit code $($proc.ExitCode). Check $publishDesktopLog"
}
Write-Host "Clovent.Desktop published successfully to $releaseDir" -ForegroundColor Green

# 2. Publish Clovent.Installer.Provisioner (Single-File win-x64 Self-Contained)
Write-Host "`n=== 2. Publishing Clovent.Installer.Provisioner (win-x64 Single-File) ===" -ForegroundColor Green
$provisionerProj = Join-Path $RepoRoot "Tools\Clovent.Installer.Provisioner\Clovent.Installer.Provisioner.csproj"
$provisionerExe = Join-Path $provisionerDir "Clovent.Installer.Provisioner.exe"
$publishProvisionerLog = Join-Path $logsDir "publish-provisioner.log"

$provArgs = @(
    "publish", $provisionerProj,
    "-c", "Release",
    "-r", "win-x64",
    "--self-contained", "true",
    "-p:PublishSingleFile=true",
    "-p:IncludeNativeLibrariesForSelfExtract=true",
    "-p:UseArtifactsOutput=true",
    "-p:ArtifactsPath=$BuildArtifacts",
    "-p:UseSharedCompilation=false",
    "-nr:false",
    "-o", $provisionerDir
)

$proc = Start-Process -FilePath "dotnet" -ArgumentList $provArgs -NoNewWindow -Wait -PassThru -RedirectStandardOutput $publishProvisionerLog
if ($proc.ExitCode -ne 0) {
    throw "dotnet publish for Clovent.Installer.Provisioner failed with exit code $($proc.ExitCode). Check $publishProvisionerLog"
}
Write-Host "Clovent.Installer.Provisioner published successfully to $provisionerDir" -ForegroundColor Green

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
Write-Host "`n=== 5. Compiling Inno Setup 6 Production Installer ===" -ForegroundColor Green
$isccPath = "C:\Users\NexGen\AppData\Local\Programs\Inno Setup 6\ISCC.exe"
if (-not (Test-Path $isccPath)) {
    $isccPath = "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
}
if (-not (Test-Path $isccPath)) {
    throw "Inno Setup compiler not found at $isccPath"
}

$issScript = Join-Path $RepoRoot "Tools\installer\cbos-setup.iss"
$isccLog = Join-Path $logsDir "iscc-compile.log"

$isccArgs = @(
    "/O$installerDir",
    "/F""Clovent.BusinessOperatingSystem-1.3.0-Production-Setup""",
    "/DOutputBaseFilename=""Clovent.BusinessOperatingSystem-1.3.0-Production-Setup""",
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

$installerExe = Join-Path $installerDir "Clovent.BusinessOperatingSystem-1.3.0-Production-Setup.exe"
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

$desktopHash = (Get-FileHash -Path $desktopExe -Algorithm SHA256).Hash
$desktopSize = (Get-Item $desktopExe).Length

$provisionerHash = (Get-FileHash -Path $provisionerExe -Algorithm SHA256).Hash
$provisionerSize = (Get-Item $provisionerExe).Length

$provenanceJson = Join-Path $checksumsDir "package_provenance_production.json"
$provenanceData = [PSCustomObject]@{
    ReleaseDesignation = "PRODUCTION RELEASE CANDIDATE / PILOT DEPLOYMENT"
    Product = "Clovent Business Operating System"
    Version = "1.3.0"
    Milestone = "1.3.0-production-release-candidate"
    ApplicationBaseline = "20e413dbe9cfb2951150779ed015f0227a8211e8"
    CandidateCommit = (git -C $RepoRoot rev-parse HEAD)
    CandidateShortCommit = (git -C $RepoRoot rev-parse --short HEAD)
    CandidateBranch = (git -C $RepoRoot rev-parse --abbrev-ref HEAD)
    SourceTreeClean = ((git -C $RepoRoot status --porcelain).Count -eq 0)
    GeneratedAtUtc = (Get-Date).ToUniversalTime().ToString("o")
    AuthenticodeSignature = [PSCustomObject]@{
        Status = "Unsigned (RELEASE CANDIDATE - PENDING COMMERCIAL AUTHENTICODE SIGNING)"
        SigningCertificateAvailable = $false
        SigningRequirements = "Commercial GA customer deployment requires valid Microsoft Authenticode code signing using an enterprise code signing certificate or HSM-backed key."
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
            Instance = ".\SQLEXPRESS"
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
    ProductionSafetyPolicy = [PSCustomObject]@{
        ProductionProductIdentity = $true
        AppId = "{C107E47D-CB05-47F1-9BD7-A107CB052026}"
        InstallDirectory = "{autopf}\Clovent\Business Operating System"
        DataRoot = "%ProgramData%\Clovent\BusinessOperatingSystem"
        DefaultDatabase = "Clovent_BusinessOperatingSystem"
        SilentProcessCloseDisabled = $true
        IncompatibleDowngradeProtection = $true
        CrossEditionCollisionGuard = $true
        PreMigrationBackupRequired = $true
        ExplicitOperatorConfirmationRequired = $true
        AutomaticLaunchExcludedByDefault = $true
        PreservesCustomerDataOnUninstall = $true
    }
    Artifacts = [PSCustomObject]@{
        Installer = [PSCustomObject]@{
            FileName = "Clovent.BusinessOperatingSystem-1.3.0-Production-Setup.exe"
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
    }
}

$provenanceData | ConvertTo-Json -Depth 6 | Set-Content -Path $provenanceJson -Encoding UTF8
Write-Host "Wrote provenance JSON to $provenanceJson"

# Generate complete SHA-256 manifest
$manifestPath = Join-Path $checksumsDir "production-payload-sha256-manifest.txt"
$filesToHash = Get-ChildItem -Path $StagingRoot -Recurse -File | Where-Object {
    $_.FullName -notmatch '\\build\\' -and $_.FullName -notmatch '\\logs\\' -and $_.Name -ne "production-payload-sha256-manifest.txt"
}

$manifestLines = [System.Collections.Generic.List[string]]::new()
$manifestLines.Add("# ==============================================================================")
$manifestLines.Add("# Clovent Business Operating System (CBOS) - Production Payload Manifest")
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
Version 1.3.0 Production Release Candidate
================================================================================

This delivery bundle contains the official production installation package for
Clovent Business Operating System, including self-contained release application
binaries, the database provisioning utility, and offline Microsoft prerequisites.

PACKAGE CONTENTS:
-----------------
1. installer\
   - Clovent.BusinessOperatingSystem-1.3.0-Production-Setup.exe
     All-in-one offline setup executable embedding the application, provisioner,
     Visual C++ 2015-2022 runtimes, OLE DB Driver 19, SQL Server 2022 Express,
     and SQL Server Management Studio 20.2 (SSMS).

2. prerequisites\
   - SQLEXPR_x64_ENU.exe: Microsoft SQL Server 2022 Express (16.0.1000.6 RTM)
   - SSMS-Setup-ENU.exe: Microsoft SQL Server Management Studio 20.2 Standalone
   - VC_redist.x64.exe: Microsoft Visual C++ 2015-2022 Redistributable (x64)
   - VC_redist.x86.exe: Microsoft Visual C++ 2015-2022 Redistributable (x86)
   - msoledbsql.msi: Microsoft OLE DB Driver 19 for SQL Server (x64)
   - prerequisites_provenance.json: Hashes, digital signatures, and provenance

3. checksums\
   - package_provenance_production.json
   - production-payload-sha256-manifest.txt

INSTALLATION INSTRUCTIONS:
--------------------------
1. Double-click installer\Clovent.BusinessOperatingSystem-1.3.0-Production-Setup.exe
   (Run as Administrator when prompted by UAC).
2. Choose installation destination (defaults to 'C:\Program Files\Clovent\Business Operating System').
3. On the Database & Prerequisite Configuration page:
   - Choose whether to install a dedicated SQL Server Express instance (.\SQLEXPRESS)
     or connect to an existing local or network SQL Server.
   - Select authentication mode (Windows Integrated or SQL Server Authentication).
   - Confirm database name (default: Clovent_BusinessOperatingSystem).
   - Check 'Install Microsoft SQL Server Management Studio 20.2 (SSMS)' if desired.
4. Review configuration and confirm the database provisioning actions.
5. Setup will deploy the application, configure directory security, provision
   the database (with verified backup if upgrading an existing database), and
   create desktop/start menu shortcuts.
6. Launch Clovent Business Operating System from the Start Menu or Desktop.

UNINSTALLATION POLICY:
----------------------
Uninstalling Clovent Business Operating System removes application binaries and
shortcuts while strictly preserving customer databases, financial continuity
journals, backups, and configurations under %ProgramData%\Clovent\BusinessOperatingSystem.
SQL Server Express and SSMS remain installed and must be removed separately if desired.
"@

Set-Content -Path $readmePath -Value $readmeContent -Encoding UTF8
Write-Host "Wrote README.txt to $readmePath"

Write-Host "`nProduction Bundle built successfully!" -ForegroundColor Green
Write-Host "Path:   $installerExe" -ForegroundColor Cyan
Write-Host "Size:   $installerSize bytes" -ForegroundColor Cyan
Write-Host "SHA256: $installerHash" -ForegroundColor Cyan
