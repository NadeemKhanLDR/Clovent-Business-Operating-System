<#
.SYNOPSIS
    Automated Production Release Packaging Security Guard for Clovent Business Operating System.
.DESCRIPTION
    Scans the published release folder and verifies that no forbidden files, sensitive secrets,
    private keys, development settings, or test credentials exist in the release distribution.
    Exits with code 0 on success, or code 1 on any violation.
.PARAMETER ReleaseDir
    Path to the release folder to scan. Defaults to artifacts/release/Clovent.BusinessOperatingSystem-win-x64
#>
param(
    [string]$ReleaseDir = "artifacts\release\Clovent.BusinessOperatingSystem-win-x64"
)

$ErrorActionPreference = "Stop"

$fullPath = Resolve-Path $ReleaseDir -ErrorAction SilentlyContinue
if (-not $fullPath -or -not (Test-Path $ReleaseDir)) {
    Write-Host "[ERROR] Target release directory does not exist: $ReleaseDir" -ForegroundColor Red
    exit 1
}

$releasePath = $fullPath.Path
Write-Host "================================================================================" -ForegroundColor Cyan
Write-Host "CLOVENT BUSINESS OPERATING SYSTEM - RELEASE SECURITY SCAN" -ForegroundColor Cyan
Write-Host "Target: $releasePath" -ForegroundColor Cyan
Write-Host "================================================================================" -ForegroundColor Cyan

$violations = @()

# 1. Private signing keys & certificates
$keyPatterns = @("*.privatekey", "*.pfx", "*.pem", "*.key", "*private_key*.xml", "*vendor_private_key*")
foreach ($pat in $keyPatterns) {
    $found = Get-ChildItem -Path $releasePath -Filter $pat -Recurse -File -ErrorAction SilentlyContinue |
        Where-Object { $_.Name -notlike "System.Private.*" }
    foreach ($f in $found) {
        $violations += "FORBIDDEN KEY FILE: $($f.FullName)"
    }
}

# 2. Development settings
$devSettings = Get-ChildItem -Path $releasePath -Filter "*Development*.json" -Recurse -File -ErrorAction SilentlyContinue
foreach ($f in $devSettings) {
    $violations += "DEVELOPMENT SETTINGS FILE FOUND: $($f.FullName)"
}

# 3. Development / Test licenses
$devLicenses = Get-ChildItem -Path $releasePath -Filter "*development*.lic" -Recurse -File -ErrorAction SilentlyContinue
$devLicenses += Get-ChildItem -Path $releasePath -Filter "*test*.lic" -Recurse -File -ErrorAction SilentlyContinue
foreach ($f in $devLicenses) {
    $violations += "DEVELOPMENT/TEST LICENSE FOUND: $($f.FullName)"
}

# 4. Universal / Wildcard license in generic release
$genericLicense = Get-ChildItem -Path $releasePath -Filter "clovent.lic" -Recurse -File -ErrorAction SilentlyContinue
foreach ($f in $genericLicense) {
    $violations += "UNIVERSAL/UNAUTHORIZED LICENSE IN GENERIC RELEASE: $($f.FullName) (Generic production release must NOT bundle an active license)"
}

# 5. Source code files (*.cs, *.csproj, *.sln, *.slnx)
$sourcePatterns = @("*.cs", "*.csproj", "*.sln", "*.slnx", "*.vb", "*.fs")
foreach ($pat in $sourcePatterns) {
    $found = Get-ChildItem -Path $releasePath -Filter $pat -Recurse -File -ErrorAction SilentlyContinue
    foreach ($f in $found) {
        $violations += "SOURCE CODE FILE FOUND: $($f.FullName)"
    }
}

# 6. PDB debug symbol files
$pdbs = Get-ChildItem -Path $releasePath -Filter "*.pdb" -Recurse -File -ErrorAction SilentlyContinue
foreach ($f in $pdbs) {
    $violations += "PDB DEBUG SYMBOL FOUND: $($f.FullName)"
}

# 7. Database backup and data files (*.bak, *.mdf, *.ldf, *.sqlite, *.db)
$dbPatterns = @("*.bak", "*.mdf", "*.ldf", "*.ndf", "*.sqlite", "*.db")
foreach ($pat in $dbPatterns) {
    $found = Get-ChildItem -Path $releasePath -Filter $pat -Recurse -File -ErrorAction SilentlyContinue
    foreach ($f in $found) {
        $violations += "DATABASE/BACKUP FILE FOUND: $($f.FullName)"
    }
}

# 8. Content inspection for sensitive secrets (Admin123!, plain-text SQL passwords, RSA Private Key markers, dev connection strings)
$textFiles = Get-ChildItem -Path $releasePath -Recurse -File | Where-Object {
    $_.Extension -in @(".json", ".config", ".xml", ".txt", ".lic")
}

foreach ($tf in $textFiles) {
    $content = Get-Content $tf.FullName -Raw -ErrorAction SilentlyContinue
    if (-not $content) { continue }

    if ($content -match "Admin123!") {
        $violations += "DEFAULT TEST PASSWORD (Admin123!) FOUND IN: $($tf.FullName)"
    }
    if ($content -match "BEGIN RSA PRIVATE KEY" -or $content -match "BEGIN PRIVATE KEY" -or ($content -match "<RSAKeyValue>" -and $content -match "<D>")) {
        $violations += "PRIVATE SIGNING KEY DATA FOUND IN: $($tf.FullName)"
    }
    if ($content -match 'Password\s*=\s*(?!;)(?!["'']?\s*;)[^;]{3,}' -or $content -match '"Password"\s*:\s*"[^"]{3,}"') {
        $violations += "POTENTIAL PLAINTEXT SQL PASSWORD FOUND IN: $($tf.FullName)"
    }
}

# Report results
Write-Host "Scan completed." -ForegroundColor Gray
Write-Host "Total files inspected: $((Get-ChildItem -Path $releasePath -Recurse -File).Count)" -ForegroundColor Gray

if ($violations.Count -gt 0) {
    Write-Host "`n[SECURITY VIOLATIONS DETECTED: $($violations.Count)]" -ForegroundColor Red
    foreach ($v in $violations) {
        Write-Host "  - $v" -ForegroundColor Red
    }
    Write-Host "`nRelease security scan FAILED." -ForegroundColor Red
    exit 1
} else {
    Write-Host "`n[PASS] No private keys, dev licenses, source files, PDBs, backup files, or plain text passwords found." -ForegroundColor Green
    Write-Host "Production release package passes all security gate requirements." -ForegroundColor Green
    exit 0
}
