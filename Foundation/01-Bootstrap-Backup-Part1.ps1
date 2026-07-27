# ==========================================================
# Clovent Business Operating System
#
# Foundation Script 01
#
# Bootstrap
#
# Version : 1.0.0
# ==========================================================

$ErrorActionPreference = "Stop"

Clear-Host

Write-Host ""
Write-Host "=============================================" -ForegroundColor Cyan
Write-Host " Clovent Business Operating System" -ForegroundColor Yellow
Write-Host " Bootstrap" -ForegroundColor Yellow
Write-Host "=============================================" -ForegroundColor Cyan
Write-Host ""

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
    param([string]$Text)

    Write-Host ""
    Write-Host "------------------------------------------------" -ForegroundColor DarkCyan
    Write-Host $Text -ForegroundColor Cyan
    Write-Host "------------------------------------------------" -ForegroundColor DarkCyan

    Add-Content $Global:LogFile ""
    Add-Content $Global:LogFile $Text
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
        throw
    }
}

Write-Section "Validating Repository"

Validate-Path $Global:Root

Validate-Path $Global:SolutionFolder

Validate-Path $Global:Solution

Write-Host ""
Write-Host "Bootstrap Part 1 completed." -ForegroundColor Green
