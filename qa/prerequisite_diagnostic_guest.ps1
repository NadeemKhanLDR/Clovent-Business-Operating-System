$ErrorActionPreference = 'Stop'
$evidence = 'C:\Diagnostic'
$started = [DateTime]::UtcNow
$status = @{ start_utc=$started.ToString('o'); steps=@(); outcome='ERROR' }
function Record-Json($name, $value) {
    $value | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath (Join-Path $evidence $name) -Encoding UTF8
}
function Run-Bounded($name, $exe, $arguments, $seconds) {
    $record = @{ name=$name; exe=$exe; arguments=$arguments; start_utc=[DateTime]::UtcNow.ToString('o'); timeout_seconds=$seconds }
    # Keep a native Process handle from launch through exit. Windows PowerShell
    # Start-Process -PassThru can otherwise yield a null ExitCode after reaping.
    $info = New-Object System.Diagnostics.ProcessStartInfo
    $info.FileName = $exe
    $info.Arguments = $arguments
    $info.UseShellExecute = $false
    $info.CreateNoWindow = $true
    $info.RedirectStandardOutput = $true
    $info.RedirectStandardError = $true
    $proc = New-Object System.Diagnostics.Process
    $proc.StartInfo = $info
    if (-not $proc.Start()) { throw "Process start failed: $name" }
    $stdoutTask = $proc.StandardOutput.ReadToEndAsync()
    $stderrTask = $proc.StandardError.ReadToEndAsync()
    $record.pid = $proc.Id
    Record-Json "${name}_started.json" $record
    if (-not $proc.WaitForExit($seconds * 1000)) {
        $record.timed_out = $true
        $proc.Kill()
        $proc.WaitForExit(5000) | Out-Null
        $record.exit_code = $null
    } else {
        $proc.WaitForExit()
        $record.exit_code = $proc.ExitCode
    }
    if ($stdoutTask.Wait(3000)) { [IO.File]::WriteAllText("$evidence\${name}_stdout.txt", $stdoutTask.Result) }
    if ($stderrTask.Wait(3000)) { [IO.File]::WriteAllText("$evidence\${name}_stderr.txt", $stderrTask.Result) }
    $record.end_utc = [DateTime]::UtcNow.ToString('o')
    Record-Json "${name}_exit.json" $record
    $status.steps += $record
    if ($null -eq $record.exit_code -and -not $record.timed_out) { throw "HARNESS ERROR: missing exit code for $name" }
    if ($record.timed_out -or $record.exit_code -notin @(0,3010)) { throw "First prerequisite/extraction failure: $name; exit=$($record.exit_code); timed_out=$($record.timed_out)" }
}

