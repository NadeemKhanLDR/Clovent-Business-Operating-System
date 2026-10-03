param(
    [string]$Command = "status",
    [string]$Arg1 = "",
    [string]$Arg2 = "",
    [string]$Arg3 = ""
)

Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

if (-not ([System.Management.Automation.PSTypeName]'WinApi.Native').Type) {
    Add-Type -MemberDefinition @'
[DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
[DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
[DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
[DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h, int cmd);
[DllImport("user32.dll")] public static extern void mouse_event(uint f, uint dx, uint dy, uint data, int ex);
[DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
[DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr hwnd, IntPtr hdcBlt, uint nFlags);
public struct RECT { public int L, T, R, B; }
'@ -Name Native -Namespace WinApi
}

[WinApi.Native]::SetProcessDPIAware() | Out-Null

function Get-CloventWindow {
    param([string]$TitlePattern = "*")
    $root = [System.Windows.Automation.AutomationElement]::RootElement
    $allWins = $root.FindAll([System.Windows.Automation.TreeScope]::Children, [System.Windows.Automation.Condition]::TrueCondition)
    foreach ($w in $allWins) {
        $pname = ""
        try {
            $p = Get-Process -Id $w.Current.ProcessId -ErrorAction SilentlyContinue
            $pname = $p.ProcessName
        } catch {}
        if ($pname -eq "Clovent.Desktop") {
            if ($TitlePattern -eq "*" -or $w.Current.Name -like $TitlePattern) {
                return $w
            }
        }
    }
    return $null
}

function Capture-Window {
    param([System.Windows.Automation.AutomationElement]$Win, [string]$OutPath)
    if (-not $Win) { Write-Error "No window provided to Capture-Window"; return $false }
    $hwnd = [IntPtr]$Win.Current.NativeWindowHandle
    [WinApi.Native]::SetForegroundWindow($hwnd) | Out-Null
    Start-Sleep -Milliseconds 400
    
    $r = New-Object WinApi.Native+RECT
    [WinApi.Native]::GetWindowRect($hwnd, [ref]$r) | Out-Null
    $w = $r.R - $r.L
    $h = $r.B - $r.T
    
    if ($w -le 0 -or $h -le 0) {
        $rect = $Win.Current.BoundingRectangle
        $w = [int]$rect.Width
        $h = [int]$rect.Height
        $r.L = [int]$rect.X
        $r.T = [int]$rect.Y
    }
    
    $bmp = New-Object System.Drawing.Bitmap($w, $h)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    try {
        $g.CopyFromScreen($r.L, $r.T, 0, 0, (New-Object System.Drawing.Size($w, $h)))
    } catch {
        # Fallback to PrintWindow
        $hdc = $g.GetHdc()
        [WinApi.Native]::PrintWindow($hwnd, $hdc, 2) | Out-Null
        $g.ReleaseHdc($hdc)
    }
    $g.Dispose()
    
    $dir = [System.IO.Path]::GetDirectoryName($OutPath)
    if (-not (Test-Path $dir)) { New-Item -ItemType Directory -Path $dir -Force | Out-Null }
    $bmp.Save($OutPath, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
    Write-Output "CAPTURED: $OutPath ($w x $h)"
    return $true
}

function Dump-WindowElements {
    param([System.Windows.Automation.AutomationElement]$Win)
    $all = $Win.FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.Condition]::TrueCondition)
    Write-Output "=== Elements for window '$($Win.Current.Name)' (Total: $($all.Count)) ==="
    foreach ($e in $all) {
        $n = $e.Current.Name
        $c = $e.Current.ClassName
        $t = $e.Current.ControlType.ProgrammaticName.Replace("ControlType.", "")
        $r = $e.Current.BoundingRectangle
        $aid = $e.Current.AutomationId
        if ($n -or $aid -or $t -in @("Button", "Edit", "ComboBox", "CheckBox", "List", "DataGrid", "Table", "ProgressBar")) {
            Write-Output "[$t] Name='$n' Id='$aid' Class='$c' Rect=($([int]$r.X),$([int]$r.Y),$([int]$r.Width),$([int]$r.Height))"
        }
    }
}

function Click-ElementByName {
    param([System.Windows.Automation.AutomationElement]$Win, [string]$Name, [string]$ControlType = $null)
    $conds = @(New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::NameProperty, $Name))
    if ($ControlType) {
        $typeField = [System.Windows.Automation.ControlType].GetField($ControlType).GetValue($null)
        $conds += New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ControlTypeProperty, $typeField)
    }
    $andCond = if ($conds.Count -gt 1) { New-Object System.Windows.Automation.AndCondition($conds) } else { $conds[0] }
    $el = $Win.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $andCond)
    if (-not $el) {
        Write-Error "Element not found by name: '$Name' (Type: $ControlType)"
        return $false
    }
    
    $inv = $null
    if ($el.TryGetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern, [ref]$inv)) {
        $inv.Invoke()
        Write-Output "INVOKED '$Name'"
        Start-Sleep -Milliseconds 600
        return $true
    }
    
    # Fallback to mouse click
    $r = $el.Current.BoundingRectangle
    $cx = [int]($r.X + $r.Width / 2)
    $cy = [int]($r.Y + $r.Height / 2)
    [WinApi.Native]::SetCursorPos($cx, $cy) | Out-Null
    Start-Sleep -Milliseconds 100
    [WinApi.Native]::mouse_event(2, 0, 0, 0, 0)
    Start-Sleep -Milliseconds 50
    [WinApi.Native]::mouse_event(4, 0, 0, 0, 0)
    Write-Output "CLICKED '$Name' at ($cx, $cy)"
    Start-Sleep -Milliseconds 600
    return $true
}

