Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing
Add-Type -MemberDefinition '[DllImport("user32.dll")] public static extern bool SetProcessDPIAware(); [DllImport("user32.dll")] public static extern void mouse_event(uint f,uint dx,uint dy,uint data,int ex); [DllImport("user32.dll")] public static extern bool SetCursorPos(int x,int y); [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h); [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h, int cmd); [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r); public struct RECT { public int L, T, R, B; }' -Name U -Namespace WinApi

[WinApi.U]::SetProcessDPIAware() | Out-Null

function Click-Element {
    param([System.Windows.Automation.AutomationElement]$el)
    $inv = $null
    if ($el.TryGetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern, [ref]$inv)) {
        $inv.Invoke()
        Start-Sleep -Milliseconds 500
        return $true
    }
    $r = $el.Current.BoundingRectangle
    if (-not $r.IsEmpty -and $r.Width -gt 0 -and $r.Height -gt 0) {
        $cx = [int]($r.X + $r.Width / 2)
        $cy = [int]($r.Y + $r.Height / 2)
        [WinApi.U]::SetCursorPos($cx, $cy) | Out-Null
        Start-Sleep -Milliseconds 150
        [WinApi.U]::mouse_event(2, 0, 0, 0, 0)
        Start-Sleep -Milliseconds 60
        [WinApi.U]::mouse_event(4, 0, 0, 0, 0)
        Start-Sleep -Milliseconds 500
        return $true
    }
    return $false
}

function Capture-Window {
    param([IntPtr]$hwnd, [string]$outPath)
    $r = New-Object WinApi.RECT
    [WinApi.U]::GetWindowRect($hwnd, [ref]$r) | Out-Null
    $w = $r.R - $r.L
    $h = $r.B - $r.T
    if ($w -le 0 -or $h -le 0) {
        $b = [System.Windows.Forms.Screen]::PrimaryScreen.Bounds
        $w = $b.Width; $h = $b.Height; $r.L = 0; $r.T = 0
    }
    $bmp = New-Object System.Drawing.Bitmap($w, $h)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.CopyFromScreen($r.L, $r.T, 0, 0, (New-Object System.Drawing.Size($w, $h)))
    $g.Dispose()
    $bmp.Save($outPath, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
    Write-Output "CAPTURED $outPath ($w x $h)"
}

function Find-ElementByName {
    param([System.Windows.Automation.AutomationElement]$parent, [string]$name, [string]$controlType = $null)
    $conds = @(New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::NameProperty, $name))
    if ($controlType) {
        $typeField = [System.Windows.Automation.ControlType].GetField($controlType).GetValue($null)
        $conds += New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ControlTypeProperty, $typeField)
    }
    $andCond = if ($conds.Count -gt 1) { New-Object System.Windows.Automation.AndCondition($conds) } else { $conds[0] }
    return $parent.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $andCond)
}

# 1. Stop existing process if running
Get-Process Clovent.Desktop -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Seconds 1

# 2. Launch process
Write-Output "Starting Clovent.Desktop.exe..."
$exe = "d:\Clovent Business Operating System\src\Clovent.Desktop\bin\Debug\net10.0-windows\Clovent.Desktop.exe"
$p = Start-Process $exe -PassThru
$procId = $p.Id
Write-Output "Started with PID: $procId"

# 3. Wait for window
$root = [System.Windows.Automation.AutomationElement]::RootElement
$procCond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ProcessIdProperty, $procId)

$mainWin = $null
for ($i = 0; $i -lt 30; $i++) {
    Start-Sleep -Seconds 1
    $wins = $root.FindAll([System.Windows.Automation.TreeScope]::Children, $procCond)
    if ($wins.Count -gt 0) {
        $mainWin = $wins[0]
        Write-Output "Found window: '$($mainWin.Current.Name)'"
        break
    }
}

if (-not $mainWin) {
    Write-Output "Failed to find any window for Clovent.Desktop"
    exit 1
}

# 4. Handle Login window if present
if ($mainWin.Current.Name -like "*Sign in*" -or $mainWin.Current.Name -like "*Login*") {
    Write-Output "Handling login..."
    [WinApi.U]::SetForegroundWindow([IntPtr]$mainWin.Current.NativeWindowHandle) | Out-Null
    Start-Sleep -Milliseconds 600

    # Find text edits for username & password
    $editType = [System.Windows.Automation.ControlType]::Edit
    $editCond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ControlTypeProperty, $editType)
    $edits = $mainWin.FindAll([System.Windows.Automation.TreeScope]::Descendants, $editCond)

    if ($edits.Count -ge 2) {
        Click-Element $edits[0] | Out-Null
        [System.Windows.Forms.SendKeys]::SendWait("^aadmin")
        Start-Sleep -Milliseconds 300
        Click-Element $edits[1] | Out-Null
        [System.Windows.Forms.SendKeys]::SendWait("^aAdmin123!")
        Start-Sleep -Milliseconds 300
        [System.Windows.Forms.SendKeys]::SendWait("{ENTER}")
    } else {
        # Fallback to direct clicking coordinates from previous QA
        $r = $mainWin.Current.BoundingRectangle
        $cx = [int]($r.X + $r.Width / 2)
        $cy = [int]($r.Y + $r.Height / 2)
        [WinApi.U]::SetCursorPos($cx, $cy - 60) | Out-Null
        [WinApi.U]::mouse_event(2, 0, 0, 0, 0); [WinApi.U]::mouse_event(4, 0, 0, 0, 0)
        [System.Windows.Forms.SendKeys]::SendWait("^aadmin")
        Start-Sleep -Milliseconds 300
        [WinApi.U]::SetCursorPos($cx, $cy) | Out-Null
        [WinApi.U]::mouse_event(2, 0, 0, 0, 0); [WinApi.U]::mouse_event(4, 0, 0, 0, 0)
        [System.Windows.Forms.SendKeys]::SendWait("^aAdmin123!")
        Start-Sleep -Milliseconds 300
        [System.Windows.Forms.SendKeys]::SendWait("{ENTER}")
    }
    Write-Output "Login submitted. Waiting for main form..."
    Start-Sleep -Seconds 5
}

