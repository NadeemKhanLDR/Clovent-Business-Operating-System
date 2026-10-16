# D:\CBOS\Repository\scripts\package_candidate_a83554b.ps1
$ErrorActionPreference = "Stop"

$repoRoot = "D:\CBOS\Repository"
$runDir = "D:\CBOS\Repository\artifacts\acceptance_run_20261014_1830"
$installerDir = Join-Path $runDir "installer"
$payloadDir = Join-Path $runDir "payload"
$payloadDriverDir = Join-Path $payloadDir "driver\StandalonePosUiAcceptanceDriver"
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
$candidateInstaller = Join-Path $installerDir "Clovent.BusinessOperatingSystem-1.3.0-Setup-a83554b.exe"
Copy-Item -LiteralPath $builtInstaller -Destination $candidateInstaller -Force

# Preserve frozen candidate b4a98f5
$frozenSource = "D:\CBOS\Repository\artifacts\acceptance_run_20261013_1320\installer\Clovent.BusinessOperatingSystem-1.3.0-Setup-b4a98f5.exe"
$frozenDest = Join-Path $installerDir "Clovent.BusinessOperatingSystem-1.3.0-Setup-b4a98f5.exe"
Copy-Item -LiteralPath $frozenSource -Destination $frozenDest -Force

Write-Host "=== 5. Copying authoritative driver to payload ==="
$authoritativeDriverDir = "C:\Users\NexGen\.gemini\antigravity\brain\59eff724-c78a-4baa-990d-841d80a4faf7\scratch\StandalonePosUiAcceptanceDriver\publish"
Copy-Item "$authoritativeDriverDir\*" $payloadDriverDir -Recurse -Force

Write-Host "=== 6. Recording Package Provenance ==="
$hashes = [ordered]@{
    CandidateCommit = "a83554bf329c21b15fb36367d831e8aefaf7c51b"
    CandidateBranch = "fix/refund-allocation-and-ui-acceptance"
    GeneratedAtUtc = [DateTimeOffset]::UtcNow.ToString("o")
    FrozenInstaller_b4a98f5 = @{
        Path = $frozenDest
        Sha256 = (Get-FileHash -LiteralPath $frozenDest -Algorithm SHA256).Hash
        SizeBytes = (Get-Item $frozenDest).Length
    }
    CandidateInstaller_a83554b = @{
        Path = $candidateInstaller
        Sha256 = (Get-FileHash -LiteralPath $candidateInstaller -Algorithm SHA256).Hash
        SizeBytes = (Get-Item $candidateInstaller).Length
    }
    DesktopExe = @{
        Path = "$relDir\Clovent.Desktop.exe"
        Sha256 = (Get-FileHash -LiteralPath "$relDir\Clovent.Desktop.exe" -Algorithm SHA256).Hash
        SizeBytes = (Get-Item "$relDir\Clovent.Desktop.exe").Length
    }
    ProvisionerExe = @{
        Path = "$provDir\Clovent.Installer.Provisioner.exe"
        Sha256 = (Get-FileHash -LiteralPath "$provDir\Clovent.Installer.Provisioner.exe" -Algorithm SHA256).Hash
        SizeBytes = (Get-Item "$provDir\Clovent.Installer.Provisioner.exe").Length
    }
    AuthoritativeDriverExe = @{
        SourcePath = "$authoritativeDriverDir\StandalonePosUiAcceptanceDriver.exe"
        PayloadPath = "$payloadDriverDir\StandalonePosUiAcceptanceDriver.exe"
        Sha256 = (Get-FileHash -LiteralPath "$payloadDriverDir\StandalonePosUiAcceptanceDriver.exe" -Algorithm SHA256).Hash
        SizeBytes = (Get-Item "$payloadDriverDir\StandalonePosUiAcceptanceDriver.exe").Length
    }
}
$provenancePath = Join-Path $runDir "package_provenance_a83554b.json"
$hashes | ConvertTo-Json -Depth 5 | Set-Content $provenancePath -Encoding UTF8
Write-Host "Package provenance written to $provenancePath"
$hashes | ConvertTo-Json -Depth 5
