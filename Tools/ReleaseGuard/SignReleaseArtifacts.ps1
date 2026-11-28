<#
.SYNOPSIS
    Authenticode signing and verification script for CBOS release artifacts.
.DESCRIPTION
    Signs Clovent.Desktop.exe, Clovent.Installer.Provisioner.exe, and Clovent.BusinessOperatingSystem-1.3.0-Setup.exe
    using a configured code-signing certificate (PFX via environment variable or Windows Certificate Store thumbprint),
    applies an RFC 3161 SHA-256 timestamp, and verifies each signature with signtool.exe.
    Never generates or uses self-signed/fake certificates.
.PARAMETER TargetPaths
    One or more executable/installer paths to sign and verify.
.PARAMETER PfxPath
    Optional path to a .pfx certificate outside the repository (defaults to $env:CBOS_SIGN_PFX_PATH).
.PARAMETER PfxPassword
    Optional password for the .pfx certificate (defaults to $env:CBOS_SIGN_PFX_PASSWORD).
.PARAMETER CertThumbprint
    Optional certificate thumbprint in Windows Certificate Store (defaults to $env:CBOS_SIGN_THUMBPRINT).
.PARAMETER TimestampUrl
    RFC 3161 timestamp server URL (defaults to $env:CBOS_SIGN_TIMESTAMP_URL or http://timestamp.digicert.com).
.PARAMETER RequireSigning
    If set (or $env:CBOS_REQUIRE_SIGNING -eq 'true'), fails with exit code 1 when signing cannot be performed.
#>
[CmdletBinding(PositionalBinding = $false)]
param(
    [Parameter(Mandatory = $true)]
    [string[]]$TargetPaths,
    [string]$PfxPath = $env:CBOS_SIGN_PFX_PATH,
    [string]$PfxPassword = $env:CBOS_SIGN_PFX_PASSWORD,
    [string]$CertThumbprint = $env:CBOS_SIGN_THUMBPRINT,
    [string]$TimestampUrl = $(if ($env:CBOS_SIGN_TIMESTAMP_URL) { $env:CBOS_SIGN_TIMESTAMP_URL } else { "http://timestamp.digicert.com" }),
    [switch]$RequireSigning
)

$ErrorActionPreference = "Stop"
$mustSign = $RequireSigning.IsPresent -or ($env:CBOS_REQUIRE_SIGNING -eq "true") -or ($env:CBOS_REQUIRE_SIGNING -eq "1")

function Find-SignTool {
    $cmd = Get-Command "signtool.exe" -ErrorAction SilentlyContinue
    if ($cmd) { return $cmd.Source }

    $kitsRoot = "${env:ProgramFiles(x86)}\Windows Kits\10\bin"
    if (Test-Path $kitsRoot) {
        $found = Get-ChildItem -Path $kitsRoot -Filter "signtool.exe" -Recurse -ErrorAction SilentlyContinue |
            Where-Object { $_.FullName -match "\\x64\\signtool\.exe$" } |
            Sort-Object FullName -Descending |
            Select-Object -First 1
        if ($found) { return $found.FullName }
    }
    return $null
}

foreach ($p in $TargetPaths) {
    if (-not (Test-Path $p)) {
        Write-Host "[ERROR] Signing target not found: $p" -ForegroundColor Red
        exit 1
    }
}

$hasPfx = -not [string]::IsNullOrWhiteSpace($PfxPath)
$hasThumbprint = -not [string]::IsNullOrWhiteSpace($CertThumbprint)

if (-not $hasPfx -and -not $hasThumbprint) {
    $msg = "Authenticode signing BLOCKED: No signing certificate configured (set CBOS_SIGN_PFX_PATH or CBOS_SIGN_THUMBPRINT)."
    if ($mustSign) {
        Write-Host "[ERROR] $msg" -ForegroundColor Red
        exit 1
    }
    Write-Host "[WARN] $msg" -ForegroundColor Yellow
    exit 0
}

if ($hasPfx -and -not (Test-Path $PfxPath)) {
    Write-Host "[ERROR] Configured PFX certificate file not found: $PfxPath" -ForegroundColor Red
    exit 1
}

$signTool = Find-SignTool
if (-not $signTool) {
    $msg = "Authenticode signing BLOCKED: signtool.exe not found in PATH or Windows Kits 10."
    if ($mustSign) {
        Write-Host "[ERROR] $msg" -ForegroundColor Red
        exit 1
    }
    Write-Host "[WARN] $msg" -ForegroundColor Yellow
    exit 0
}

Write-Host "Using signtool: $signTool" -ForegroundColor Cyan

foreach ($target in $TargetPaths) {
    $resolved = (Resolve-Path $target).Path
    Write-Host "Signing: $resolved" -ForegroundColor Cyan

    $signArgs = @("sign", "/fd", "SHA256", "/tr", $TimestampUrl, "/td", "SHA256")
    if ($hasPfx) {
        $signArgs += @("/f", $PfxPath)
        if (-not [string]::IsNullOrWhiteSpace($PfxPassword)) {
            $signArgs += @("/p", $PfxPassword)
        }
    } else {
        $signArgs += @("/sha1", $CertThumbprint.Trim(), "/sm")
    }
    $signArgs += $resolved

    & $signTool @signArgs
    if ($LASTEXITCODE -ne 0) {
        Write-Host "[ERROR] signtool sign failed for $resolved (Exit code: $LASTEXITCODE)" -ForegroundColor Red
        exit $LASTEXITCODE
    }

    Write-Host "Verifying signature & timestamp: $resolved" -ForegroundColor Cyan
    & $signTool verify /pa /v $resolved
    if ($LASTEXITCODE -ne 0) {
        Write-Host "[ERROR] signtool verify failed for $resolved (Exit code: $LASTEXITCODE)" -ForegroundColor Red
        exit $LASTEXITCODE
    }

    $sig = Get-AuthenticodeSignature -FilePath $resolved
    if ($sig.Status -ne [System.Management.Automation.SignatureStatus]::Valid -or -not $sig.TimeStamperCertificate) {
        Write-Host "[ERROR] Signature or timestamp validation failed for $resolved (Status: $($sig.Status))" -ForegroundColor Red
        exit 1
    }

    Write-Host "[PASS] Signed and timestamped: $resolved (Subject: $($sig.SignerCertificate.Subject))" -ForegroundColor Green
}

exit 0
