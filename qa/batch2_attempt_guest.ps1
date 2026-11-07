$ErrorActionPreference='Stop'
$folder='C:\Diagnostic'
$run=@{start_utc=[DateTime]::UtcNow.ToString('o');outcome='UNFINISHED';phase='PREFLIGHT';sql_outcome='NOT EXECUTED';ui_outcome='NOT EXECUTED'}
$owner=Get-Content "$folder\ownership.json" -Raw|ConvertFrom-Json
$deadline=[DateTimeOffset]::Parse([string]$owner.deadline_utc)
function Remaining-Work {return [int][Math]::Max(0,[Math]::Floor(($deadline-[DateTimeOffset]::UtcNow).TotalSeconds))}
function Verify-Asset($Name,$Expected,$RequireMicrosoft) {
    $path='C:\SqlMedia\'+$Name;$actual=(Get-FileHash -LiteralPath $path).Hash
    $record=@{name=$Name;sha256=$actual;expected_sha256=$Expected}
    if($RequireMicrosoft){$sig=Get-AuthenticodeSignature -LiteralPath $path;$record.signature_status=[string]$sig.Status;$record.signer=$sig.SignerCertificate.Subject;if($sig.Status -ne 'Valid' -or $sig.SignerCertificate.Subject -notmatch 'O=Microsoft Corporation'){throw "Microsoft signature invalid: $Name"}}
    Write-RunJson "$folder\verified_$Name.json" $record
    if($actual -ne $Expected){throw "Frozen asset hash mismatch: $Name"}
    return $path
}
$collector=$null
try {
    Write-RunJson "$folder\guest_started.json" @{utc=$run.start_utc;pid=$PID;deadline=$owner.deadline_utc;scope='Frozen 9ccaa17; no business transactions'}
    $collector=Start-Job -ScriptBlock {
        $ErrorActionPreference='Continue'
        while(-not(Test-Path 'C:\Diagnostic\collector_stop.flag')){
            $root='C:\Program Files\Microsoft SQL Server\160\Setup Bootstrap\Log'
            if(Test-Path $root){New-Item -ItemType Directory 'C:\Diagnostic\SqlSetupLogs' -Force -ErrorAction SilentlyContinue|Out-Null;Copy-Item "$root\*" 'C:\Diagnostic\SqlSetupLogs' -Recurse -Force -ErrorAction Continue 2>>'C:\Diagnostic\log_copy_errors.txt'}
            foreach($channel in @('Microsoft-Windows-CodeIntegrity/Operational','Application')){
                $name=if($channel -eq 'Application'){'Application'}else{'CodeIntegrity'}
                try {$events=@(Get-WinEvent -LogName $channel -ErrorAction SilentlyContinue);$xml='<Events>'+(($events|ForEach-Object {$_.ToXml()})-join "`n")+'</Events>';[IO.File]::WriteAllText("C:\Diagnostic\${name}_live.xml.writing",$xml);Move-Item "C:\Diagnostic\${name}_live.xml.writing" "C:\Diagnostic\${name}_live.xml" -Force -ErrorAction Continue 2>>'C:\Diagnostic\event_copy_errors.txt'}catch{[IO.File]::AppendAllText('C:\Diagnostic\event_copy_errors.txt',($_.ToString()+"`r`n"))}
            }
            Start-Sleep -Seconds 2
        }
    }
    $run.phase='OLEDB_PREINSTALL'
    $msi=Verify-Asset 'msoledbsql.msi' 'A7B51B2907D8DE4C16E6F11069148A8127A24B8BEA7529A697CAB7817DAE13AE' $true
    $null=Invoke-RecordedNative $folder 'oledb' 'C:\Windows\System32\msiexec.exe' ('/i "'+$msi+'" /qn /norestart IACCEPTMSOLEDBSQLLICENSETERMS=YES /L*V! "C:\Diagnostic\oledb_full_msi.log"') 240
    $run.phase='SQL_SETUP'
    $sql=Verify-Asset 'SQLEXPR_x64_ENU.exe' '2E61C8BBDE6021F9026C54AD9DB4BBB1227E68761D4C00A6A50A2C70FE7AFE05' $true
    $arguments='/Q /ACTION=Install /IACCEPTSQLSERVERLICENSETERMS=1 /FEATURES=SQLEngine /INSTANCENAME=SQLEXPRESS /SQLSVCACCOUNT="NT AUTHORITY\SYSTEM" /SQLSYSADMINACCOUNTS="BUILTIN\Administrators" /TCPENABLED=1 /NPENABLED=1'
    $sqlStart=[DateTime]::UtcNow
    $sqlBudget=[int][Math]::Min(1500,[Math]::Max(1,(Remaining-Work)-60))
    $record=@{start_utc=$sqlStart.ToString('o');timeout_seconds=$sqlBudget;maximum_sql_seconds=1500;termination='NATURAL';inner_processes=@()}
    $launcher=New-RetainedProcess $sql $arguments;$record.launcher_pid=$launcher.Id
    $tracked=@{};$clock=[Diagnostics.Stopwatch]::StartNew();$quiet=$null
    Write-RunJson "$folder\sql_setup_started.json" $record
    while($true){
        $found=@(Get-CimInstance Win32_Process -Filter "Name='setup.exe' OR Name='SETUP.EXE' OR Name='setup100.exe'" -ErrorAction Stop)
        foreach($item in $found){
            if($item.ProcessId -eq $launcher.Id -or $item.CreationDate.ToUniversalTime() -lt $sqlStart.AddSeconds(-1)){continue}
            $key=[string]$item.ProcessId
            if(-not $tracked.ContainsKey($key)){
                try {$proc=[Diagnostics.Process]::GetProcessById($item.ProcessId);$null=$proc.Handle;$tracked[$key]=@{process=$proc;pid=$item.ProcessId;parent_pid=$item.ParentProcessId;exe=$item.ExecutablePath;command_line=$item.CommandLine;observed_utc=[DateTime]::UtcNow.ToString('o')}}catch{[IO.File]::AppendAllText("$folder\process_tracking_errors.txt",($_.ToString()+"`r`n"))}
            }
        }
        $snapshots=@();$active=0
        foreach($entry in $tracked.Values){
            $snapshot=@{pid=$entry.pid;parent_pid=$entry.parent_pid;exe=$entry.exe;command_line=$entry.command_line;observed_utc=$entry.observed_utc;has_exited=$entry.process.HasExited}
            if($entry.process.HasExited){$snapshot.exit_code=$entry.process.ExitCode}else{$active++}
            $snapshots+=$snapshot
        }
        $record.inner_processes=$snapshots
        $record.launcher_exited=$launcher.HasExited
        if($launcher.HasExited){$record.launcher_exit_code=$launcher.ExitCode}
        Write-RunJson "$folder\sql_process_tracking.json" $record
        if($tracked.Count -gt 0 -and $active -eq 0 -and $launcher.HasExited){
            if(-not $quiet){$quiet=[Diagnostics.Stopwatch]::StartNew()}
            if($quiet.Elapsed.TotalSeconds -ge 3){break}
        }else{$quiet=$null}
        $blocks=@(Get-WinEvent -FilterHashtable @{LogName='Microsoft-Windows-CodeIntegrity/Operational';Id=3077;StartTime=$sqlStart.ToLocalTime()} -ErrorAction SilentlyContinue)
        if($blocks.Count){$record.termination='BLOCKED_BY_CODE_INTEGRITY';('<Events>'+(($blocks|ForEach-Object {$_.ToXml()})-join "`n")+'</Events>')|Set-Content "$folder\sql_block_events.xml" -Encoding UTF8;break}
        if($clock.Elapsed.TotalSeconds -ge $sqlBudget){$record.termination='HARNESS_SQL_DEADLINE';break}
        Start-Sleep -Seconds 1
    }
    $record.end_utc=[DateTime]::UtcNow.ToString('o')
    Write-RunJson "$folder\sql_setup_exit.json" $record
    # Do not kill just a launcher and mistake that for setup termination. On any
    # blocker the finally path exports evidence, then supervisor stops the VM.
    $bad=@($record.inner_processes|Where-Object {-not $_.has_exited -or $_.exit_code -notin @(0,3010)})
    if($record.termination -ne 'NATURAL' -or -not $tracked.Count -or $bad.Count -or $record.launcher_exit_code -notin @(0,3010)){throw "SQL not naturally completed: $($record.termination); inner count=$($tracked.Count); unresolved/failed=$($bad.Count)"}
    $run.phase='SQL_CONNECTION'
    $connection=New-Object Data.SqlClient.SqlConnection('Server=localhost\SQLEXPRESS;Database=master;Integrated Security=True;TrustServerCertificate=True;Connect Timeout=10')
    $connection.Open();$cmd=$connection.CreateCommand();$cmd.CommandTimeout=10;$cmd.CommandText='SELECT @@VERSION';$version=[string]$cmd.ExecuteScalar()
    Write-RunJson "$folder\real_sql_connection.json" @{connected=$true;version=$version;utc=[DateTime]::UtcNow.ToString('o')};$run.sql_outcome='PASS'
    if((Remaining-Work) -lt 180){throw 'SQL passed; insufficient work time for restore/install/UI'}
    $run.phase='ISOLATED_RESTORE'
    $backup=Verify-Asset 'backup06_isolated_copy.bak' '3E1642B87D6B94CE1C4ABDEAC83664630378A707E20627E04FE6E51A046FE518' $false
    $cmd.CommandText="RESTORE FILELISTONLY FROM DISK='$backup'";$cmd.CommandTimeout=30;$reader=$cmd.ExecuteReader();$files=New-Object Data.DataTable;$files.Load($reader);$reader.Close()
    New-Item -ItemType Directory 'C:\Batch2IsolatedData' -Force|Out-Null;$moves=@();$i=0
    foreach($file in $files.Rows){$i++;$extension=if($file.Type -eq 'L'){'ldf'}else{'mdf'};$logical=([string]$file.LogicalName).Replace("'","''");$moves+="MOVE '$logical' TO 'C:\Batch2IsolatedData\batch2_$i.$extension'"}
    $cmd.CommandText="RESTORE DATABASE [Clovent_BusinessOperatingSystem] FROM DISK='$backup' WITH "+($moves-join ', ');$cmd.CommandTimeout=120;$cmd.ExecuteNonQuery()|Out-Null;$connection.Close()
    Write-RunJson "$folder\restore_result.json" @{restored=$true;isolated=$true;utc=[DateTime]::UtcNow.ToString('o')}
    $run.phase='FROZEN_CBOS_INSTALL'
    $installer=Verify-Asset 'Clovent.BusinessOperatingSystem-1.3.0-Setup-9ccaa17.exe' '5C85CF57048C15A221D363D51F955FD7DB7B1007098AD45ED7A64CE71596C6DC' $false
    $null=Invoke-RecordedNative $folder 'cbos_install' $installer '/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /LOG="C:\Diagnostic\cbos_install.log"' 120
    $desktop=@('C:\Program Files\Clovent\Business Operating System\Clovent.Desktop.exe','C:\Program Files\Clovent\BusinessOperatingSystem\Clovent.Desktop.exe')|Where-Object {Test-Path $_}|Select-Object -First 1
    if(-not $desktop){throw 'Frozen desktop executable missing'}
    $hash=(Get-FileHash $desktop).Hash
    if($hash -ne '5DE94CECCC160085A29EC8A758826F7A755D82FFE0C481C7EF416B98534D6A01'){throw 'Frozen installed desktop hash mismatch'}
    Write-RunJson "$folder\frozen_binary_verification.json" @{candidate='9ccaa17';desktop_path=$desktop;desktop_sha256=$hash;verified=$true}
    $run.phase='UI_PENDING'
    Write-RunJson "$folder\ui_ready.json" @{desktop=$desktop;utc=[DateTime]::UtcNow.ToString('o');remaining_seconds=(Remaining-Work)}
    while(-not(Test-Path "$folder\ui_complete.json") -and (Remaining-Work) -gt 45){Start-Sleep -Seconds 1}
    $run.outcome='SETUP_PASS_UI_RECORDED_OR_TIME_EXPIRED'
} catch {
    $run.outcome='STOPPED_AT_BLOCKER';$run.error=$_.ToString()
    try{Write-RunJson "$folder\first_blocker.json" $run}catch{}
} finally {
    Complete-RunEvidence $folder $run {
        # No Receive-Job error replay. Stop collector within a bounded wait.
        New-Item -ItemType File "$folder\collector_stop.flag" -Force -ErrorAction SilentlyContinue|Out-Null
        if($collector){Wait-Job $collector -Timeout 3 -ErrorAction SilentlyContinue|Out-Null;if($collector.State -eq 'Running'){Stop-Job $collector -ErrorAction SilentlyContinue};$collector.ChildJobs|ForEach-Object {$_.Error|Out-String|Add-Content "$folder\collector_recorded_errors.txt" -ErrorAction SilentlyContinue}}
        $root='C:\Program Files\Microsoft SQL Server\160\Setup Bootstrap\Log'
        if(Test-Path $root){Copy-Item "$root\*" "$folder\SqlSetupLogs" -Recurse -Force -ErrorAction Continue 2>>"$folder\final_log_copy_errors.txt"}
    } {
        foreach($channel in @('Microsoft-Windows-CodeIntegrity/Operational','Application')){
            $name=if($channel -eq 'Application'){'Application'}else{'CodeIntegrity'}
            try{$events=@(Get-WinEvent -LogName $channel -ErrorAction Stop);('<Events>'+(($events|ForEach-Object {$_.ToXml()})-join "`n")+'</Events>')|Set-Content "$folder\${name}_final.xml" -Encoding UTF8}catch{$_.ToString()|Add-Content "$folder\final_event_errors.txt"}
            try{$null=Invoke-RecordedNative $folder "export_$name" 'C:\Windows\System32\wevtutil.exe' "epl $channel C:\Diagnostic\$name.evtx /ow:true" 10}catch{$_.ToString()|Add-Content "$folder\final_event_errors.txt"}
        }
    }
}
