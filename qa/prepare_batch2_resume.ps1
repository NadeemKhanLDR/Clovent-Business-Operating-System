param([Parameter(Mandatory)][string]$AttemptFolder)
$ErrorActionPreference='Stop'
$template='D:\CBOS\Worktrees\cbos-unified-candidate\artifacts\oledb18_supported_replacement_20261104_115735_e87e3553\guest_source.ps1'
$source=Get-Content -LiteralPath $template -Raw
$begin=$source.IndexOf("try {`r`n    Record-Json 'guest_started.json'")
if($begin -lt 0){$begin=$source.IndexOf("try {`n    Record-Json 'guest_started.json'")}
$end=$source.IndexOf('} catch {',$begin)
if($begin -lt 0 -or $end -lt 0){throw 'Frozen diagnostic template mismatch'}
$body=@'
function Run-SqlSetup {
    $exe='C:\SqlMedia\SQLEXPR_x64_ENU.exe'
    $args='/Q /ACTION=Install /IACCEPTSQLSERVERLICENSETERMS=1 /FEATURES=SQLEngine /INSTANCENAME=SQLEXPRESS /SQLSVCACCOUNT="NT AUTHORITY\SYSTEM" /SQLSYSADMINACCOUNTS="BUILTIN\Administrators" /TCPENABLED=1 /NPENABLED=1'
    $info=New-Object Diagnostics.ProcessStartInfo
    $info.FileName=$exe;$info.Arguments=$args;$info.UseShellExecute=$false;$info.CreateNoWindow=$true;$info.RedirectStandardOutput=$true;$info.RedirectStandardError=$true
    $proc=New-Object Diagnostics.Process;$proc.StartInfo=$info
    $sqlStart=[DateTime]::UtcNow
    $owner=Get-Content 'C:\Diagnostic\ownership.json' -Raw|ConvertFrom-Json
    $workDeadline=[DateTimeOffset]::Parse([string]$owner.deadline_utc)
    $sqlBudget=[int][Math]::Max(1,[Math]::Floor(($workDeadline-[DateTimeOffset]::UtcNow).TotalSeconds-20))
    $record=@{phase='SQL_SETUP';start_utc=$sqlStart.ToString('o');arguments=$args;timeout_seconds=$sqlBudget;absolute_work_deadline_utc=$owner.deadline_utc;termination='NATURAL'}
    $proc.Start()|Out-Null;$record.pid=$proc.Id
    $out=$proc.StandardOutput.ReadToEndAsync();$err=$proc.StandardError.ReadToEndAsync()
    Record-Json 'sql_setup_started.json' $record
    $timer=[Diagnostics.Stopwatch]::StartNew()
    while(-not $proc.WaitForExit(1000)) {
        $ci=@(Get-WinEvent -FilterHashtable @{LogName='Microsoft-Windows-CodeIntegrity/Operational';Id=3077;StartTime=$sqlStart.ToLocalTime()} -ErrorAction SilentlyContinue)
        $detailFailure=@(Get-ChildItem 'C:\Diagnostic\SqlSetupLogs' -Recurse -Filter Detail.txt -ErrorAction SilentlyContinue|Select-String -Pattern 'MSI Error: 1723|Error: Action ".+" failed during execution' -ErrorAction SilentlyContinue|Select-Object -First 1)
        if($ci.Count -or $detailFailure.Count) {
            $record.termination='HARNESS_STOP_FIRST_BLOCKER'
            $record.blocker=if($ci.Count){'Code Integrity enforcement event 3077 during SQL setup'}else{$detailFailure[0].Line}
            if($ci.Count){('<Events>'+(($ci|ForEach-Object {$_.ToXml()})-join "`n")+'</Events>')|Set-Content 'C:\Diagnostic\first_sql_ci_block.xml' -Encoding UTF8}
            Record-Json 'unresolved_setup_blocker.json' $record
            $proc.Kill();$proc.WaitForExit(5000)|Out-Null
            break
        }
        if($timer.Elapsed.TotalSeconds -ge $sqlBudget){$record.termination='HARNESS_TIMEOUT';$proc.Kill();$proc.WaitForExit(5000)|Out-Null;break}
    }
    $record.exit_code=if($proc.HasExited){$proc.ExitCode}else{$null}
    $record.end_utc=[DateTime]::UtcNow.ToString('o')
    if($out.Wait(3000)){[IO.File]::WriteAllText('C:\Diagnostic\sql_setup_stdout.txt',$out.Result)}
    if($err.Wait(3000)){[IO.File]::WriteAllText('C:\Diagnostic\sql_setup_stderr.txt',$err.Result)}
    Record-Json 'sql_setup_exit.json' $record
    $status.steps+=$record
    if($record.termination -ne 'NATURAL' -or $record.exit_code -notin @(0,3010)){throw "SQL setup unresolved: termination=$($record.termination); exit=$($record.exit_code); blocker=$($record.blocker)"}
}
$setupCollector=Start-Job -ScriptBlock {
    while(-not(Test-Path 'C:\Diagnostic\collector_stop.flag')) {
        $root='C:\Program Files\Microsoft SQL Server\160\Setup Bootstrap\Log'
        if(Test-Path $root){New-Item -ItemType Directory 'C:\Diagnostic\SqlSetupLogs' -Force|Out-Null; Copy-Item "$root\*" 'C:\Diagnostic\SqlSetupLogs' -Recurse -Force -ErrorAction SilentlyContinue}
        Start-Sleep -Seconds 2
    }
}
try {
    Record-Json 'guest_started.json' @{utc=$started.ToString('o');pid=$PID;scope='Frozen 9ccaa17 Batch 2; OLE DB preinstall then unchanged SQL setup; UI only after SQL gates'}
    $status.phase='OLEDB_PREINSTALL'
    $msi='C:\SqlMedia\msoledbsql.msi'
    $sig=Get-AuthenticodeSignature $msi;$hash=(Get-FileHash $msi).Hash
    Record-Json 'oledb_provenance.json' @{sha256=$hash;signature_status=[string]$sig.Status;signer=$sig.SignerCertificate.Subject;version='18.7.6.0'}
    if($hash -ne 'A7B51B2907D8DE4C16E6F11069148A8127A24B8BEA7529A697CAB7817DAE13AE' -or $sig.Status -ne 'Valid' -or $sig.SignerCertificate.Subject -notmatch 'O=Microsoft Corporation'){throw 'OLE DB provenance mismatch'}
    Run-Bounded 'oledb' 'C:\Windows\System32\msiexec.exe' ('/i "'+$msi+'" /qn /norestart IACCEPTMSOLEDBSQLLICENSETERMS=YES /L*V! "C:\Diagnostic\oledb_full_msi.log"') 240
    $status.phase='SQL_SETUP'
    $sqlExe='C:\SqlMedia\SQLEXPR_x64_ENU.exe';$sqlSig=Get-AuthenticodeSignature $sqlExe;$sqlHash=(Get-FileHash $sqlExe).Hash
    Record-Json 'sql_media_provenance.json' @{sha256=$sqlHash;signature_status=[string]$sqlSig.Status;signer=$sqlSig.SignerCertificate.Subject;media_modified=$false}
    if($sqlHash -ne '2E61C8BBDE6021F9026C54AD9DB4BBB1227E68761D4C00A6A50A2C70FE7AFE05' -or $sqlSig.Status -ne 'Valid'){throw 'Original SQL media verification failed'}
    Run-SqlSetup
    $status.phase='SQL_CONNECTION'
    $conn=New-Object System.Data.SqlClient.SqlConnection('Server=localhost\SQLEXPRESS;Database=master;Integrated Security=True;TrustServerCertificate=True;Connect Timeout=10')
    $conn.Open();$cmd=$conn.CreateCommand();$cmd.CommandTimeout=10;$cmd.CommandText='SELECT @@VERSION';$version=[string]$cmd.ExecuteScalar()
    Record-Json 'real_sql_connection.json' @{connected=$true;utc=[DateTime]::UtcNow.ToString('o');version=$version}
    $status.sql_outcome='PASS'
    $status.phase='RESTORE_ISOLATED_BACKUP'
    $backup='C:\SqlMedia\backup06_isolated_copy.bak'
    if((Get-FileHash $backup).Hash -ne '3E1642B87D6B94CE1C4ABDEAC83664630378A707E20627E04FE6E51A046FE518'){throw 'Backup 06 copy hash mismatch'}
    $cmd.CommandText="RESTORE FILELISTONLY FROM DISK='$backup'";$cmd.CommandTimeout=30
    $table=New-Object Data.DataTable;$reader=$cmd.ExecuteReader();$table.Load($reader);$reader.Close()
    New-Item -ItemType Directory 'C:\Batch2IsolatedData' -Force|Out-Null
    $moves=@();$index=0
    foreach($row in $table.Rows){$index++;$ext=if($row.Type -eq 'L'){'ldf'}else{'mdf'};$logical=([string]$row.LogicalName).Replace("'","''");$moves+="MOVE '$logical' TO 'C:\Batch2IsolatedData\batch2_$index.$ext'"}
    $cmd.CommandTimeout=120;$cmd.CommandText="RESTORE DATABASE [Clovent_BusinessOperatingSystem] FROM DISK='$backup' WITH "+($moves -join ', ');$cmd.ExecuteNonQuery()|Out-Null
    Record-Json 'restore_result.json' @{restored=$true;backup_sha256='3E1642B87D6B94CE1C4ABDEAC83664630378A707E20627E04FE6E51A046FE518';database='Clovent_BusinessOperatingSystem';isolated=$true;utc=[DateTime]::UtcNow.ToString('o')};$conn.Close()
    $status.phase='FROZEN_CBOS_INSTALL'
    $installer='C:\SqlMedia\Clovent.BusinessOperatingSystem-1.3.0-Setup-9ccaa17.exe'
    if((Get-FileHash $installer).Hash -ne '5C85CF57048C15A221D363D51F955FD7DB7B1007098AD45ED7A64CE71596C6DC'){throw 'Frozen installer hash mismatch'}
    Run-Bounded 'cbos_install' $installer '/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /LOG="C:\Diagnostic\cbos_install.log"' 120
    $desktop=@('C:\Program Files\Clovent\Business Operating System\Clovent.Desktop.exe','C:\Program Files\Clovent\BusinessOperatingSystem\Clovent.Desktop.exe')|Where-Object {Test-Path $_}|Select-Object -First 1
    if(-not $desktop){throw 'Installed frozen desktop executable missing'}
    $desktopHash=(Get-FileHash $desktop).Hash
    if($desktopHash -ne '5DE94CECCC160085A29EC8A758826F7A755D82FFE0C481C7EF416B98534D6A01'){throw 'Frozen desktop binary hash mismatch'}
    Record-Json 'frozen_binary_verification.json' @{installer_sha256='5C85CF57048C15A221D363D51F955FD7DB7B1007098AD45ED7A64CE71596C6DC';desktop_sha256=$desktopHash;desktop_path=$desktop;candidate='9ccaa17';verified=$true}
    $status.phase='UI_PENDING'
    Record-Json 'ui_ready.json' @{utc=[DateTime]::UtcNow.ToString('o');desktop=$desktop;sql_outcome='PASS';restore=$true;frozen_hashes_verified=$true}
    while(-not(Test-Path 'C:\Diagnostic\ui_complete.json')){Start-Sleep -Seconds 1}
    $status.outcome='SETUP_PASSED_UI_RECORDED'
'@
$source=$source.Substring(0,$begin)+$body+"`r`n"+$source.Substring($end)
$source=$source.Replace('Receive-Job $collector 2>&1','Receive-Job $collector -ErrorAction Continue 2>&1').Replace('Receive-Job $eventCollector 2>&1','Receive-Job $eventCollector -ErrorAction Continue 2>&1')
$source=$source.Replace("    Start-Sleep -Seconds 2`r`n    New-Item", "    Start-Sleep -Seconds 2`r`n    New-Item")
$needle="    Record-Json 'guest_result.json' `$status"
$flush=@'
    if($setupCollector){Wait-Job $setupCollector -Timeout 5|Out-Null;if($setupCollector.State -eq 'Running'){Stop-Job $setupCollector};Receive-Job $setupCollector 2>&1|Out-String|Set-Content 'C:\Diagnostic\setup_collector_output.txt'}
    $sqlLogRoot='C:\Program Files\Microsoft SQL Server\160\Setup Bootstrap\Log'
    if(Test-Path $sqlLogRoot){Copy-Item "$sqlLogRoot\*" 'C:\Diagnostic\SqlSetupLogs' -Recurse -Force -ErrorAction SilentlyContinue}
    Record-Json 'guest_result.json' $status
'@
$source=$source.Replace($needle,$flush)
$source|Set-Content (Join-Path $AttemptFolder 'guest_source.ps1') -Encoding UTF8
$stream=New-Object IO.MemoryStream
$gzip=New-Object IO.Compression.GZipStream($stream,[IO.Compression.CompressionMode]::Compress,$true)
$bytes=[Text.Encoding]::UTF8.GetBytes($source);$gzip.Write($bytes,0,$bytes.Length);$gzip.Dispose()
$packed=[Convert]::ToBase64String($stream.ToArray());$stream.Dispose()
# Compress command transport only; readable frozen source remains beside evidence.
# No execution-policy argument or security-policy setting is changed.
$command="`$m=[IO.MemoryStream]::new([Convert]::FromBase64String('$packed'));`$g=[IO.Compression.GZipStream]::new(`$m,[IO.Compression.CompressionMode]::Decompress);`$r=[IO.StreamReader]::new(`$g,[Text.Encoding]::UTF8);`$code=`$r.ReadToEnd();`$r.Dispose();& ([scriptblock]::Create(`$code))"
$encoded=[Convert]::ToBase64String([Text.Encoding]::Unicode.GetBytes($command))
if($encoded.Length -gt 30000){throw 'Compressed command still exceeds safe command length'}
@('@echo off',('powershell.exe -NoProfile -NonInteractive -EncodedCommand '+$encoded+' > C:\Diagnostic\guest_stdout.txt 2>&1'))|Set-Content (Join-Path $AttemptFolder 'guest.cmd') -Encoding ASCII
$tokens=$null;$errors=$null
[Management.Automation.Language.Parser]::ParseFile((Join-Path $AttemptFolder 'guest_source.ps1'),[ref]$tokens,[ref]$errors)|Out-Null
if($errors.Count){throw ($errors|Out-String)}
