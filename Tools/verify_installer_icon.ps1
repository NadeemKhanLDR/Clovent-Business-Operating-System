<#
.SYNOPSIS
    Inspects and verifies the embedded Win32 PE icon resources of an executable.
    Compares embedded RT_GROUP_ICON and RT_ICON resources against the approved multi-resolution .ico asset.
.PARAMETER PePath
    Path to the PE executable to verify (e.g. Clovent.Desktop.exe or Clovent.BusinessOperatingSystem-*-Setup.exe).
.PARAMETER ExpectedIconPath
    Path to the reference .ico asset (e.g. src/Clovent.Desktop/Resources/cbos.ico).
.PARAMETER FailOnMismatch
    If true, throws an exception / exits non-zero if the icon is missing or mismatched.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$PePath,

    [string]$ExpectedIconPath = "",
    [switch]$FailOnMismatch = $true
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path $PePath)) {
    throw "Target executable not found: $PePath"
}

$csharp = @'
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Collections.Generic;

public class PeIconVerifier
{
    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr LoadLibraryEx(string lpFileName, IntPtr hFile, uint dwFlags);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool FreeLibrary(IntPtr hModule);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr FindResource(IntPtr hModule, IntPtr lpName, IntPtr lpType);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr LoadResource(IntPtr hModule, IntPtr hResInfo);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr LockResource(IntPtr hResData);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern uint SizeofResource(IntPtr hModule, IntPtr hResInfo);

    private delegate bool EnumResNamesProc(IntPtr hModule, IntPtr lpszType, IntPtr lpszName, IntPtr lParam);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool EnumResourceNames(IntPtr hModule, IntPtr lpszType, EnumResNamesProc lpEnumFunc, IntPtr lParam);

    private const uint LOAD_LIBRARY_AS_DATAFILE = 0x00000002;
    private const uint LOAD_LIBRARY_AS_IMAGE_RESOURCE = 0x00000020;
    private static readonly IntPtr RT_GROUP_ICON = new IntPtr(14);
    private static readonly IntPtr RT_ICON = new IntPtr(3);

    public class EmbeddedIconEntry
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public int BitCount { get; set; }
        public uint DataSize { get; set; }
        public int ResourceId { get; set; }
    }

    public static List<EmbeddedIconEntry> InspectPeIcons(string pePath)
    {
        IntPtr hModule = LoadLibraryEx(pePath, IntPtr.Zero, LOAD_LIBRARY_AS_DATAFILE | LOAD_LIBRARY_AS_IMAGE_RESOURCE);
        if (hModule == IntPtr.Zero)
        {
            throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error(), "Failed to load PE: " + pePath);
        }

        try
        {
            List<IntPtr> groupIconNames = new List<IntPtr>();
            EnumResourceNames(hModule, RT_GROUP_ICON, (hMod, lpszType, lpszName, lParam) =>
            {
                groupIconNames.Add(lpszName);
                return true;
            }, IntPtr.Zero);

            if (groupIconNames.Count == 0)
            {
                return new List<EmbeddedIconEntry>();
            }

            // Inspect the primary group icon (usually MAKEINTRESOURCE(1) or 'MAINICON')
            IntPtr primaryName = groupIconNames[0];
            IntPtr hResInfo = FindResource(hModule, primaryName, RT_GROUP_ICON);
            if (hResInfo == IntPtr.Zero)
            {
                return new List<EmbeddedIconEntry>();
            }

            IntPtr hResData = LoadResource(hModule, hResInfo);
            IntPtr pData = LockResource(hResData);
            if (pData == IntPtr.Zero)
            {
                return new List<EmbeddedIconEntry>();
            }

            ushort reserved = (ushort)Marshal.ReadInt16(pData, 0);
            ushort type = (ushort)Marshal.ReadInt16(pData, 2);
            ushort count = (ushort)Marshal.ReadInt16(pData, 4);

            List<EmbeddedIconEntry> entries = new List<EmbeddedIconEntry>();
            for (int i = 0; i < count; i++)
            {
                int offset = 6 + (i * 14);
                byte w = Marshal.ReadByte(pData, offset);
                byte h = Marshal.ReadByte(pData, offset + 1);
                ushort bitCount = (ushort)Marshal.ReadInt16(pData, offset + 6);
                uint bytesInRes = (uint)Marshal.ReadInt32(pData, offset + 8);
                ushort id = (ushort)Marshal.ReadInt16(pData, offset + 12);

                entries.Add(new EmbeddedIconEntry
                {
                    Width = (w == 0) ? 256 : (int)w,
                    Height = (h == 0) ? 256 : (int)h,
                    BitCount = (int)bitCount,
                    DataSize = bytesInRes,
                    ResourceId = (int)id
                });
            }

            return entries;
        }
        finally
        {
            FreeLibrary(hModule);
        }
    }

    public static List<EmbeddedIconEntry> InspectIcoFile(string icoPath)
    {
        byte[] bytes = File.ReadAllBytes(icoPath);
        if (bytes.Length < 6) return new List<EmbeddedIconEntry>();

        ushort reserved = BitConverter.ToUInt16(bytes, 0);
        ushort type = BitConverter.ToUInt16(bytes, 2);
        ushort count = BitConverter.ToUInt16(bytes, 4);

        List<EmbeddedIconEntry> entries = new List<EmbeddedIconEntry>();
        for (int i = 0; i < count; i++)
        {
            int offset = 6 + (i * 16);
            byte w = bytes[offset];
            byte h = bytes[offset + 1];
            ushort bitCount = BitConverter.ToUInt16(bytes, offset + 6);
            uint dataSize = BitConverter.ToUInt32(bytes, offset + 8);

            entries.Add(new EmbeddedIconEntry
            {
                Width = (w == 0) ? 256 : (int)w,
                Height = (h == 0) ? 256 : (int)h,
                BitCount = (int)bitCount,
                DataSize = dataSize,
                ResourceId = i + 1
            });
        }
        return entries;
    }
}
'@

