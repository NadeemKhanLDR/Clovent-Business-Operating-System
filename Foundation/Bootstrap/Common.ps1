# ==========================================================
# Clovent Business Operating System
#
# Bootstrap Common Library
#
# Version : 1.0.0
# ==========================================================

$ErrorActionPreference = "Stop"

$Global:Root = "D:\Clovent Business Operating System"

$Global:SolutionFolder =
"$Global:Root\Tools\Clovent.CLI"

$Global:Solution =
"$Global:SolutionFolder\Clovent.CLI.slnx"

$Global:LogFolder =
"$Global:Root\Foundation\Logs"

New-Item `
-ItemType Directory `
-Force `
-Path $Global:LogFolder | Out-Null

$Global:LogFile =
"$Global:LogFolder\Bootstrap.log"

function Write-Section
{
    param([string]$Title)

    Write-Host ""

    Write-Host "------------------------------------------------" -ForegroundColor Cyan

    Write-Host $Title -ForegroundColor Yellow

    Write-Host "------------------------------------------------" -ForegroundColor Cyan
}

function Success
{
    param([string]$Text)

    Write-Host "✔ $Text" -ForegroundColor Green

    Add-Content $Global:LogFile "SUCCESS : $Text"
}

function Failure
{
    param([string]$Text)

    Write-Host "✘ $Text" -ForegroundColor Red

    Add-Content $Global:LogFile "FAILED : $Text"
}

function Validate-Path
{
    param([string]$Path)

    if(Test-Path $Path)
    {
        Success $Path
    }
    else
    {
        Failure $Path
        throw "Missing path: $Path"
    }
}

function Validate-Command
{
    param([string]$Command)

    if(Get-Command $Command -ErrorAction SilentlyContinue)
    {
        Success "$Command installed"
    }
    else
    {
        Failure "$Command not installed"
        throw "$Command not installed"
    }
}
