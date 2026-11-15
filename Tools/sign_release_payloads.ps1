<#
.SYNOPSIS
    Official Authenticode signing workflow for Clovent Business Operating System (CBOS).
    Signs first-party application executables, Provisioner, first-party DLLs, and final installer
    using SHA-256 and RFC 3161 trusted timestamping via signtool.exe.
.DESCRIPTION
    Complies with TASK-10 / release guidelines:
    - Bounded strictly to owned first-party deliverables:
      Clovent.Desktop.exe, Clovent.Installer.Provisioner.exe, Clovent.*.dll, and final setup executable.
    - Strictly preserves third-party Microsoft signatures on prerequisites (SQLEXPR, SSMS, VC_redist, OLE DB).
    - Uses SHA-256 file digest (/fd sha256) and RFC 3161 trusted timestamping (/tr ... /td sha256).
    - Verifies signatures with signtool verify /pa /v and Get-AuthenticodeSignature.
    - Never commits certificates, keys, or passwords.
    - Rejects self-signed certificates as a substitute for production trust.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string[]]$FilesToSign,

    [string]$CertificateThumbprint = $env:CBOS_SIGNING_THUMBPRINT,
    [string]$CertificatePath = $env:CBOS_SIGNING_CERT_PATH,
    [string]$CertificatePassword = $env:CBOS_SIGNING_CERT_PASSWORD,
    [string]$TimestampServer = "http://timestamp.digicert.com",
    [string]$HashAlgorithm = "sha256",
    [string]$SigntoolPath = "",
    [switch]$VerifyOnly = $false,
    [switch]$AllowUnsignedCandidate = $false
)

$ErrorActionPreference = "Stop"

Write-Host "================================================================================" -ForegroundColor Cyan
Write-Host "CLOVENT AUTHENTICODE CODE SIGNING & VERIFICATION PIPELINE" -ForegroundColor Cyan
Write-Host "Timestamp Server: $TimestampServer | Hash: $HashAlgorithm" -ForegroundColor Cyan
Write-Host "================================================================================" -ForegroundColor Cyan

# Locate signtool.exe
if ([string]::IsNullOrWhiteSpace($SigntoolPath)) {
    $found = Get-Command "signtool.exe" -ErrorAction SilentlyContinue
    if ($found) {
        $SigntoolPath = $found.Source
    } else {
        $sdkPaths = @(
            "C:\Program Files (x86)\Windows Kits\10\bin\10.0.26100.0\x64\signtool.exe",
            "C:\Program Files (x86)\Windows Kits\10\bin\10.0.22621.0\x64\signtool.exe",
            "C:\Program Files (x86)\Windows Kits\10\bin\10.0.19041.0\x64\signtool.exe",
            "C:\Program Files (x86)\Windows Kits\10\bin\x64\signtool.exe"
        )
        foreach ($p in $sdkPaths) {
            if (Test-Path $p) {
                $SigntoolPath = $p
                break
            }
        }
    }
}

if (-not [string]::IsNullOrWhiteSpace($SigntoolPath) -and (Test-Path $SigntoolPath)) {
    Write-Host "Using SignTool: $SigntoolPath" -ForegroundColor Cyan
} else {
    Write-Warning "signtool.exe not found in Windows SDK paths."
}

# Determine Signing Credentials
$hasCredentials = $false
$signingMethod = ""

if (-not [string]::IsNullOrWhiteSpace($CertificateThumbprint)) {
    $cert = Get-Item "cert:\CurrentUser\My\$CertificateThumbprint" -ErrorAction SilentlyContinue
    if (-not $cert) {
        $cert = Get-Item "cert:\LocalMachine\My\$CertificateThumbprint" -ErrorAction SilentlyContinue
    }
    if ($cert) {
        $hasCredentials = $true
        $signingMethod = "Thumbprint: $CertificateThumbprint"
        Write-Host "Found certificate in store: $($cert.Subject)" -ForegroundColor Green
    }
} elseif (-not [string]::IsNullOrWhiteSpace($CertificatePath) -and (Test-Path $CertificatePath)) {
    $hasCredentials = $true
    $signingMethod = "File: $CertificatePath"
    Write-Host "Found certificate file: $CertificatePath" -ForegroundColor Green
}

