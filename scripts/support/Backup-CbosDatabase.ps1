<#
.SYNOPSIS
    Creates an authorized full SQL Server backup of the CBOS database with privilege verification.
.DESCRIPTION
    Ensures that support staff privileges are verified and separated from frontline cashier accounts,
    creates a consistent full backup with COPY_ONLY and CHECKSUM, and verifies backup integrity.
    Never hardcodes or logs passwords.
.EXAMPLE
    .\Backup-CbosDatabase.ps1 -Server "localhost" -Database "Clovent_BusinessOperatingSystem" -UseWindowsAuth
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $false)]
    [string]$Server = "localhost",

    [Parameter(Mandatory = $false)]
    [string]$Database = "Clovent_BusinessOperatingSystem",

    [Parameter(Mandatory = $false)]
    [switch]$UseWindowsAuth,

    [Parameter(Mandatory = $false)]
    [string]$SqlUser,

    [Parameter(Mandatory = $false)]
    [System.Security.SecureString]$SqlPassword,

    [Parameter(Mandatory = $false)]
    [string]$BackupDirectory,

    [Parameter(Mandatory = $false)]
    [string]$CustomBackupFileName
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = "Stop"

$modulePath = Join-Path $PSScriptRoot "CbosSupportModule.psm1"
Import-Module $modulePath -Force

Backup-CbosDatabase `
    -Server $Server `
    -Database $Database `
    -UseWindowsAuth:$UseWindowsAuth `
    -SqlUser $SqlUser `
    -SqlPassword $SqlPassword `
    -BackupDirectory $BackupDirectory `
    -CustomBackupFileName $CustomBackupFileName
