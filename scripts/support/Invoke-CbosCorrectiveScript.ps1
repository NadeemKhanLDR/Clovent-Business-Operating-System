<#
.SYNOPSIS
    Executes a reviewed corrective script against a CBOS database with preview diagnosis,
    fresh pre-fix backup, transactional execution, repeat-execution protection, and audit logging.
.DESCRIPTION
    Strictly enforces PDR-0003: Prohibits rewriting completed sales, payments, refunds, taxes, or ledger history.
    Financial corrections must use approved compensating transactions.
.EXAMPLE
    # Preview mode:
    .\Invoke-CbosCorrectiveScript.ps1 -ScriptFilePath ".\corrective_scripts\FIX_sample.sql" -TargetDatabaseName "CBOS_Support_INC1042_Isolated" -DryRun

    # Transactional execution:
    .\Invoke-CbosCorrectiveScript.ps1 -ScriptFilePath ".\corrective_scripts\FIX_sample.sql" -TargetDatabaseName "CBOS_Support_INC1042_Isolated" -Execute -PreTestedOnRestoredCopy
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$ScriptFilePath,

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
    [switch]$DryRun,

    [Parameter(Mandatory = $false)]
    [switch]$Execute,

    [Parameter(Mandatory = $false)]
    [switch]$PreTestedOnRestoredCopy,

    [Parameter(Mandatory = $false)]
    [string]$CompensationReference,

    [Parameter(Mandatory = $false)]
    [string]$BackupDirectory,

    [Parameter(Mandatory = $false)]
    [switch]$AllowUnapprovedScript,

    [Parameter(Mandatory = $false)]
    [string]$TargetCustomerId
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = "Stop"

$modulePath = Join-Path $PSScriptRoot "CbosSupportModule.psm1"
Import-Module $modulePath -Force

Invoke-CbosCorrectiveScript `
    -ScriptFilePath $ScriptFilePath `
    -TargetDatabaseName $TargetDatabaseName `
    -Server $Server `
    -UseWindowsAuth:$UseWindowsAuth `
    -SqlUser $SqlUser `
    -SqlPassword $SqlPassword `
    -DryRun:$DryRun `
    -Execute:$Execute `
    -PreTestedOnRestoredCopy:$PreTestedOnRestoredCopy `
    -CompensationReference $CompensationReference `
    -BackupDirectory $BackupDirectory `
    -AllowUnapprovedScript:$AllowUnapprovedScript `
    -TargetCustomerId $TargetCustomerId
