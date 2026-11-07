param([Parameter(Mandatory)][string]$EvidenceFolder)
$ErrorActionPreference='Stop'
$evidence=$EvidenceFolder
$status=@{steps=@()}
New-Item -ItemType Directory -Path $evidence -Force | Out-Null
$tokens=$null;$errors=$null
$ast=[Management.Automation.Language.Parser]::ParseFile((Join-Path $PSScriptRoot 'prerequisite_diagnostic_guest.ps1'),[ref]$tokens,[ref]$errors)
if($errors.Count){throw ($errors | Out-String)}
foreach($name in @('Record-Json','Run-Bounded')) {
    $functionAst=$ast.Find({param($node) $node -is [Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -eq $name},$true)
    Invoke-Expression $functionAst.Extent.Text
}
Run-Bounded 'exit_zero' $env:ComSpec '/c exit 0' 3
$caught=$false
try {Run-Bounded 'exit_seven' $env:ComSpec '/c exit 7' 3} catch {$caught=$true}
if(-not $caught -or $status.steps[-1].exit_code -ne 7){throw 'Nonzero exit test failed'}
$caught=$false
try {Run-Bounded 'timeout' "$PSHOME\powershell.exe" '-NoProfile -Command Start-Sleep -Seconds 10' 1} catch {$caught=$true}
if(-not $caught -or -not $status.steps[-1].timed_out){throw 'Bounded timeout test failed'}
@{passed=$true;tests=3;engine=$PSVersionTable.PSVersion.ToString();utc=[DateTime]::UtcNow.ToString('o')} | ConvertTo-Json | Set-Content "$evidence\process_capture_tests.json"
