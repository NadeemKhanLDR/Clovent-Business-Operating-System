param([Parameter(Mandatory)][string]$EvidenceFolder)
$ErrorActionPreference='Stop'
New-Item -ItemType Directory -Path $EvidenceFolder -Force|Out-Null
$helper=Get-Content (Join-Path $PSScriptRoot 'batch2_attempt_helpers.ps1') -Raw
& ([scriptblock]::Create($helper)) # Loaded below in current scope for function tests.
Invoke-Expression $helper
$checks=@()
$outer=New-RetainedProcess $env:ComSpec '/c exit 0'
$inner=New-RetainedProcess "$env:WINDIR\System32\WindowsPowerShell\v1.0\powershell.exe" '-NoProfile -Command "Start-Sleep -Seconds 2; exit 7"'
$clock=[Diagnostics.Stopwatch]::StartNew();$observed=Wait-TrackedNatural @($outer,$inner) 10
if(-not $observed.natural -or 7 -notin $observed.exit_codes -or $clock.Elapsed.TotalSeconds -lt 1){throw 'Inner-process natural completion test failed'}
$checks+=@{name='Launcher exit does not end inner process wait; actual inner exit 7 captured';passed=$true}
$stalled=New-RetainedProcess "$env:WINDIR\System32\WindowsPowerShell\v1.0\powershell.exe" '-NoProfile -Command "Start-Sleep -Seconds 10"'
try{$timed=Wait-TrackedNatural @($stalled) 1;if($timed.natural -or -not $timed.timed_out -or $stalled.Id -notin $timed.active_pids){throw 'Timeout incorrectly classified as natural'}}finally{if(-not $stalled.HasExited){$stalled.Kill();$stalled.WaitForExit(5000)|Out-Null}}
$checks+=@{name='Stalled inner process reports bounded timeout, not natural exit';passed=$true}
$copyCase=Join-Path $EvidenceFolder 'copy_failure';New-Item -ItemType Directory $copyCase|Out-Null
Complete-RunEvidence $copyCase @{outcome='TEST'} {throw 'Simulated sharing violation copying logs'} {Set-Content (Join-Path $copyCase 'export_ran.txt') 'yes'}
$copyResult=Get-Content (Join-Path $copyCase 'guest_result.json') -Raw|ConvertFrom-Json
if($copyResult.finalization_error_count -ne 1 -or -not(Test-Path (Join-Path $copyCase 'export_ran.txt'))){throw 'Copy failure aborted finalization'}
$checks+=@{name='Copy error preserved; export and supervisor completion signal still written';passed=$true}
$exportCase=Join-Path $EvidenceFolder 'export_failure';New-Item -ItemType Directory $exportCase|Out-Null
Complete-RunEvidence $exportCase @{outcome='TEST'} {} {throw 'Simulated event export failure'}
if(-not(Test-Path (Join-Path $exportCase 'guest_result.json'))){throw 'Export failure aborted finalization'}
$checks+=@{name='Export failure cannot suppress supervisor cleanup signal';passed=$true}
foreach($name in @('batch2_attempt_helpers.ps1','batch2_attempt_guest.ps1')){
 $tokens=$null;$errors=$null;[Management.Automation.Language.Parser]::ParseFile((Join-Path $PSScriptRoot $name),[ref]$tokens,[ref]$errors)|Out-Null;if($errors.Count){throw ($errors|Out-String)}
}
$checks+=@{name='Actual helper and guest source parse successfully';passed=$true}
Write-RunJson (Join-Path $EvidenceFolder 'preflight_results.json') @{passed=$true;checks=$checks;engine=$PSVersionTable.PSVersion.ToString();utc=[DateTime]::UtcNow.ToString('o');sandbox_started=$false}
Get-Content (Join-Path $EvidenceFolder 'preflight_results.json')
