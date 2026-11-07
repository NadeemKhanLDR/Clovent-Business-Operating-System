function Write-RunJson($Path,$Value) {
    $temp=$Path+'.writing'
    $Value|ConvertTo-Json -Depth 12|Set-Content -LiteralPath $temp -Encoding UTF8 -ErrorAction Stop
    Move-Item -LiteralPath $temp -Destination $Path -Force -ErrorAction Stop
}
function New-RetainedProcess($Exe,$Arguments) {
    $info=New-Object Diagnostics.ProcessStartInfo
    $info.FileName=$Exe;$info.Arguments=$Arguments;$info.UseShellExecute=$false;$info.CreateNoWindow=$true
    $process=New-Object Diagnostics.Process;$process.StartInfo=$info
    if(-not $process.Start()){throw "Could not start $Exe"}
    $null=$process.Handle
    return $process
}
function Wait-TrackedNatural($Processes,$Seconds) {
    $clock=[Diagnostics.Stopwatch]::StartNew()
    while($true){
        $active=@($Processes|Where-Object {-not $_.HasExited})
        if(-not $active.Count){return @{natural=$true;exit_codes=@($Processes|ForEach-Object {$_.ExitCode})}}
        if($clock.Elapsed.TotalSeconds -ge $Seconds){return @{natural=$false;timed_out=$true;active_pids=@($active|ForEach-Object {$_.Id})}}
        Start-Sleep -Milliseconds 50
    }
}
function Invoke-RecordedNative($Folder,$Name,$Exe,$Arguments,$Seconds) {
    $record=@{name=$Name;start_utc=[DateTime]::UtcNow.ToString('o');exe=$Exe;arguments=$Arguments;timeout_seconds=$Seconds;termination='NATURAL'}
    $process=New-RetainedProcess $Exe $Arguments;$record.pid=$process.Id
    Write-RunJson (Join-Path $Folder ($Name+'_started.json')) $record
    if(-not $process.WaitForExit($Seconds*1000)){
        $record.termination='HARNESS_TIMEOUT';$process.Kill();$process.WaitForExit(5000)|Out-Null
    }
    $record.exit_code=if($process.HasExited){$process.ExitCode}else{$null}
    $record.end_utc=[DateTime]::UtcNow.ToString('o')
    Write-RunJson (Join-Path $Folder ($Name+'_exit.json')) $record
    if($record.termination -ne 'NATURAL' -or $record.exit_code -notin @(0,3010)){throw "Native step $Name unresolved: $($record.termination), exit $($record.exit_code)"}
    return $record
}
function Complete-RunEvidence($Folder,$Result,[scriptblock]$Copy,[scriptblock]$Export) {
    # Evidence failures are recorded separately; completion always reaches the
    # supervisor's cleanup signal. No job error stream is replayed as fatal.
    $errorsFound=@()
    try { & $Copy } catch {$errorsFound+=@{phase='copy';error=$_.ToString()}}
    try { & $Export } catch {$errorsFound+=@{phase='export';error=$_.ToString()}}
    try { Write-RunJson (Join-Path $Folder 'finalization_errors.json') @($errorsFound) } catch {}
    $Result.finalization_error_count=$errorsFound.Count
    $Result.end_utc=[DateTime]::UtcNow.ToString('o')
    Write-RunJson (Join-Path $Folder 'guest_result.json') $Result
}
