# ==============================================================================
# Clovent Business Operating System (CBOS) - Prerequisite Fetcher & Signature Verifier
# Target: Microsoft SQL Server 2022 Express, SSMS 20, Visual C++, MSOLEDBSQL
# ==============================================================================
[CmdletBinding()]
param(
    [string]$TargetDir = "C:\CBOS_ManualQA_20261111\prerequisites_cache"
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path $TargetDir)) {
    New-Item -ItemType Directory -Path $TargetDir -Force | Out-Null
}

$prerequisites = @(
    @{
        Name = "SQL Server 2022 Express (x64, ENU)";
        FileName = "SQLEXPR_x64_ENU.exe";
        Url = "https://download.microsoft.com/download/3/8/d/38de7036-2433-4207-8eae-06e247e17b25/SQLEXPR_x64_ENU.exe";
        ExpectedMinSize = 250MB;
        Description = "Microsoft SQL Server 2022 Express (16.0.1000.6 RTM)";
    },
    @{
        Name = "SQL Server Management Studio 20.2 (ENU)";
        FileName = "SSMS-Setup-ENU.exe";
        Url = "https://download.microsoft.com/download/7519f0ff-997c-4f36-b5aa-9a51d47dd34c/SSMS-Setup-ENU.exe";
        ExpectedMinSize = 450MB;
        Description = "Microsoft SQL Server Management Studio 20.2 Standalone Offline Setup";
    },
    @{
        Name = "Microsoft Visual C++ 2015-2022 Redistributable (x64)";
        FileName = "VC_redist.x64.exe";
        Url = "https://download.visualstudio.microsoft.com/download/pr/e9604e21-4b79-4096-a4b3-1b63d4b55b4d/CC0FF0EB1DC3F5188AE6300FAEF32BF5BEEBA4BDD6E8E445A9184072096B713B/VC_redist.x64.exe";
        ExpectedMinSize = 20MB;
        Description = "Visual C++ 2015-2022 Redistributable 64-bit runtime";
    },
    @{
        Name = "Microsoft Visual C++ 2015-2022 Redistributable (x86)";
        FileName = "VC_redist.x86.exe";
        Url = "https://download.visualstudio.microsoft.com/download/pr/e9604e21-4b79-4096-a4b3-1b63d4b55b4d/0C09F2611660441084CE0DF425C51C11E147E6447963C3690F97E0B25C55ED64/VC_redist.x86.exe";
        ExpectedMinSize = 10MB;
        Description = "Visual C++ 2015-2022 Redistributable 32-bit runtime (MSOLEDBSQL dependency)";
    },
    @{
        Name = "Microsoft OLE DB Driver 19 for SQL Server (x64)";
        FileName = "msoledbsql.msi";
        Url = "https://download.microsoft.com/download/bbb0d870-7a04-44d9-af17-af28575a0ed8/amd64/1033/msoledbsql.msi";
        ExpectedMinSize = 5MB;
        Description = "Microsoft OLE DB Driver 19 for SQL Server 64-bit MSI";
    }
)

$provenanceResults = @()

foreach ($item in $prerequisites) {
    $destPath = Join-Path $TargetDir $item.FileName
    Write-Host ("=== Processing prerequisite: " + $item.Name + " ===") -ForegroundColor Cyan

    $needsDownload = $true
    if (Test-Path $destPath) {
        $existingSize = (Get-Item $destPath).Length
        if ($existingSize -ge $item.ExpectedMinSize) {
            Write-Host ("Existing file found: $destPath ($existingSize bytes). Verifying signature...") -ForegroundColor Yellow
            $sig = Get-AuthenticodeSignature $destPath
            if ($sig.Status -eq 'Valid' -and $sig.SignerCertificate.Subject -match 'Microsoft') {
                Write-Host "Authenticode signature is VALID and issued by Microsoft." -ForegroundColor Green
                $needsDownload = $false
            } else {
                Write-Host "Signature check failed or invalid. Re-downloading..." -ForegroundColor Yellow
            }
        }
    }

    if ($needsDownload) {
        Write-Host "Downloading from: $($item.Url)..." -ForegroundColor Cyan
        $tempDownload = "$destPath.downloading"
        if (Test-Path $tempDownload) { Remove-Item $tempDownload -Force }

        $start = Get-Date
        # Use curl.exe for reliable resumed download with progress
        $curlArgs = @("-L", "-f", "-s", "-S", "--show-error", "-o", $tempDownload, $item.Url)
        $proc = Start-Process -FilePath "curl.exe" -ArgumentList $curlArgs -NoNewWindow -Wait -PassThru
        if ($proc.ExitCode -ne 0) {
            throw "curl download failed for $($item.Name) with exit code $($proc.ExitCode)"
        }

        Move-Item -Path $tempDownload -Destination $destPath -Force
        $duration = (Get-Date) - $start
        Write-Host ("Download completed in {0:N1} seconds." -f $duration.TotalSeconds) -ForegroundColor Green
    }

    # Verify signature
    $sig = Get-AuthenticodeSignature $destPath
    if ($sig.Status -ne 'Valid') {
        throw "Digital signature verification FAILED for $($item.FileName): status is $($sig.Status)"
    }
    if ($sig.SignerCertificate.Subject -notmatch 'Microsoft') {
        throw "Signer certificate for $($item.FileName) is not Microsoft: $($sig.SignerCertificate.Subject)"
    }

    $hash = (Get-FileHash -Path $destPath -Algorithm SHA256).Hash
    $size = (Get-Item $destPath).Length

    Write-Host ("Verified: $($item.FileName)") -ForegroundColor Green
    Write-Host ("  Size:    $size bytes")
    Write-Host ("  SHA-256: $hash")
    Write-Host ("  Signer:  $($sig.SignerCertificate.Subject)")

    $provenanceResults += [PSCustomObject]@{
        Name = $item.Name
        FileName = $item.FileName
        SourceUrl = $item.Url
        LocalPath = $destPath
        SizeBytes = $size
        Sha256 = $hash
        AuthenticodeStatus = $sig.Status.ToString()
        SignerSubject = $sig.SignerCertificate.Subject
        SignerThumbprint = $sig.SignerCertificate.Thumbprint
        Description = $item.Description
    }
}

$provenanceJson = Join-Path $TargetDir "prerequisites_provenance.json"
$provenanceResults | ConvertTo-Json -Depth 5 | Set-Content -Path $provenanceJson -Encoding UTF8
Write-Host "All Microsoft prerequisites successfully downloaded and verified!" -ForegroundColor Green
Write-Host "Provenance written to: $provenanceJson"
