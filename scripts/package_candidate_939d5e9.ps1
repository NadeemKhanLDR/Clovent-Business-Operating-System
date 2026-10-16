# D:\CBOS\Repository\scripts\package_candidate_939d5e9.ps1
$ErrorActionPreference = "Stop"

$repoRoot = "D:\CBOS\Repository"
$commitSha = "939d5e939032e6a86d1bd3c3506707911470bff1"
$commitShort = "939d5e9"
$runDir = "D:\CBOS\Repository\artifacts\acceptance_run_20261015_1630"
$installerDir = Join-Path $runDir "installer"
$payloadDir = Join-Path $runDir "payload"
$payloadDriverDir = Join-Path $payloadDir "driver"
$evidenceDir = Join-Path $runDir "evidence"

New-Item -ItemType Directory -Path $installerDir -Force | Out-Null
New-Item -ItemType Directory -Path $payloadDriverDir -Force | Out-Null
New-Item -ItemType Directory -Path $evidenceDir -Force | Out-Null

$relDir = "$repoRoot\artifacts\release\Clovent.BusinessOperatingSystem-win-x64"
$provDir = "$repoRoot\Tools\Clovent.Installer.Provisioner\bin\publish"

if (Test-Path $relDir) { Remove-Item $relDir -Recurse -Force }
if (Test-Path $provDir) { Remove-Item $provDir -Recurse -Force }

Write-Host "=== 1. Publishing Clovent.Desktop (Release win-x64 self-contained) ==="
dotnet publish "$repoRoot\src\Clovent.Desktop\Clovent.Desktop.csproj" -c Release -r win-x64 --self-contained true -p:DebugType=None -p:DebugSymbols=false -o $relDir
if ($LASTEXITCODE -ne 0) { throw "Desktop publish failed with exit code $LASTEXITCODE" }
Get-ChildItem -Path $relDir -Include "*.pdb","*Development*.json","*.lic" -Recurse -File -ErrorAction SilentlyContinue | Remove-Item -Force

Write-Host "=== 2. Publishing Clovent.Installer.Provisioner (Release win-x64 single-file) ==="
dotnet publish "$repoRoot\Tools\Clovent.Installer.Provisioner\Clovent.Installer.Provisioner.csproj" -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:DebugType=None -p:DebugSymbols=false -o $provDir
if ($LASTEXITCODE -ne 0) { throw "Provisioner publish failed with exit code $LASTEXITCODE" }
Get-ChildItem -Path $provDir -Filter "*.pdb" -Recurse -File -ErrorAction SilentlyContinue | Remove-Item -Force

Write-Host "=== 3. Running ReleaseGuard ScanReleasePackage.ps1 ==="
powershell -NoProfile -ExecutionPolicy Bypass -File "$repoRoot\Tools\ReleaseGuard\ScanReleasePackage.ps1" -ReleaseDir $relDir
if ($LASTEXITCODE -ne 0) { throw "ReleaseGuard failed with exit code $LASTEXITCODE" }

Write-Host "=== 4. Compiling Inno Setup Installer (cbos-setup.iss) ==="
$iscc = "C:\Users\NexGen\AppData\Local\Programs\Inno Setup 6\ISCC.exe"
if (-not (Test-Path $iscc)) { $iscc = "$repoRoot\Tools\InnoSetup\ISCC.exe" }
& $iscc "$repoRoot\Tools\installer\cbos-setup.iss"
if ($LASTEXITCODE -ne 0) { throw "ISCC compilation failed with exit code $LASTEXITCODE" }

$builtInstaller = "$repoRoot\artifacts\installer\Clovent.BusinessOperatingSystem-1.3.0-Setup.exe"
$candidateInstaller = Join-Path $installerDir "Clovent.BusinessOperatingSystem-1.3.0-Setup-$commitShort.exe"
Copy-Item -LiteralPath $builtInstaller -Destination $candidateInstaller -Force

# Copy frozen candidate b4a98f5
$frozenSource = "D:\CBOS\Repository\artifacts\acceptance_run_20261015_1030\installer\Clovent.BusinessOperatingSystem-1.3.0-Setup-b4a98f5.exe"
$frozenDest = Join-Path $installerDir "Clovent.BusinessOperatingSystem-1.3.0-Setup-b4a98f5.exe"
Copy-Item -LiteralPath $frozenSource -Destination $frozenDest -Force

# Copy driver tools from existing payload
$priorDriverDir = "D:\CBOS\Repository\artifacts\acceptance_run_20261015_1520\payload\driver"
Copy-Item "$priorDriverDir\*" $payloadDriverDir -Recurse -Force

Write-Host "=== 5. Recording Package Provenance ==="
$desktopExe = "$relDir\Clovent.Desktop.exe"
$provExe = "$provDir\Clovent.Installer.Provisioner.exe"

$hashes = [ordered]@{
    CandidateCommit = $commitSha
    CandidateBranch = "fix/refund-allocation-and-ui-acceptance"
    GeneratedAtUtc = [DateTimeOffset]::UtcNow.ToString("o")
    FrozenInstaller_b4a98f5 = @{
        Path = $frozenDest
        Sha256 = (Get-FileHash $frozenDest -Algorithm SHA256).Hash
        SizeBytes = (Get-Item $frozenDest).Length
    }
    CandidateInstaller_939d5e9 = @{
        Path = $candidateInstaller
        Sha256 = (Get-FileHash $candidateInstaller -Algorithm SHA256).Hash
        SizeBytes = (Get-Item $candidateInstaller).Length
    }
    DesktopExe = @{
        Path = $desktopExe
        Sha256 = (Get-FileHash $desktopExe -Algorithm SHA256).Hash
        SizeBytes = (Get-Item $desktopExe).Length
    }
    ProvisionerExe = @{
        Path = $provExe
        Sha256 = (Get-FileHash $provExe -Algorithm SHA256).Hash
        SizeBytes = (Get-Item $provExe).Length
    }
}

$provJsonPath = Join-Path $runDir "package_provenance_$commitShort.json"
$hashes | ConvertTo-Json -Depth 5 | Set-Content -Path $provJsonPath -Encoding UTF8
Write-Host "Provenance written to $provJsonPath"
Get-Content $provJsonPath
