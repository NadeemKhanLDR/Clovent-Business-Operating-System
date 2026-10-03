$ErrorActionPreference = "Stop"

# Ensure clean commissioning markers
$pdataMarker = "C:\ProgramData\Clovent\BusinessOperatingSystem\commissioning.json"
$ldataMarker = "$env:LOCALAPPDATA\Clovent\Clovent.BusinessOperatingSystem\commissioning.json"
if (Test-Path $pdataMarker) { Remove-Item $pdataMarker -Force }
if (Test-Path $ldataMarker) { Remove-Item $ldataMarker -Force }

# Ensure clean database
$conn = New-Object System.Data.SqlClient.SqlConnection("Server=localhost;Database=master;Integrated Security=True;TrustServerCertificate=True")
$conn.Open()
$cmd = $conn.CreateCommand()
$cmd.CommandText = "IF EXISTS (SELECT 1 FROM sys.databases WHERE name = 'Clovent_BusinessOperatingSystem') BEGIN ALTER DATABASE [Clovent_BusinessOperatingSystem] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [Clovent_BusinessOperatingSystem]; END"
$cmd.ExecuteNonQuery()
$conn.Close()
Write-Host "Database clean state ensured."

# Ensure valid machine license in LocalAppData
$localLicDir = "$env:LOCALAPPDATA\Clovent\Clovent.BusinessOperatingSystem"
if (-not (Test-Path $localLicDir)) { New-Item -ItemType Directory -Path $localLicDir -Force }
Copy-Item "artifacts\acceptance_machine_bound.lic" -Destination "$localLicDir\clovent.lic" -Force
Write-Host "Machine-bound test license in place: $((Test-Path "$localLicDir\clovent.lic"))"

Write-Host "Starting C:\CloventClient104\Clovent.Desktop.exe..."
$p = Start-Process "C:\CloventClient104\Clovent.Desktop.exe" -PassThru
Write-Host "Started PID: $($p.Id)"

# 1. Step 1: Welcome
Write-Host "Waiting for First-Run wizard window..."
& "Tools\ScreenCaptureUtil\bin\Release\net10.0-windows\ScreenCaptureUtil.exe" wait-win "First-Run" 20
Start-Sleep -Seconds 1
& "Tools\ScreenCaptureUtil\bin\Release\net10.0-windows\ScreenCaptureUtil.exe" title "First-Run" "qa\acceptance_test_103\01_wizard_step1_welcome.png"
Write-Host "Step 1: Captured 01_wizard_step1_welcome.png"

# Click Next to Step 2
& "Tools\ScreenCaptureUtil\bin\Release\net10.0-windows\ScreenCaptureUtil.exe" click-btn "First-Run" "Next >"
Start-Sleep -Seconds 1

# 2. Step 2: Database Connection
Write-Host "Step 2: Testing connection..."
& "Tools\ScreenCaptureUtil\bin\Release\net10.0-windows\ScreenCaptureUtil.exe" click-btn "First-Run" "Test Connection"
Start-Sleep -Seconds 3
& "Tools\ScreenCaptureUtil\bin\Release\net10.0-windows\ScreenCaptureUtil.exe" title "First-Run" "qa\acceptance_test_103\02b_test_connection_windows_auth.png"
Write-Host "Step 2: Captured 02b_test_connection_windows_auth.png"

# Click Next to Step 3
& "Tools\ScreenCaptureUtil\bin\Release\net10.0-windows\ScreenCaptureUtil.exe" click-btn "First-Run" "Next >"
Start-Sleep -Seconds 2

# 3. Step 3: Schema Migrations
Write-Host "Step 3: Initial screen capture..."
& "Tools\ScreenCaptureUtil\bin\Release\net10.0-windows\ScreenCaptureUtil.exe" title "First-Run" "qa\acceptance_test_103\03_step3_schema_initialization.png"
Write-Host "Step 3: Captured 03_step3_schema_initialization.png"

