Add-Type -TypeDefinition '
using System;
using System.Text;
using System.Runtime.InteropServices;
namespace Qa3 {
  public static class W {
    [DllImport("user32.dll")] public static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
    [DllImport("user32.dll")] public static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
    [DllImport("user32.dll")] public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);
    [DllImport("user32.dll")] public static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);
    [DllImport("user32.dll")] public static extern bool EnumThreadWindows(int dwThreadId, EnumThreadDelegate lpfn, IntPtr lParam);
    public delegate bool EnumThreadDelegate(IntPtr hWnd, IntPtr lParam);
  }
}'
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes

$exe = "d:\Clovent Business Operating System\src\Clovent.Desktop\bin\Debug\net10.0-windows\Clovent.Desktop.exe"
$p = Start-Process $exe -ArgumentList "--pos" -PassThru
Start-Sleep -Seconds 6

$root = [System.Windows.Automation.AutomationElement]::RootElement
$procCond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ProcessIdProperty, $p.Id)
$posWin = $null
for ($i = 0; $i -lt 30; $i++) {
    $wins = $root.FindAll([System.Windows.Automation.TreeScope]::Children, $procCond)
    foreach ($w in $wins) {
        if ($w.Current.Name -eq "Restaurant POS") {
            $posWin = $w
            break
        }
    }
    if ($posWin) { break }
    Start-Sleep -Seconds 1
}

$all = $posWin.FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.Condition]::TrueCondition)
$recallHwnd = [IntPtr]::Zero
foreach ($el in $all) {
    if ($el.Current.Name -eq "Recall") {
        $recallHwnd = [IntPtr]$el.Current.NativeWindowHandle
        Write-Output "Recall button HWND: $recallHwnd"
        break
    }
}

if ($recallHwnd -ne [IntPtr]::Zero) {
    Write-Output "Sending BM_CLICK (0x00F5) to Recall button..."
    [Qa3.W]::PostMessage($recallHwnd, 0x00F5, [IntPtr]::Zero, [IntPtr]::Zero) | Out-Null
    Start-Sleep -Seconds 2
    
    # Also send WM_LBUTTONDOWN and WM_LBUTTONUP
    [Qa3.W]::PostMessage($recallHwnd, 0x0201, [IntPtr]1, [IntPtr]::Zero) | Out-Null
    [Qa3.W]::PostMessage($recallHwnd, 0x0202, [IntPtr]0, [IntPtr]::Zero) | Out-Null
    Start-Sleep -Seconds 2
    
    # Check all windows of process
    $winsAfter = $root.FindAll([System.Windows.Automation.TreeScope]::Descendants, $procCond)
    Write-Output "Total windows/elements in process after click: $($winsAfter.Count)"
    foreach ($w in $winsAfter) {
        if ($w.Current.Name -like "*Recall*" -or $w.Current.Name -like "*Sales History*") {
            Write-Output "FOUND RECALL WINDOW: '$($w.Current.Name)' HWND: $($w.Current.NativeWindowHandle)"
        }
    }
}

Get-Process Clovent.Desktop -ErrorAction SilentlyContinue | Stop-Process -Force
