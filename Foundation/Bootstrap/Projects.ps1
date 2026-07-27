Write-Section "Project Validation"

Push-Location $Global:SolutionFolder

try
{
    $projects = dotnet sln list |
        Where-Object {
            $_ -like "*.csproj"
        }

    foreach($project in $projects)
    {
        $fullPath = Join-Path $Global:SolutionFolder $project

        if(Test-Path $fullPath)
        {
            Success "Found: $project"

            try
            {
                [xml]$xml = Get-Content $fullPath

                $sdk = $xml.Project.Sdk

                if([string]::IsNullOrWhiteSpace($sdk))
                {
                    $sdk = "Unknown"
                }

                Success "SDK : $sdk"

                $framework = $xml.Project.PropertyGroup.TargetFramework

                if($framework)
                {
                    Success "Target : $framework"
                }
                else
                {
                    Failure "Target Framework Missing"
                }
            }
            catch
            {
                Failure "Invalid project file : $project"
            }
        }
        else
        {
            Failure "Missing : $project"
        }

        Write-Host ""
    }
}
finally
{
    Pop-Location
}
