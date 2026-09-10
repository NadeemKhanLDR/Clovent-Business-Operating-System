Param([int]$W = 0, [int]$H = 0)
Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;
[StructLayout(LayoutKind.Sequential, CharSet=CharSet.Ansi)]
public class DEVMODE {
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst=32)] public string dmDeviceName = "";
    public short dmSpecVersion; public short dmDriverVersion; public short dmSize = (short)220; public short dmDriverExtra;
    public int dmFields; public int dmPositionX; public int dmPositionY; public int dmDisplayOrientation; public int dmDisplayFixedOutput;
    public short dmColor; public short dmDuplex; public short dmYResolution; public short dmTTOption; public short dmCollate;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst=32)] public string dmFormName = "";
    public short dmLogPixels; public int dmBitsPerPel; public int dmPelsWidth; public int dmPelsHeight; public int dmDisplayFlags; public int dmDisplayFrequency;
    public int dmICMMethod; public int dmICMIntent; public int dmMediaType; public int dmICCManufacturer; public int dmICCModel; public int dmPanningWidth; public int dmPanningHeight;
}
public static class Res {
    [DllImport("user32.dll")] public static extern int EnumDisplaySettings(string device, int mode, DEVMODE dm);
    [DllImport("user32.dll")] public static extern int ChangeDisplaySettings(DEVMODE dm, int flags);
}
'@
$dm = New-Object DEVMODE
[Res]::EnumDisplaySettings($null, -1, $dm) | Out-Null
if ($W -le 0 -or $H -le 0) {
    # Restore registry settings (original resolution)
    $r = [Res]::ChangeDisplaySettings($null, 0)
    Write-Output "RESTORE result=$r"
} else {
    Write-Output ("CURRENT=" + $dm.dmPelsWidth + "x" + $dm.dmPelsHeight)
    $dm.dmPelsWidth = $W
    $dm.dmPelsHeight = $H
    $dm.dmFields = 0x80000 -bor 0x100000  # DM_PELSWIDTH | DM_PELSHEIGHT
    $r = [Res]::ChangeDisplaySettings($dm, 0)
    Write-Output "SET ${W}x${H} result=$r"
}
