Write-Host ""
Write-Host "=============================================" -ForegroundColor Green
Write-Host "      Clovent Bootstrap Started" -ForegroundColor Cyan
Write-Host "=============================================" -ForegroundColor Green
Write-Host ""

$Root = "D:\Clovent Business Operating System"

$Folders = @(
    "00 Vision",
    "01 Product Strategy",
    "02 Business Analysis",
    "03 SDLC",
    "04 UI UX Standards",
    "05 Software Architecture",
    "06 Coding Standards",
    "07 Domain Driven Design",
    "08 Database Design",
    "09 Security",
    "10 AI Architecture",
    "11 Platform Services",
    "12 Restaurant POS",
    "13 ADR",
    "Templates",
    "Tools",
    "Tools\PowerShell",
    "Tools\Generated"
)

foreach($Folder in $Folders)
{
    New-Item -ItemType Directory -Force -Path (Join-Path $Root $Folder) | Out-Null
}

Write-Host ""
Write-Host "Folder structure verified." -ForegroundColor Green
Write-Host ""

$Dotnet = dotnet --version

Write-Host "Installed .NET SDK : $Dotnet" -ForegroundColor Yellow

Write-Host ""
Write-Host "Bootstrap Complete." -ForegroundColor Green
