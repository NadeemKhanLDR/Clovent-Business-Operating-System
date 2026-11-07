param([Parameter(Mandatory)][string]$AttemptFolder)
$ErrorActionPreference='Stop'
if (Test-Path -LiteralPath $AttemptFolder) { throw 'Attempt folder must not exist.' }
New-Item -ItemType Directory -Path $AttemptFolder | Out-Null
$script=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'prerequisite_diagnostic_guest.ps1') -Raw
$script | Set-Content (Join-Path $AttemptFolder 'guest_source.ps1') -Encoding UTF8
# Invoke normal commands without modifying any execution-policy setting.
# Preserve the exact readable source alongside the UTF-16 command encoding.
$encoded=[Convert]::ToBase64String([Text.Encoding]::Unicode.GetBytes($script))
@('@echo off',('powershell.exe -NoProfile -NonInteractive -EncodedCommand '+$encoded+' > C:\Diagnostic\guest_stdout.txt 2>&1')) | Set-Content (Join-Path $AttemptFolder 'guest.cmd') -Encoding ASCII