Write-Host "Step 3: Applying migrations..."
& "Tools\ScreenCaptureUtil\bin\Release\net10.0-windows\ScreenCaptureUtil.exe" click-btn "First-Run" "Apply Database Migrations"

# Wait for migration completion
$sw = [System.Diagnostics.Stopwatch]::StartNew()
while ($sw.Elapsed.TotalSeconds -lt 90) {
    Start-Sleep -Seconds 3
    Write-Host "Waiting for migrations... ($([int]$sw.Elapsed.TotalSeconds)s)"
    $dumpOutput = & "Tools\ScreenCaptureUtil\bin\Release\net10.0-windows\ScreenCaptureUtil.exe" dump "First-Run" 2>&1 | Out-String
    if ($dumpOutput -match "migrated successfully" -or $dumpOutput -match "COMPLETED:") {
        Write-Host "Step 3: Migrations finished successfully!"
        break
    }
}
Start-Sleep -Seconds 2
& "Tools\ScreenCaptureUtil\bin\Release\net10.0-windows\ScreenCaptureUtil.exe" title "First-Run" "qa\acceptance_test_103\03b_schema_migrations_complete.png"
Write-Host "Step 3: Captured 03b_schema_migrations_complete.png"

# Click Next to Step 4
& "Tools\ScreenCaptureUtil\bin\Release\net10.0-windows\ScreenCaptureUtil.exe" click-btn "First-Run" "Next >"
Start-Sleep -Seconds 2

# 4. Step 4: Enterprise Hierarchy
Write-Host "Step 4: Configuring hierarchy..."
& "Tools\ScreenCaptureUtil\bin\Release\net10.0-windows\ScreenCaptureUtil.exe" set-text "First-Run" "txtOrgName" "Clovent Global Enterprises"
Start-Sleep -Milliseconds 400
& "Tools\ScreenCaptureUtil\bin\Release\net10.0-windows\ScreenCaptureUtil.exe" set-text "First-Run" "txtTaxId" "ORG-ACCEPTANCE-001"
Start-Sleep -Milliseconds 400
& "Tools\ScreenCaptureUtil\bin\Release\net10.0-windows\ScreenCaptureUtil.exe" set-text "First-Run" "txtCompanyName" "Clovent Flagship Dining Ltd"
Start-Sleep -Milliseconds 400
& "Tools\ScreenCaptureUtil\bin\Release\net10.0-windows\ScreenCaptureUtil.exe" set-text "First-Run" "txtBranchName" "Metropolitan Branch"
Start-Sleep -Milliseconds 400

& "Tools\ScreenCaptureUtil\bin\Release\net10.0-windows\ScreenCaptureUtil.exe" title "First-Run" "qa\acceptance_test_103\04_step4_enterprise_hierarchy.png"
Write-Host "Step 4: Captured 04_step4_enterprise_hierarchy.png"

# Click Next to Step 5
& "Tools\ScreenCaptureUtil\bin\Release\net10.0-windows\ScreenCaptureUtil.exe" click-btn "First-Run" "Next >"
Start-Sleep -Seconds 2

# 5. Step 5: Administrator Account
Write-Host "Step 5: Configuring administrator account..."
& "Tools\ScreenCaptureUtil\bin\Release\net10.0-windows\ScreenCaptureUtil.exe" set-text "First-Run" "txtAdminUsername" "admin_acceptance"
Start-Sleep -Milliseconds 400
& "Tools\ScreenCaptureUtil\bin\Release\net10.0-windows\ScreenCaptureUtil.exe" set-text "First-Run" "txtAdminFullName" "Acceptance Admin"
Start-Sleep -Milliseconds 400
& "Tools\ScreenCaptureUtil\bin\Release\net10.0-windows\ScreenCaptureUtil.exe" set-text "First-Run" "txtAdminEmail" "admin@clovent.local"
Start-Sleep -Milliseconds 400
& "Tools\ScreenCaptureUtil\bin\Release\net10.0-windows\ScreenCaptureUtil.exe" set-text "First-Run" "txtAdminPassword" "Password@2026!"
Start-Sleep -Milliseconds 400
& "Tools\ScreenCaptureUtil\bin\Release\net10.0-windows\ScreenCaptureUtil.exe" set-text "First-Run" "txtAdminConfirmPassword" "Password@2026!"
Start-Sleep -Milliseconds 600