# 5. Check for Restaurant POS or main dashboard
for ($i = 0; $i -lt 20; $i++) {
    $wins = $root.FindAll([System.Windows.Automation.TreeScope]::Children, $procCond)
    foreach ($w in $wins) {
        if ($w.Current.Name -like "*POS*" -or $w.Current.Name -like "*Restaurant*" -or $w.Current.Name -like "*Clovent*") {
            $mainWin = $w
            break
        }
    }
    if ($mainWin.Current.Name -like "*POS*") { break }
    Start-Sleep -Seconds 1
}

Write-Output "Active window: '$($mainWin.Current.Name)'"
[WinApi.U]::SetForegroundWindow([IntPtr]$mainWin.Current.NativeWindowHandle) | Out-Null
Start-Sleep -Milliseconds 800

# Capture POS initial state
Capture-Window ([IntPtr]$mainWin.Current.NativeWindowHandle) "qa\qa_01_startup_pos.png"

# 6. Find Recall button
Write-Output "Looking for Recall button..."
$btnCond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ControlTypeProperty, [System.Windows.Automation.ControlType]::Button)
$allBtns = $mainWin.FindAll([System.Windows.Automation.TreeScope]::Descendants, $btnCond)

$recallBtn = $null
$cancelBtn = $null
$clearBtn = $null

foreach ($b in $allBtns) {
    if ($b.Current.Name -eq "Recall") { $recallBtn = $b }
    elseif ($b.Current.Name -eq "Clear") { $clearBtn = $b }
    elseif ($b.Current.Name -like "*Cancel*") { $cancelBtn = $b }
}

Write-Output ("Buttons found: Recall=" + ($recallBtn -ne $null) + ", Clear=" + ($clearBtn -ne $null) + ", Cancel=" + ($cancelBtn -ne $null))

if ($recallBtn) {
    Write-Output "Clicking Recall button..."
    Click-Element $recallBtn | Out-Null
    Start-Sleep -Seconds 2

    # Find Recall / Sales History dialog
    $dialog = $null
    $wins = $root.FindAll([System.Windows.Automation.TreeScope]::Children, $procCond)
    foreach ($w in $wins) {
        if ($w.Current.Name -like "*Recall*" -or $w.Current.Name -like "*Sales History*") {
            $dialog = $w
            break
        }
    }

    if ($dialog) {
        Write-Output "Found dialog: '$($dialog.Current.Name)'"
        $dhwnd = [IntPtr]$dialog.Current.NativeWindowHandle
        [WinApi.U]::SetForegroundWindow($dhwnd) | Out-Null

        # Capture initial Recall view (Held tab)
        Capture-Window $dhwnd "qa\qa_02_recall_held.png"

        # Check tabs inside dialog
        $dialogBtns = $dialog.FindAll([System.Windows.Automation.TreeScope]::Descendants, $btnCond)
        $closedTab = $null
        $voidedTab = $null
        $heldTab = $null

        foreach ($b in $dialogBtns) {
            if ($b.Current.Name -eq "CLOSED") { $closedTab = $b }
            elseif ($b.Current.Name -eq "VOIDED") { $voidedTab = $b }
            elseif ($b.Current.Name -eq "HELD") { $heldTab = $b }
        }

        if ($closedTab) {
            Write-Output "Switching to CLOSED tab..."
            Click-Element $closedTab | Out-Null
            Start-Sleep -Seconds 1
            Capture-Window $dhwnd "qa\qa_03_recall_closed.png"
        }

        if ($voidedTab) {
            Write-Output "Switching to VOIDED tab..."
            Click-Element $voidedTab | Out-Null
            Start-Sleep -Seconds 1
            Capture-Window $dhwnd "qa\qa_04_recall_voided.png"
        }

        if ($heldTab) {
            Write-Output "Switching back to HELD tab..."
            Click-Element $heldTab | Out-Null
            Start-Sleep -Seconds 1
            Capture-Window $dhwnd "qa\qa_05_recall_held_again.png"
        }

        # Close dialog
        $closeBtn = $null
        foreach ($b in $dialog.FindAll([System.Windows.Automation.TreeScope]::Descendants, $btnCond)) {
            if ($b.Current.Name -eq "Close" -or $b.Current.Name -eq "✕") { $closeBtn = $b; break }
        }
        if ($closeBtn) {
            Click-Element $closeBtn | Out-Null
            Start-Sleep -Seconds 1
        } else {
            [System.Windows.Forms.SendKeys]::SendWait("{ESC}")
            Start-Sleep -Seconds 1
        }
    } else {
        Write-Output "Recall dialog did not open as a separate top-level window. Checking child windows..."
    }
}

Write-Output "QA script completed successfully."