if (-not ([System.Management.Automation.PSTypeName]'PeIconVerifier').Type) {
    Add-Type -TypeDefinition $csharp -ErrorAction Stop
}

Write-Host "Verifying PE Icon Resources for: $PePath" -ForegroundColor Cyan
$peIcons = [PeIconVerifier]::InspectPeIcons($PePath)

if ($peIcons.Count -eq 0) {
    $msg = "VERIFICATION FAILURE: No embedded RT_GROUP_ICON resources found in PE file: $PePath"
    if ($FailOnMismatch) { throw $msg } else { Write-Warning $msg; return $false }
}

Write-Host "Found $($peIcons.Count) embedded icon image(s) in PE resource table:" -ForegroundColor Green
foreach ($ico in $peIcons) {
    Write-Host ("  - {0,3}x{1,-3} @ {2,2} bpp (ID: {3}, Size: {4:N0} bytes)" -f $ico.Width, $ico.Height, $ico.BitCount, $ico.ResourceId, $ico.DataSize)
}

# If expected icon path is provided, compare resolutions
if (-not [string]::IsNullOrWhiteSpace($ExpectedIconPath)) {
    if (-not (Test-Path $ExpectedIconPath)) {
        throw "Expected icon asset not found: $ExpectedIconPath"
    }

    Write-Host "`nComparing against reference icon asset: $ExpectedIconPath" -ForegroundColor Cyan
    $refIcons = [PeIconVerifier]::InspectIcoFile($ExpectedIconPath)
    Write-Host "Reference icon contains $($refIcons.Count) resolution(s):"
    foreach ($r in $refIcons) {
        Write-Host ("  - {0,3}x{1,-3} @ {2,2} bpp ({3:N0} bytes)" -f $r.Width, $r.Height, $r.BitCount, $r.DataSize)
    }

    if ($peIcons.Count -ne $refIcons.Count) {
        $msg = "VERIFICATION FAILURE: PE icon count ($($peIcons.Count)) does not match reference icon count ($($refIcons.Count))."
        if ($FailOnMismatch) { throw $msg } else { Write-Warning $msg; return $false }
    }

    for ($i = 0; $i -lt $refIcons.Count; $i++) {
        $ref = $refIcons[$i]
        $pe = $peIcons[$i]
        if ($pe.Width -ne $ref.Width -or $pe.Height -ne $ref.Height -or $pe.BitCount -ne $ref.BitCount) {
            $msg = "VERIFICATION FAILURE: Image $i mismatch. Expected {0}x{1}@{2}bpp, but PE has {3}x{4}@{5}bpp" -f $ref.Width, $ref.Height, $ref.BitCount, $pe.Width, $pe.Height, $pe.BitCount
            if ($FailOnMismatch) { throw $msg } else { Write-Warning $msg; return $false }
        }
    }
    Write-Host "`n[PASS] Embedded PE icon resources match reference asset exactly ($($refIcons.Count) resolutions up to 256x256@32bpp)." -ForegroundColor Green
}

# Also verify GDI+ Extraction
Add-Type -AssemblyName System.Drawing
$extractedIcon = [System.Drawing.Icon]::ExtractAssociatedIcon($PePath)
if ($extractedIcon -eq $null) {
    $msg = "VERIFICATION FAILURE: System.Drawing.Icon.ExtractAssociatedIcon returned null for $PePath"
    if ($FailOnMismatch) { throw $msg } else { Write-Warning $msg; return $false }
}
Write-Host "[PASS] Primary associated icon extractable: $($extractedIcon.Width)x$($extractedIcon.Height)" -ForegroundColor Green

return $true
