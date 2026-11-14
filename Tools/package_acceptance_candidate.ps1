# tools\package_acceptance_candidate.ps1
param(
    [string]$StagingDir = "C:\CBOS_ManualQA_20261111\qa-full-acceptance-variant-20e413d"
)

$ErrorActionPreference = "Stop"

$repoRoot = $PSScriptRoot | Split-Path -Parent
Set-Location $repoRoot

$buildDir = Join-Path $StagingDir "build"
$relDir = Join-Path $StagingDir "release"
$provDir = Join-Path $StagingDir "provisioner"
$installerDir = Join-Path $StagingDir "installer"
$logsDir = Join-Path $StagingDir "logs"

if (Test-Path $StagingDir) { Remove-Item $StagingDir -Recurse -Force }
New-Item -ItemType Directory -Path $buildDir, $relDir, $provDir, $installerDir, $logsDir -Force | Out-Null

Write-Host "=== 1. Publishing Clovent.Desktop (Release win-x64 self-contained on C:) ==="
dotnet publish "$repoRoot\src\Clovent.Desktop\Clovent.Desktop.csproj" `
    -c Release -r win-x64 --self-contained true `
    -p:UseArtifactsOutput=true -p:ArtifactsPath="$buildDir" `
    -p:DebugType=None -p:DebugSymbols=false `
    -o "$relDir" --nologo *>&1 | Tee-Object -FilePath "$logsDir\publish-desktop.log"

if ($LASTEXITCODE -ne 0) { throw "Desktop publish failed with exit code $LASTEXITCODE" }
Get-ChildItem -Path $relDir -Include "*.pdb","*Development*.json","*.lic" -Recurse -File -ErrorAction SilentlyContinue | Remove-Item -Force

Write-Host "=== 2. Publishing Clovent.Installer.Provisioner (Release win-x64 single-file on C:) ==="
dotnet publish "$repoRoot\Tools\Clovent.Installer.Provisioner\Clovent.Installer.Provisioner.csproj" `
    -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true `
    -p:UseArtifactsOutput=true -p:ArtifactsPath="$buildDir" `
    -p:DebugType=None -p:DebugSymbols=false `
    -o "$provDir" --nologo *>&1 | Tee-Object -FilePath "$logsDir\publish-provisioner.log"

if ($LASTEXITCODE -ne 0) { throw "Provisioner publish failed with exit code $LASTEXITCODE" }
Get-ChildItem -Path $provDir -Filter "*.pdb" -Recurse -File -ErrorAction SilentlyContinue | Remove-Item -Force

Write-Host "=== 3. Running ReleaseGuard ScanReleasePackage.ps1 ==="
powershell -NoProfile -ExecutionPolicy Bypass -File "$repoRoot\Tools\ReleaseGuard\ScanReleasePackage.ps1" -ReleaseDir $relDir *>&1 | Tee-Object -FilePath "$logsDir\releaseguard.log"
if ($LASTEXITCODE -ne 0) { throw "ReleaseGuard failed with exit code $LASTEXITCODE" }

Write-Host "=== 4. Compiling Inno Setup Full Acceptance Installer (cbos-acceptance-setup.iss) ==="
$iscc = "C:\Users\NexGen\AppData\Local\Programs\Inno Setup 6\ISCC.exe"
if (-not (Test-Path $iscc)) { throw "ISCC.exe not found at $iscc" }

& $iscc "/DPublishDir=$relDir" "/DProvisionerDir=$provDir" "/DOutputDir=$installerDir" "$repoRoot\Tools\installer\cbos-acceptance-setup.iss" *>&1 | Tee-Object -FilePath "$logsDir\iscc-compile.log"
if ($LASTEXITCODE -ne 0) { throw "ISCC compilation failed with exit code $LASTEXITCODE" }

$builtInstaller = Join-Path $installerDir "Clovent.BusinessOperatingSystem-1.3.0-Acceptance-Setup.exe"
if (-not (Test-Path $builtInstaller)) { throw "Acceptance Installer not generated at $builtInstaller" }

$installerHash = (Get-FileHash $builtInstaller -Algorithm SHA256).Hash
$installerSize = (Get-Item $builtInstaller).Length
$desktopExe = Join-Path $relDir "Clovent.Desktop.exe"
$desktopHash = (Get-FileHash $desktopExe -Algorithm SHA256).Hash
$provisionerExe = Join-Path $provDir "Clovent.Installer.Provisioner.exe"
$provisionerHash = (Get-FileHash $provisionerExe -Algorithm SHA256).Hash

# 5. Verify Shipped Runtime Versions & Security Remediation
Write-Host "`n=== 5. Verifying Shipped Runtime Pack Resolution ==="
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

$gitCommit = (git rev-parse HEAD).ToString().Trim()
$gitShort = (git rev-parse --short HEAD).ToString().Trim()
$gitBranch = (git branch --show-current).ToString().Trim()
$rawGitStatus = git status --porcelain
$isClean = ($null -eq $rawGitStatus -or [string]::IsNullOrWhiteSpace(($rawGitStatus | Out-String)))

