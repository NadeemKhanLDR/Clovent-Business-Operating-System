. "d:\Clovent Business Operating System\qa\v3_common.ps1"

Guard-Modals
[QaV3.E]::FindMain($procId)
Log ("order: '" + (Get-OrderPaneText) + "'")
[QaV3.E]::SetForegroundWindow([QaV3.E]::Main) | Out-Null
Start-Sleep -Milliseconds 700

Add-Type -MemberDefinition '[DllImport("user32.dll")] public static extern bool MoveWindow(IntPtr h, int x, int y, int w, int ht, bool repaint);' -Name MV -Namespace QaV3

# restore from maximized first
[QaV3.E]::ShowWindow([QaV3.E]::Main, 1) | Out-Null
Start-Sleep -Milliseconds 800

# 1024x768 logical = 2560x1920
[QaV3.MV]::MoveWindow([QaV3.E]::Main, 640, 240, 2560, 1920, $true) | Out-Null
Start-Sleep -Seconds 2
Capture-Pos "v3_final_1024x768_active.png" "1024x768 with active order"

# 1366x768 logical = 3415x1920
[QaV3.MV]::MoveWindow([QaV3.E]::Main, 212, 240, 3415, 1920, $true) | Out-Null
Start-Sleep -Seconds 2
Capture-Pos "v3_final_1366x768_active.png" "1366x768 with active order"

# 1920x1080 logical = 4800x2700 (will clamp ~ screen+borders)
[QaV3.MV]::MoveWindow([QaV3.E]::Main, -480, -150, 4800, 2700, $true) | Out-Null
Start-Sleep -Seconds 2
Capture-Pos "v3_final_1920x1080_active.png" "1920x1080 attempt with active order"

# back to maximized
[QaV3.E]::ShowWindow([QaV3.E]::Main, 3) | Out-Null
Start-Sleep -Seconds 2
Capture-Pos "v3_final_4k250_maximized.png" "final maximized 250%"
Log "=== PHASE 12 COMPLETE ==="
