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

Write-Host "`n=== PACKAGING SUCCESSFUL ==="
Write-Host "Installer: $builtInstaller"
Write-Host "SHA-256:   $installerHash"
Write-Host "Size:      $installerSize bytes"
Write-Host "Desktop:   $desktopExe"
Write-Host "SHA-256:   $desktopHash"
