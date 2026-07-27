Write-Section "Build Validation"

Push-Location $Global:SolutionFolder

try
{
    Write-Host ""

    Write-Host "Building solution..." -ForegroundColor Yellow

    dotnet build $Global:Solution `
        --configuration Release `
        --no-restore

    if($LASTEXITCODE -eq 0)
    {
        Success "Solution Build Successful"
    }
    else
    {
        Failure "Solution Build Failed"
    }

    Write-Host ""

    Write-Host "Running Tests..." -ForegroundColor Yellow

    dotnet test $Global:Solution `
        --configuration Release `
        --no-build

    if($LASTEXITCODE -eq 0)
    {
        Success "All Tests Passed"
    }
    else
    {
        Failure "Tests Failed"
    }
}
finally
{
    Pop-Location
}
