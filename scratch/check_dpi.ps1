Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

[System.Windows.Forms.Screen]::AllScreens | ForEach-Object {
    Write-Host "Device: $($_.DeviceName), Primary: $($_.Primary), Bounds: $($_.Bounds.Width)x$($_.Bounds.Height), WorkingArea: $($_.WorkingArea.Width)x$($_.WorkingArea.Height), BitsPerPixel: $($_.BitsPerPixel)"
}

$source = @"
using System;
using System.Runtime.InteropServices;

public class DpiHelper
{
    [DllImport("gdi32.dll")]
    static extern int GetDeviceCaps(IntPtr hdc, int nIndex);

    [DllImport("user32.dll")]
    static extern IntPtr GetDC(IntPtr hWnd);

    [DllImport("user32.dll")]
    static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

    public static void PrintDpi()
    {
        IntPtr hdc = GetDC(IntPtr.Zero);
        int logpixelsx = GetDeviceCaps(hdc, 88);
        int logpixelsy = GetDeviceCaps(hdc, 90);
        int horzres = GetDeviceCaps(hdc, 8);
        int vertres = GetDeviceCaps(hdc, 10);
        int desthorzres = GetDeviceCaps(hdc, 118);
        int destvertres = GetDeviceCaps(hdc, 117);
        ReleaseDC(IntPtr.Zero, hdc);

        Console.WriteLine(string.Format("GDI LOGPIXELS: {0}x{1}", logpixelsx, logpixelsy));
        Console.WriteLine(string.Format("GDI HORZRES x VERTRES: {0}x{1}", horzres, vertres));
        Console.WriteLine(string.Format("GDI DESKTOPHORZRES x DESKTOPVERTRES: {0}x{1}", desthorzres, destvertres));
        double scale = (double)desthorzres / (double)horzres;
        Console.WriteLine(string.Format("Scale factor: {0}%", scale * 100));
    }
}
"@

Add-Type -TypeDefinition $source
[DpiHelper]::PrintDpi()