# 6. Generate Manifest of Packaged Release Files
Write-Host "`n=== 6. Generating Complete Package Manifest & Checksums ==="
$manifestLines = [System.Collections.Generic.List[string]]::new()
$relFiles = Get-ChildItem $relDir -Recurse -File | Sort-Object FullName
foreach ($f in $relFiles) {
    $relPath = $f.FullName.Substring($relDir.Length + 1)
    $h = (Get-FileHash $f.FullName -Algorithm SHA256).Hash
    $manifestLines.Add("$h  release\$relPath")
}
$provFiles = Get-ChildItem $provDir -Recurse -File | Sort-Object FullName
foreach ($pf in $provFiles) {
    $relPath = $pf.FullName.Substring($provDir.Length + 1)
    $h = (Get-FileHash $pf.FullName -Algorithm SHA256).Hash
    $manifestLines.Add("$h  provisioner\$relPath")
}
$tmplDir = Join-Path $repoRoot "Tools\installer\templates"
foreach ($tf in (Get-ChildItem $tmplDir -Recurse -File | Sort-Object FullName)) {
    $relPath = $tf.FullName.Substring($tmplDir.Length + 1)
    $h = (Get-FileHash $tf.FullName -Algorithm SHA256).Hash
    $manifestLines.Add("$h  templates\$relPath")
}
$launchCmd = Join-Path $repoRoot "Tools\installer\Launch-Acceptance.cmd"
$launchHash = (Get-FileHash $launchCmd -Algorithm SHA256).Hash
$manifestLines.Add("$launchHash  installer\Launch-Acceptance.cmd")

$issPath = Join-Path $repoRoot "Tools\installer\cbos-acceptance-setup.iss"
$issHash = (Get-FileHash $issPath -Algorithm SHA256).Hash
$manifestLines.Add("$issHash  installer\cbos-acceptance-setup.iss")

$manifestPath = Join-Path $StagingDir "acceptance-release-payload-sha256-manifest.txt"
$manifestLines | Set-Content -Path $manifestPath -Encoding UTF8

$manifest = [ordered]@{
    ReleaseDesignation = "INTERNAL ACCEPTANCE FULL INSTALLER / UNSIGNED"
    Product = "Clovent Business Operating System (Internal Acceptance)"
    Version = "1.3.0"
    Milestone = "1.3.0-full-acceptance"
    ApplicationBaseline = "20e413dbe9cfb2951150779ed015f0227a8211e8"
    CandidateCommit = $gitCommit
    CandidateShortCommit = $gitShort
    CandidateBranch = $gitBranch
    SourceTreeClean = $isClean
    GeneratedAtUtc = [DateTimeOffset]::UtcNow.ToString("o")
    AuthenticodeSignature = [ordered]@{
        Status = "NotSigned (INTERNAL ACCEPTANCE ONLY)"
        SigningCertificateAvailable = $false
        WaiverScope = "Internal acceptance test qualification only; signing credentials unavailable in internal build environment. Customer deployment or commercial GA strictly requires authentic code signing."
    }
    PrerequisiteInventory = [ordered]@{
        DotNetRuntime = [ordered]@{
            Type = "OfflineBundled"
            Framework = ".NET 10.0.13 WindowsDesktop / NETCore win-x64"
            Deployment = "Self-contained in application payload"
            DownloadRequired = $false
        }
        DatabaseProvisioner = [ordered]@{
            Type = "OfflineBundled"
            Executable = "Clovent.Installer.Provisioner.exe"
            Deployment = "Single-file win-x64 self-contained"
            DownloadRequired = $false
        }
        SqlServerExpress = [ordered]@{
            Type = "SupportedOption"
            DefaultAction = "Use existing SQL instance (e.g. .\\SQLEXPRESS, (local), (localdb)\\MSSQLLocalDB)"
            OptionalInstallation = "Microsoft SQL Server 2022 Express (Instance: .\\CLOVENT_QA)"
            OfflineMedia = "Checked at {src}\\SQLEXPR_x64_ENU.exe or {src}\\prerequisites\\SQLEXPR_x64_ENU.exe"
            OnlineDownloadUrl = "https://download.microsoft.com/download/3/8/d/38de7036-2433-4207-8eae-06e247e17b25/SQLEXPR_x64_ENU.exe"
            DownloadRequired = "Only if dedicated SQL installation is selected and offline media is not placed in installer folder"
        }
    }
    SafetyConstraints = [ordered]@{
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
    Artifacts = [ordered]@{
        Installer = [ordered]@{
            FileName = "Clovent.BusinessOperatingSystem-1.3.0-Acceptance-Setup.exe"
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
            SizeBytes = (Get-Item $provisionerExe).Length
        }
        LaunchScript = [ordered]@{
            FileName = "Launch-Acceptance.cmd"
            Sha256 = $launchHash
            SizeBytes = (Get-Item $launchCmd).Length
        }
    }
}

$provJsonPath = Join-Path $StagingDir "package_provenance_acceptance_variant.json"
$manifest | ConvertTo-Json -Depth 6 | Set-Content -Path $provJsonPath -Encoding UTF8
Write-Host "Wrote provenance JSON to $provJsonPath"
Write-Host "Wrote complete payload manifest ($($manifestLines.Count) entries) to $manifestPath"
Write-Host "`nFull Internal Acceptance Installer built successfully!"
Write-Host "Path:   $builtInstaller"
Write-Host "Size:   $installerSize bytes"
Write-Host "SHA256: $installerHash"
