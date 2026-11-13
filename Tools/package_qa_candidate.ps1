# tools\package_qa_candidate.ps1
param(
    [string]$StagingDir = "C:\CBOS_ManualQA_20261111\qa-installer-variant-20e413d"
)

$ErrorActionPreference = "Stop"

$repoRoot = $PSScriptRoot | Split-Path -Parent
Set-Location $repoRoot

$buildDir = Join-Path $StagingDir "build"
$relDir = Join-Path $StagingDir "release"
$installerDir = Join-Path $StagingDir "installer"
$logsDir = Join-Path $StagingDir "logs"

if (Test-Path $StagingDir) { Remove-Item $StagingDir -Recurse -Force }
New-Item -ItemType Directory -Path $buildDir, $relDir, $installerDir, $logsDir -Force | Out-Null

Write-Host "=== 1. Publishing Clovent.Desktop (Release win-x64 self-contained on C:) ==="
dotnet publish "$repoRoot\src\Clovent.Desktop\Clovent.Desktop.csproj" `
    -c Release -r win-x64 --self-contained true `
    -p:UseArtifactsOutput=true -p:ArtifactsPath="$buildDir" `
    -p:DebugType=None -p:DebugSymbols=false `
    -o "$relDir" --nologo *>&1 | Tee-Object -FilePath "$logsDir\publish-desktop.log"

if ($LASTEXITCODE -ne 0) { throw "Desktop publish failed with exit code $LASTEXITCODE" }
Get-ChildItem -Path $relDir -Include "*.pdb","*Development*.json","*.lic" -Recurse -File -ErrorAction SilentlyContinue | Remove-Item -Force

Write-Host "=== 2. Running ReleaseGuard ScanReleasePackage.ps1 ==="
powershell -NoProfile -ExecutionPolicy Bypass -File "$repoRoot\tools\ReleaseGuard\ScanReleasePackage.ps1" -ReleaseDir $relDir *>&1 | Tee-Object -FilePath "$logsDir\releaseguard.log"
if ($LASTEXITCODE -ne 0) { throw "ReleaseGuard failed with exit code $LASTEXITCODE" }

Write-Host "=== 3. Compiling Inno Setup QA Installer (cbos-qa-setup.iss) ==="
$iscc = "C:\Users\NexGen\AppData\Local\Programs\Inno Setup 6\ISCC.exe"
if (-not (Test-Path $iscc)) { throw "ISCC.exe not found at $iscc" }

& $iscc "/DPublishDir=$relDir" "/DOutputDir=$installerDir" "$repoRoot\tools\installer\cbos-qa-setup.iss" *>&1 | Tee-Object -FilePath "$logsDir\iscc-compile.log"
if ($LASTEXITCODE -ne 0) { throw "ISCC compilation failed with exit code $LASTEXITCODE" }

$builtInstaller = Join-Path $installerDir "Clovent.BusinessOperatingSystem-1.3.0-QA-Setup.exe"
if (-not (Test-Path $builtInstaller)) { throw "QA Installer not generated at $builtInstaller" }

$installerHash = (Get-FileHash $builtInstaller -Algorithm SHA256).Hash
$installerSize = (Get-Item $builtInstaller).Length
$desktopExe = Join-Path $relDir "Clovent.Desktop.exe"
$desktopHash = (Get-FileHash $desktopExe -Algorithm SHA256).Hash

# 4. Verify Shipped Runtime Versions & Security Remediation
Write-Host "`n=== 4. Verifying Shipped Runtime Pack Resolution ==="
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

# 5. Generate Manifest of Packaged Release Files
Write-Host "`n=== 5. Generating Package Manifest & Checksums ==="
$manifestLines = [System.Collections.Generic.List[string]]::new()
$relFiles = Get-ChildItem $relDir -Recurse -File | Sort-Object FullName
foreach ($f in $relFiles) {
    $relPath = $f.FullName.Substring($relDir.Length + 1)
    $h = (Get-FileHash $f.FullName -Algorithm SHA256).Hash
    $manifestLines.Add("$h  release\$relPath")
}
$tmplDir = Join-Path $repoRoot "tools\installer\templates"
foreach ($tf in (Get-ChildItem $tmplDir -Recurse -File | Sort-Object FullName)) {
    $relPath = $tf.FullName.Substring($tmplDir.Length + 1)
    $h = (Get-FileHash $tf.FullName -Algorithm SHA256).Hash
    $manifestLines.Add("$h  templates\$relPath")
}
$launchCmd = Join-Path $repoRoot "tools\installer\Launch-QA.cmd"
$launchHash = (Get-FileHash $launchCmd -Algorithm SHA256).Hash
$manifestLines.Add("$launchHash  installer\Launch-QA.cmd")

$manifestPath = Join-Path $StagingDir "qa-release-payload-sha256-manifest.txt"
$manifestLines | Set-Content -Path $manifestPath -Encoding UTF8

$manifest = [ordered]@{
    ReleaseDesignation = "INTERNAL QA VARIANT / UNSIGNED"
    Product = "Clovent Business Operating System (Internal QA)"
    Version = "1.3.0"
    Milestone = "1.3.0-qa-variant"
    CandidateCommit = $gitCommit
    CandidateShortCommit = $gitShort
    CandidateBranch = $gitBranch
    SourceTreeClean = $isClean
    GeneratedAtUtc = [DateTimeOffset]::UtcNow.ToString("o")
    AuthenticodeSignature = [ordered]@{
        Status = "NotSigned (INTERNAL QA VARIANT ONLY)"
        SigningCertificateAvailable = $false
        WaiverScope = "Internal QA test qualification only; DOES NOT authorize customer deployment, commercial GA, or customer-release signing waiver."
    }
    SafetyConstraints = [ordered]@{
        ApplicationFilesOnly = $true
        SqlInstallationExcluded = $true
        DatabaseProvisioningExcluded = $true
        AutomaticLaunchExcluded = $true
        NormalModeShortcutExcluded = $true
        SharedProductionAclExcluded = $true
        PreservesExistingDataOnUninstall = $true
        DistinctAppId = "{B0A7CA9A-QA01-47F1-9BD7-A107CB052026}"
        DistinctInstallDir = "{autopf}\Clovent\Business Operating System QA"
    }
    Artifacts = [ordered]@{
        Installer = [ordered]@{
            FileName = "Clovent.BusinessOperatingSystem-1.3.0-QA-Setup.exe"
            Path = $builtInstaller
            Sha256 = $installerHash
            SizeBytes = $installerSize
        }
        DesktopExecutable = [ordered]@{
            FileName = "Clovent.Desktop.exe"
            Sha256 = $desktopHash
            SizeBytes = (Get-Item $desktopExe).Length
        }
        LaunchScript = [ordered]@{
            FileName = "Launch-QA.cmd"
            Sha256 = $launchHash
            SizeBytes = (Get-Item $launchCmd).Length
        }
    }
}

$provJsonPath = Join-Path $StagingDir "package_provenance_qa_variant.json"
$manifest | ConvertTo-Json -Depth 5 | Set-Content -Path $provJsonPath -Encoding UTF8
Write-Host "Wrote provenance JSON to $provJsonPath"
Write-Host "Wrote payload manifest ($($manifestLines.Count) entries) to $manifestPath"
Write-Host "Installer built successfully: $builtInstaller ($installerSize bytes, SHA-256: $installerHash)"
