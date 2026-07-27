Write-Section ".NET SDK"

dotnet --list-sdks | ForEach-Object {

    Success $_
}

Write-Section "Git Version"

Success (git --version)

Write-Section "PowerShell Version"

Success $PSVersionTable.PSVersion

Write-Section "Visual Studio"

$vsPath =
"C:\Program Files\Microsoft Visual Studio\18\Insiders\Common7\IDE\devenv.exe"

if(Test-Path $vsPath)
{
    Success $vsPath
}
else
{
    Failure "Visual Studio not found"
}
