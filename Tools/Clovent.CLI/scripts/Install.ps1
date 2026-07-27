$PackageRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$PackageRoot = Split-Path -Parent $PackageRoot

$ProjectRoot = "D:\Clovent Business Operating System\Tools\Clovent.CLI"

Write-Host "Installing package..."

Copy-Item "$PackageRoot\src\*" "$ProjectRoot\src" -Recurse -Force -ErrorAction SilentlyContinue

Set-Location $ProjectRoot

dotnet restore
dotnet build

Write-Host "Installation complete."
