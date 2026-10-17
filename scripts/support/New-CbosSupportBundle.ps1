<#
.SYNOPSIS
    Generates a secure customer support bundle containing database backup, SHA-256 checksum,
    application/SQL/migration version manifest, and sanitized operational logs.
.DESCRIPTION
    Requires explicit customer consent. Strictly forbids automatic cloud upload.
.EXAMPLE
    .\New-CbosSupportBundle.ps1 -CustomerConsent -CustomerOrganization "Main St Diner" -AuthorizedBy "John Doe (Owner)" -TicketId "INC-1042"
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
    [string]$TicketId,

    [Parameter(Mandatory = $false)]
    [string]$CorrelationId,

    [Parameter(Mandatory = $false)]
    [string]$CustomerOrganization = "Acme Retailers",

    [Parameter(Mandatory = $false)]
    [string]$AuthorizedBy = "Store Manager",

    [Parameter(Mandatory = $false)]
    [switch]$CustomerConsent,

    [Parameter(Mandatory = $false)]
    [string]$OutputDirectory,

    [Parameter(Mandatory = $false)]
    [int]$LogRetentionDays = 3
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = "Stop"

$modulePath = Join-Path $PSScriptRoot "CbosSupportModule.psm1"
Import-Module $modulePath -Force

New-CbosSupportBundle `
    -Server $Server `
    -Database $Database `
    -UseWindowsAuth:$UseWindowsAuth `
    -SqlUser $SqlUser `
    -SqlPassword $SqlPassword `
    -TicketId $TicketId `
    -CorrelationId $CorrelationId `
    -CustomerOrganization $CustomerOrganization `
    -AuthorizedBy $AuthorizedBy `
    -CustomerConsent:$CustomerConsent `
    -OutputDirectory $OutputDirectory `
    -LogRetentionDays $LogRetentionDays
