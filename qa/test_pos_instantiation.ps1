$ErrorActionPreference = "Stop"

$pubDir = "D:\Clovent Business Operating System\artifacts\release\Clovent.BusinessOperatingSystem-1.0.7-win-x64"

# Set location to pubDir so all dependencies load
Set-Location $pubDir

# Load core desktop assembly
$asm = [System.Reflection.Assembly]::LoadFrom("$pubDir\Clovent.Desktop.dll")

# Find Program or ApplicationBootstrapper
$bootstrapperType = [System.Type]::GetType("Clovent.Platform.Bootstrap.ApplicationBootstrapper, Clovent.Platform")
$createMethod = $bootstrapperType.GetMethod("Create", [System.Reflection.BindingFlags]"Public,Static", $null, @([string[]], [string], [string]), $null)
$bootstrapper = $createMethod.Invoke($null, @($null, $pubDir, "Production"))

# Add modules/services as Program.cs does
$programType = $asm.GetType("Clovent.Desktop.Program")

Write-Host "Creating Host and ServiceProvider..."
# Let's inspect Program.Main setup or invoke services directly
$hostMethod = $bootstrapper.GetType().GetMethod("BuildAndInitializeAsync")
$hostTask = $hostMethod.Invoke($bootstrapper, $null)
$host = $hostTask.GetAwaiter().GetResult()

Write-Host "Resolving RestaurantPosForm from Services..."
try {
    $posForm = [Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions]::GetRequiredService($host.Services, [System.Type]::GetType("Clovent.Desktop.Restaurant.Orders.RestaurantPosForm, Clovent.Desktop"))
    Write-Host "PosForm resolved successfully: $posForm"

    Write-Host "Calling Show() or CreateControl()..."
    $posForm.CreateControl()
    Write-Host "CreateControl succeeded!"
} catch {
    Write-Host "CAUGHT EXCEPTION:"
    $ex = $_.Exception
    while ($ex) {
        Write-Host "--------------------------------"
        Write-Host "$($ex.GetType().FullName): $($ex.Message)"
        Write-Host $ex.StackTrace
        $ex = $ex.InnerException
    }
}
