Write-Section "Solution Analysis"

Push-Location $Global:SolutionFolder

try
{
    $projects = dotnet sln list

    foreach($line in $projects)
    {
        if([string]::IsNullOrWhiteSpace($line))
        {
            continue
        }

        if($line -match "^Project")
        {
            continue
        }

        if($line -match "^-+$")
        {
            continue
        }

        Success $line
    }
}
finally
{
    Pop-Location
}