# Collector runs independently of MSI; no event formatting/truncation, no policy writes.
$collector = Start-Job -ArgumentList $evidence, $started -ScriptBlock {
    param($evidence,$started)
    $ErrorActionPreference='Continue'
    $files=New-Item -ItemType Directory -Path "$evidence\CapturedInstallerFiles" -Force
    $seen=@{}
    while (-not (Test-Path "$evidence\collector_stop.flag")) {
        foreach ($file in @(Get-ChildItem 'C:\Windows\Installer\MSI*.tmp' -File -ErrorAction SilentlyContinue)) {
            $key=$file.Name+'_'+$file.Length+'_'+$file.LastWriteTimeUtc.Ticks
            if (-not $seen.ContainsKey($key)) {
                try {
                    $dest=Join-Path $files.FullName ($key+'.bin')
                    Copy-Item -LiteralPath $file.FullName -Destination $dest -ErrorAction Stop
                    # Copy first; trust verification can block long enough to miss
                    # the next transient DLL. Hash/sign these preserved bytes later.
                    @{source=$file.FullName;captured_utc=[DateTime]::UtcNow.ToString('o');copy=$dest} | ConvertTo-Json -Compress | Add-Content "$evidence\captured_file_paths.jsonl"
                    $seen[$key]=$true
                } catch { $_ | Out-String | Add-Content "$evidence\collector_errors.txt" }
            }
        }
        Start-Sleep -Milliseconds 10
    }
}
$eventCollector = Start-Job -ArgumentList $evidence -ScriptBlock {
    param($evidence)
    while (-not (Test-Path "$evidence\collector_stop.flag")) {
        foreach ($channel in @('Microsoft-Windows-CodeIntegrity/Operational','Application')) {
            $label=if ($channel -eq 'Application') {'Application'} else {'CodeIntegrity'}
            $events=@(Get-WinEvent -LogName $channel -ErrorAction SilentlyContinue)
            ('<Events>'+ (($events | ForEach-Object {$_.ToXml()}) -join "`n") + '</Events>') | Set-Content "$evidence\${label}_live.xml" -Encoding UTF8
        }
        Start-Sleep -Seconds 1
    }
}
try {
    Record-Json 'guest_started.json' @{utc=$started.ToString('o');pid=$PID;scope='prerequisites only; no SQL engine or CBOS install'}
    $source='C:\SqlMedia\SQLEXPR_x64_ENU.exe'
    $sig=Get-AuthenticodeSignature $source
    $mediaHash=(Get-FileHash $source).Hash
    Record-Json 'media_provenance.json' @{path=$source;sha256=$mediaHash;expected_sha256='2E61C8BBDE6021F9026C54AD9DB4BBB1227E68761D4C00A6A50A2C70FE7AFE05';signature_status=[string]$sig.Status;signer=$sig.SignerCertificate.Subject}
    if ($mediaHash -ne '2E61C8BBDE6021F9026C54AD9DB4BBB1227E68761D4C00A6A50A2C70FE7AFE05') { throw 'SQL source hash mismatch; refusing execution.' }
    if ($sig.Status -ne 'Valid') { throw 'SQL source signature validation failed; refusing execution.' }
    Run-Bounded 'extract' $source '/Q /x:C:\SqlExtract' 150
    $msi=Get-ChildItem 'C:\SqlExtract' -Recurse -File -Filter msoledbsql.msi | Where-Object {$_.FullName -match '\\x64\\'} | Select-Object -First 1
    if (-not $msi) { throw 'OLE DB x64 MSI not found in extracted media.' }
    Record-Json 'msi_provenance.json' @{path=$msi.FullName;sha256=(Get-FileHash $msi.FullName).Hash;signature_status=[string](Get-AuthenticodeSignature $msi.FullName).Status}
    # Direct failing prerequisite only, with full verbose flushed MSI log. No SQL engine invocation.
    Run-Bounded 'oledb' 'C:\Windows\System32\msiexec.exe' ('/i "'+$msi.FullName+'" /qn /norestart IACCEPTMSOLEDBSQLLICENSETERMS=YES /L*V! "C:\Diagnostic\oledb_full_msi.log"') 120
    $status.outcome='OLEDB_PASSED'
    # Success ends this targeted diagnostic; no further prerequisite installs are needed to identify the previous failure.
} catch {
    $status.outcome='FIRST_FAILURE'
    $status.error=$_.ToString()
    $_ | Out-String | Set-Content "$evidence\first_failure.txt"
} finally {
    Start-Sleep -Seconds 2
    New-Item -ItemType File "$evidence\collector_stop.flag" -Force | Out-Null
    Wait-Job $collector -Timeout 10 | Out-Null
    if ($collector.State -eq 'Running') { Stop-Job $collector }
    Receive-Job $collector 2>&1 | Out-String | Set-Content "$evidence\collector_output.txt"
    Wait-Job $eventCollector -Timeout 5 | Out-Null
    if ($eventCollector.State -eq 'Running') { Stop-Job $eventCollector }
    Receive-Job $eventCollector 2>&1 | Out-String | Set-Content "$evidence\event_collector_output.txt"
    foreach ($line in @(Get-Content "$evidence\captured_file_paths.jsonl" -ErrorAction SilentlyContinue)) {
        $item=$line | ConvertFrom-Json
        $sig=Get-AuthenticodeSignature -LiteralPath $item.copy
        @{source=$item.source;captured_utc=$item.captured_utc;sha256=(Get-FileHash $item.copy).Hash;signature_status=[string]$sig.Status;signature_message=$sig.StatusMessage;signer=$sig.SignerCertificate.Subject;copy=$item.copy} | ConvertTo-Json -Compress | Add-Content "$evidence\captured_files.jsonl"
    }
    foreach ($channel in @('Microsoft-Windows-CodeIntegrity/Operational','Application')) {
        $label=if ($channel -eq 'Application') {'Application'} else {'CodeIntegrity'}
        $events=@(Get-WinEvent -LogName $channel -ErrorAction SilentlyContinue)
        ('<Events>'+ (($events | ForEach-Object {$_.ToXml()}) -join "`n") + '</Events>') | Set-Content "$evidence\${label}.xml" -Encoding UTF8
        try { Run-Bounded "export_$label" 'C:\Windows\System32\wevtutil.exe' "epl $channel C:\Diagnostic\$label.evtx /ow:true" 10 } catch { $_ | Out-String | Add-Content "$evidence\export_errors.txt" }
    }
    $status.end_utc=[DateTime]::UtcNow.ToString('o')
    Record-Json 'guest_result.json' $status
}