function Set-TextByName {
    param([System.Windows.Automation.AutomationElement]$Win, [string]$Name, [string]$Text)
    $cond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::NameProperty, $Name)
    $el = $Win.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $cond)
    if (-not $el) {
        Write-Error "Edit element not found by name: '$Name'"
        return $false
    }
    
    $valPat = $null
    if ($el.TryGetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern, [ref]$valPat)) {
        $valPat.SetValue($Text)
        Write-Output "SET-VALUE '$Name' = '$Text'"
        return $true
    }
    
    # Fallback: click and SendKeys
    $r = $el.Current.BoundingRectangle
    $cx = [int]($r.X + $r.Width / 2)
    $cy = [int]($r.Y + $r.Height / 2)
    [WinApi.Native]::SetCursorPos($cx, $cy) | Out-Null
    Start-Sleep -Milliseconds 100
    [WinApi.Native]::mouse_event(2, 0, 0, 0, 0)
    Start-Sleep -Milliseconds 50
    [WinApi.Native]::mouse_event(4, 0, 0, 0, 0)
    Start-Sleep -Milliseconds 200
    [System.Windows.Forms.SendKeys]::SendWait("^a{BACKSPACE}")
    [System.Windows.Forms.SendKeys]::SendWait($Text)
    Write-Output "TYPED '$Name' = '$Text'"
    return $true
}

# Command dispatch
switch ($Command.ToLowerInvariant()) {
    "status" {
        $w = Get-CloventWindow
        if ($w) {
            Write-Output "ACTIVE_WINDOW: '$($w.Current.Name)' | PID=$($w.Current.ProcessId) | HWND=$($w.Current.NativeWindowHandle)"
        } else {
            Write-Output "NO_ACTIVE_WINDOW"
        }
    }
    "cap" {
        $w = Get-CloventWindow
        if ($w) {
            Capture-Window $w $Arg1
        } else {
            Write-Error "No active Clovent window found."
        }
    }
    "dump" {
        $w = Get-CloventWindow
        if ($w) {
            Dump-WindowElements $w
        } else {
            Write-Error "No active Clovent window found."
        }
    }
    "click" {
        $w = Get-CloventWindow
        if ($w) {
            Click-ElementByName $w $Arg1 $Arg2
        } else {
            Write-Error "No active Clovent window found."
        }
    }
    "settext" {
        $w = Get-CloventWindow
        if ($w) {
            Set-TextByName $w $Arg1 $Arg2
        } else {
            Write-Error "No active Clovent window found."
        }
    }
    default {
        Write-Output "Unknown command: $Command"
    }
}