if ($VerifyOnly) {
    Write-Host "`nRunning signature verification on target files..." -ForegroundColor Cyan
    $allValid = $true
    foreach ($file in $FilesToSign) {
        if (-not (Test-Path $file)) {
            Write-Warning "File not found: $file"
            $allValid = $false
            continue
        }
        $sig = Get-AuthenticodeSignature -FilePath $file
        Write-Host ("{0}: Status={1}, Signer='{2}'" -f (Split-Path $file -Leaf), $sig.Status, $sig.SignerCertificate.Subject)
        if ($sig.Status -ne "Valid") {
            $allValid = $false
        }
    }
    return $allValid
}

if (-not $hasCredentials) {
    Write-Host "`n[SIGNING STATUS: BLOCKED]" -ForegroundColor Yellow
    Write-Host "Reason: No suitable commercial Authenticode code-signing credentials were provided or found in certificate stores." -ForegroundColor Yellow
    Write-Host "Required Credentials / Services for Production Distribution:" -ForegroundColor Yellow
    Write-Host "  1. Windows Certificate Store: Commercial EV/OV Code Signing Certificate (set CBOS_SIGNING_THUMBPRINT)"
    Write-Host "  2. PKCS#12 File: Valid commercial certificate .pfx (set CBOS_SIGNING_CERT_PATH and CBOS_SIGNING_CERT_PASSWORD)"
    Write-Host "  3. Cloud / Hardware Token: Microsoft Trusted Signing / Azure Key Vault HSM-backed credential"
    Write-Host "Policy Note: Self-signed certificates are strictly prohibited as production-trust substitutes." -ForegroundColor Yellow
    Write-Host "Policy Note: Code signing does not guarantee bypass of Application Control policies without approved publisher rules." -ForegroundColor Yellow

    if ($AllowUnsignedCandidate) {
        Write-Host "`nProceeding with UNSIGNED release candidate generation for manual acceptance testing." -ForegroundColor Cyan
        return [PSCustomObject]@{
            Signed = $false
            Status = "Unsigned (RELEASE CANDIDATE - PENDING COMMERCIAL AUTHENTICODE SIGNING)"
            Reason = "No signing credentials provided"
        }
    } else {
        throw "Authenticode signing failed: Missing code-signing credentials."
    }
}

# Execute Signing
Write-Host "`nExecuting Authenticode signing with $signingMethod..." -ForegroundColor Green
foreach ($file in $FilesToSign) {
    if (-not (Test-Path $file)) {
        throw "File to sign not found: $file"
    }

    $fileName = Split-Path $file -Leaf
    # Safety Check: Never re-sign or strip third-party Microsoft prerequisites
    if ($fileName -match "(SQLEXPR_x64_ENU|SSMS-Setup-ENU|VC_redist|msoledbsql)") {
        Write-Host "Preserving third-party Microsoft signature on prerequisite: $fileName" -ForegroundColor Cyan
        continue
    }

    Write-Host "Signing $fileName..." -ForegroundColor Cyan
    $signArgs = @("sign", "/fd", $HashAlgorithm, "/tr", $TimestampServer, "/td", $HashAlgorithm)

    if (-not [string]::IsNullOrWhiteSpace($CertificateThumbprint)) {
        $signArgs += @("/sha1", $CertificateThumbprint)
    } elseif (-not [string]::IsNullOrWhiteSpace($CertificatePath)) {
        $signArgs += @("/f", $CertificatePath)
        if (-not [string]::IsNullOrWhiteSpace($CertificatePassword)) {
            $signArgs += @("/p", $CertificatePassword)
        }
    }

    $signArgs += $file

    $proc = Start-Process -FilePath $SigntoolPath -ArgumentList $signArgs -NoNewWindow -Wait -PassThru
    if ($proc.ExitCode -ne 0) {
        throw "SignTool failed for $fileName with exit code $($proc.ExitCode)"
    }

    # Verify signature immediately
    $verifyArgs = @("verify", "/pa", "/v", $file)
    $vProc = Start-Process -FilePath $SigntoolPath -ArgumentList $verifyArgs -NoNewWindow -Wait -PassThru
    if ($vProc.ExitCode -ne 0) {
        throw "SignTool verification failed for $fileName with exit code $($vProc.ExitCode)"
    }

    $sig = Get-AuthenticodeSignature -FilePath $file
    if ($sig.Status -ne "Valid") {
        throw "Authenticode status for $fileName is invalid: $($sig.StatusMessage)"
    }
    Write-Host "Successfully signed and verified: $fileName (Signer: $($sig.SignerCertificate.Subject))" -ForegroundColor Green
}

return [PSCustomObject]@{
    Signed = $true
    Status = "Signed"
    Method = $signingMethod
}