& "Tools\ScreenCaptureUtil\bin\Release\net10.0-windows\ScreenCaptureUtil.exe" title "First-Run" "qa\acceptance_test_103\05_step5_admin_account.png"
Write-Host "Step 5: Captured 05_step5_admin_account.png"

# Click Next to Step 6
& "Tools\ScreenCaptureUtil\bin\Release\net10.0-windows\ScreenCaptureUtil.exe" click-btn "First-Run" "Next >"
Start-Sleep -Seconds 2

# 6. Step 6: Regional Settings
Write-Host "Step 6: Configuring regional settings..."
& "Tools\ScreenCaptureUtil\bin\Release\net10.0-windows\ScreenCaptureUtil.exe" set-text "First-Run" "txtTerminalName" "POS-TERM-01"
Start-Sleep -Milliseconds 400

& "Tools\ScreenCaptureUtil\bin\Release\net10.0-windows\ScreenCaptureUtil.exe" title "First-Run" "qa\acceptance_test_103\06_step6_regional_settings.png"
Write-Host "Step 6: Captured 06_step6_regional_settings.png"

# Click Next to Step 7
& "Tools\ScreenCaptureUtil\bin\Release\net10.0-windows\ScreenCaptureUtil.exe" click-btn "First-Run" "Next >"
Start-Sleep -Seconds 2

# 7. Step 7: Licensing
Write-Host "Step 7: Verifying licensing status..."
& "Tools\ScreenCaptureUtil\bin\Release\net10.0-windows\ScreenCaptureUtil.exe" title "First-Run" "qa\acceptance_test_103\07_step7_licensing.png"
Write-Host "Step 7: Captured 07_step7_licensing.png"

# Click Next to Step 8
& "Tools\ScreenCaptureUtil\bin\Release\net10.0-windows\ScreenCaptureUtil.exe" click-btn "First-Run" "Next >"
Start-Sleep -Seconds 2

# 8. Step 8: Review & Complete
Write-Host "Step 8: Review configuration..."
& "Tools\ScreenCaptureUtil\bin\Release\net10.0-windows\ScreenCaptureUtil.exe" title "First-Run" "qa\acceptance_test_103\08_step8_review_complete.png"
Write-Host "Step 8: Captured 08_step8_review_complete.png"

# Click Finish Setup
Write-Host "Clicking Finish Setup..."
& "Tools\ScreenCaptureUtil\bin\Release\net10.0-windows\ScreenCaptureUtil.exe" click-btn "First-Run" "Finish Setup"
Start-Sleep -Seconds 4

# Check for setup completed message box
Write-Host "Checking for Setup Completed message box..."
$wins = & "Tools\ScreenCaptureUtil\bin\Release\net10.0-windows\ScreenCaptureUtil.exe" list-wins 2>&1 | Out-String
if ($wins -match "Setup Completed") {
    Write-Host "Dismissing Setup Completed message box..."
    & "Tools\ScreenCaptureUtil\bin\Release\net10.0-windows\ScreenCaptureUtil.exe" click-btn "Setup Completed" "OK"
    Start-Sleep -Seconds 3
}

# Wait for LoginForm to appear
Write-Host "Waiting for LoginForm..."
& "Tools\ScreenCaptureUtil\bin\Release\net10.0-windows\ScreenCaptureUtil.exe" wait-win "Sign in" 25
Start-Sleep -Seconds 2
& "Tools\ScreenCaptureUtil\bin\Release\net10.0-windows\ScreenCaptureUtil.exe" title "Sign in" "qa\acceptance_test_103\09_login_form.png"
Write-Host "Captured 09_login_form.png"

Write-Host "Commissioning and transition to Sign In completed successfully! Process PID: $($p.Id)"
