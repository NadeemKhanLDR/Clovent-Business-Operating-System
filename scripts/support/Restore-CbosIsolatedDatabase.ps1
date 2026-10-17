<#
.SYNOPSIS
    Restores a CBOS database backup into a NEW isolated developer/test database,
    verifying checksums, applying file relocation, and sanitizing the environment.
.DESCRIPTION
    Guarantees never to overwrite existing databases by default.
    Prohibits restoring to operational or customer databases.
    Applies environment isolation: disables outbox sync, background jobs, physical printing,
    and handles machine-bound DPAPI configuration.
.EXAMPLE
    .\Restore-CbosIsolatedDatabase.ps1 -BackupFilePath "C:\Backups\demo.bak" -TargetDatabaseName "CBOS_Support_INC1042_Isolated"
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$BackupFilePath,

    [Parameter(Mandatory = $true)]
    [string]$TargetDatabaseName,

    [Parameter(Mandatory = $false)]
    [string]$Server = "localhost",

    [Parameter(Mandatory = $false)]
    [switch]$UseWindowsAuth,

    [Parameter(Mandatory = $false)]
    [string]$SqlUser,

    [Parameter(Mandatory = $false)]
    [System.Security.SecureString]$SqlPassword,

    [Parameter(Mandatory = $false)]
    [string]$ExpectedSha256,

    [Parameter(Mandatory = $false)]
    [switch]$AllowOverwriteExisting,

    [Parameter(Mandatory = $false)]
    [switch]$SkipEnvironmentProtection
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = "Stop"

$modulePath = Join-Path $PSScriptRoot "CbosSupportModule.psm1"
Import-Module $modulePath -Force

$restoreResult = Restore-CbosIsolatedDatabase `
    -BackupFilePath $BackupFilePath `
    -TargetDatabaseName $TargetDatabaseName `
    -Server $Server `
    -UseWindowsAuth:$UseWindowsAuth `
    -SqlUser $SqlUser `
    -SqlPassword $SqlPassword `
    -ExpectedSha256 $ExpectedSha256 `
    -AllowOverwriteExisting:$AllowOverwriteExisting

if (-not $SkipEnvironmentProtection) {
    Protect-CbosRestoredEnvironment `
        -TargetDatabaseName $TargetDatabaseName `
        -Server $Server `
        -UseWindowsAuth:$UseWindowsAuth `
        -SqlUser $SqlUser `
        -SqlPassword $SqlPassword
}

return $restoreResult
