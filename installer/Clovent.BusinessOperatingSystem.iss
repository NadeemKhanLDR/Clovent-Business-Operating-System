; ==============================================================================
; Clovent Business Operating System (CBOS) - Production Installer Script
; Release: 1.0.8 (win-x64)
; Technology: Inno Setup 6 (Native 64-bit, elevated, prerequisite-chained)
; ==============================================================================

#define MyAppName "Clovent Business Operating System"
#define MyAppVersion "1.0.8"
#define MyAppPublisher "Clovent"
#define MyAppExeName "Clovent.Desktop.exe"
#define MyAppId "{{C107E47D-CB05-47F1-9BD7-A107CB052026}}"

[Setup]
AppId={#MyAppId}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\Clovent\Business Operating System
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
OutputDir=..\artifacts\installer
OutputBaseFilename=Clovent.BusinessOperatingSystem-1.0.8-Setup
; SetupIconFile=..\src\Clovent.Desktop\Resources\cbos.ico
UninstallDisplayIcon={app}\Clovent.Desktop.exe
Compression=lzma2/max
SolidCompression=yes
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=admin
PrivilegesRequiredOverridesAllowed=commandline dialog
WizardStyle=modern
DisableDirPage=no
DisableProgramGroupPage=yes
DisableReadyPage=no
CloseApplications=yes
RestartApplications=no
SetupLogging=yes

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; Main application payload (CBOS 1.0.8 release binaries)
Source: "..\artifacts\release\Clovent.BusinessOperatingSystem-1.0.8-win-x64\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

; Internal silent database provisioner tool (extracted to {tmp} and deleted on setup completion)
Source: "..\tools\Clovent.Installer.Provisioner\bin\publish\Clovent.Installer.Provisioner.exe"; DestDir: "{tmp}"; Flags: deleteafterinstall ignoreversion
Source: "..\tools\Clovent.Installer.Provisioner\bin\publish\Microsoft.Data.SqlClient.SNI.dll"; DestDir: "{tmp}"; Flags: deleteafterinstall ignoreversion

[Icons]
Name: "{autoprograms}\{#MyAppName}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\Resources\cbos.ico"
Name: "{autoprograms}\{#MyAppName}\Support Diagnostics"; Filename: "{app}\{#MyAppExeName}"; Parameters: "--diagnostics"; IconFilename: "{app}\Resources\cbos.ico"
Name: "{autoprograms}\{#MyAppName}\Uninstall {#MyAppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\Resources\cbos.ico"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[Code]
var
  DetectedSqlInstance: String;
  DetectedSqlServer: String;
  SqlNeedsInstall: Boolean;
  IsUpgradeMode: Boolean;

// Helper: Append line to installer log file in %ProgramData%\Clovent\BusinessOperatingSystem\Logs
procedure LogInstallerMessage(Msg: String);
var
  LogDir, LogFile, TimeStamp: String;
begin
  LogDir := ExpandConstant('{commonappdata}\Clovent\BusinessOperatingSystem\Logs');
  ForceDirectories(LogDir);
  LogFile := LogDir + '\Setup-1.0.8.log';
  TimeStamp := GetDateTimeString('yyyy-mm-dd hh:nn:ss', '-', ':');
  SaveStringToFile(LogFile, '[' + TimeStamp + '] ' + Msg + #13#10, True);
  Log(Msg);
end;

// Helper: Check if a Windows Service is running via sc.exe query
function IsServiceRunning(ServiceName: String): Boolean;
var
  ResultCode: Integer;
  TempFile: String;
  Output: AnsiString;
begin
  TempFile := ExpandConstant('{tmp}\sc_' + ServiceName + '.txt');
  ResultCode := -1;
  Exec(ExpandConstant('{cmd}'), '/c sc query "' + ServiceName + '" > "' + TempFile + '" 2>&1', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  if (ResultCode = 0) and FileExists(TempFile) then
  begin
    LoadStringFromFile(TempFile, Output);
    DeleteFile(TempFile);
    Result := (Pos('RUNNING', String(Output)) > 0);
  end
  else
    Result := False;
end;

// Helper: Check if a Windows Service exists
function DoesServiceExist(ServiceName: String): Boolean;
var
  ResultCode: Integer;
  TempFile: String;
  Output: AnsiString;
begin
  TempFile := ExpandConstant('{tmp}\sc_exist_' + ServiceName + '.txt');
  ResultCode := -1;
  Exec(ExpandConstant('{cmd}'), '/c sc query "' + ServiceName + '" > "' + TempFile + '" 2>&1', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  if FileExists(TempFile) then
  begin
    LoadStringFromFile(TempFile, Output);
    DeleteFile(TempFile);
    Result := (Pos('1060', String(Output)) = 0); // 1060 means ERROR_SERVICE_DOES_NOT_EXIST
  end
  else
    Result := False;
end;

// Helper: Start a Windows Service and wait up to 30 seconds
function EnsureServiceStarted(ServiceName: String): Boolean;
var
  ResultCode, Attempts: Integer;
begin
  if IsServiceRunning(ServiceName) then
  begin
    Result := True;
    Exit;
  end;

  LogInstallerMessage('Starting service: ' + ServiceName + '...');
  Exec('net.exe', 'start "' + ServiceName + '"', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);

  for Attempts := 1 to 15 do
  begin
    Sleep(2000);
    if IsServiceRunning(ServiceName) then
    begin
      LogInstallerMessage('Service ' + ServiceName + ' is now RUNNING.');
      Result := True;
      Exit;
    end;
  end;

  Result := IsServiceRunning(ServiceName);
end;

// Detect existing compatible local SQL Server instance
function DetectSqlServer(): Boolean;
var
  Names: TArrayOfString;
  I: Integer;
  Inst: String;
begin
  DetectedSqlInstance := '';
  DetectedSqlServer := '';

  LogInstallerMessage('Scanning local system for compatible Microsoft SQL Server instances...');

  // 1. Check registry instance names
  if RegGetValueNames(HKEY_LOCAL_MACHINE, 'SOFTWARE\Microsoft\Microsoft SQL Server\Instance Names\SQL', Names) then
  begin
    // Check CLOVENT named instance first
    for I := 0 to GetArrayLength(Names) - 1 do
    begin
      Inst := Names[I];
      if CompareText(Inst, 'CLOVENT') = 0 then
      begin
        DetectedSqlInstance := 'CLOVENT';
        DetectedSqlServer := '.\CLOVENT';
        LogInstallerMessage('Found dedicated instance: ' + DetectedSqlServer);
        EnsureServiceStarted('MSSQL$CLOVENT');
        Result := True;
        Exit;
      end;
    end;

    // Check default MSSQLSERVER instance
    for I := 0 to GetArrayLength(Names) - 1 do
    begin
      Inst := Names[I];
      if CompareText(Inst, 'MSSQLSERVER') = 0 then
      begin
        DetectedSqlInstance := 'MSSQLSERVER';
        DetectedSqlServer := '(local)';
        LogInstallerMessage('Found default instance: ' + DetectedSqlServer);
        EnsureServiceStarted('MSSQLSERVER');
        Result := True;
        Exit;
      end;
    end;

    // Check standard SQLEXPRESS instance
    for I := 0 to GetArrayLength(Names) - 1 do
    begin
      Inst := Names[I];
      if CompareText(Inst, 'SQLEXPRESS') = 0 then
      begin
        DetectedSqlInstance := 'SQLEXPRESS';
        DetectedSqlServer := '.\SQLEXPRESS';
        LogInstallerMessage('Found express instance: ' + DetectedSqlServer);
        EnsureServiceStarted('MSSQL$SQLEXPRESS');
        Result := True;
        Exit;
      end;
    end;

    // Any other registered instance
    if GetArrayLength(Names) > 0 then
    begin
      Inst := Names[0];
      DetectedSqlInstance := Inst;
      DetectedSqlServer := '.\' + Inst;
      LogInstallerMessage('Found local SQL instance: ' + DetectedSqlServer);
      EnsureServiceStarted('MSSQL$' + Inst);
      Result := True;
      Exit;
    end;
  end;

  // 2. Service fallback check
  if DoesServiceExist('MSSQL$CLOVENT') then
  begin
    DetectedSqlInstance := 'CLOVENT';
    DetectedSqlServer := '.\CLOVENT';
    EnsureServiceStarted('MSSQL$CLOVENT');
    Result := True;
    Exit;
  end;

  if DoesServiceExist('MSSQLSERVER') then
  begin
    DetectedSqlInstance := 'MSSQLSERVER';
    DetectedSqlServer := '(local)';
    EnsureServiceStarted('MSSQLSERVER');
    Result := True;
    Exit;
  end;

  if DoesServiceExist('MSSQL$SQLEXPRESS') then
  begin
    DetectedSqlInstance := 'SQLEXPRESS';
    DetectedSqlServer := '.\SQLEXPRESS';
    EnsureServiceStarted('MSSQL$SQLEXPRESS');
    Result := True;
    Exit;
  end;

  LogInstallerMessage('No local SQL Server instance detected.');
  Result := False;
end;

// Download and install SQL Server 2022 Express silently
function InstallSqlServerExpress(): Boolean;
var
  SqlSetupPath, SqlParams: String;
  ResultCode: Integer;
  DownloadUrl, TempSqlExe: String;
begin
  LogInstallerMessage('Initiating automatic SQL Server Express installation...');

  // 1. Look for pre-staged installer beside Setup.exe
  SqlSetupPath := ExpandConstant('{src}\SQLEXPR_x64_ENU.exe');
  if not FileExists(SqlSetupPath) then
    SqlSetupPath := ExpandConstant('{src}\prerequisites\SQLEXPR_x64_ENU.exe');

  if not FileExists(SqlSetupPath) then
  begin
    // 2. Download from official Microsoft CDN
    DownloadUrl := 'https://download.microsoft.com/download/3/8/d/38de7036-2433-4207-8eae-06e247e17b25/SQLEXPR_x64_ENU.exe';
    TempSqlExe := ExpandConstant('{tmp}\SQLEXPR_x64_ENU.exe');
    LogInstallerMessage('Downloading Microsoft SQL Server 2022 Express from official CDN: ' + DownloadUrl);

    WizardForm.StatusLabel.Caption := 'Downloading Microsoft SQL Server 2022 Express...';
    try
      DownloadTemporaryFile(DownloadUrl, 'SQLEXPR_x64_ENU.exe', '', nil);
      SqlSetupPath := TempSqlExe;
      LogInstallerMessage('SQL Server Express downloaded successfully.');
    except
      LogInstallerMessage('ERROR: Failed to download SQL Server Express from Microsoft CDN: ' + GetExceptionMessage);
      MsgBox('Failed to download Microsoft SQL Server Express installer.' + #13#10 +
             'Please ensure an internet connection is available, or place SQLEXPR_x64_ENU.exe in the installer folder and run setup again.',
             mbError, MB_OK);
      Result := False;
      Exit;
    end;
  end
  else
  begin
    LogInstallerMessage('Using local offline SQL Server Express installer: ' + SqlSetupPath);
  end;

  // 3. Execute silent installation
  WizardForm.StatusLabel.Caption := 'Installing Microsoft SQL Server Express (Instance: CLOVENT)...';
  SqlParams := '/QS /ACTION=Install /FEATURES=SQLEngine /INSTANCENAME=CLOVENT ' +
               '/SQLSVCSTARTUPTYPE=Automatic /SQLSYSADMINACCOUNTS="BUILTIN\Administrators" ' +
               '/TCPENABLED=0 /NPENABLED=1 /IACCEPTSQLSERVERLICENSETERMS';

  LogInstallerMessage('Executing SQL Server Express setup: ' + SqlSetupPath + ' ' + SqlParams);
  ResultCode := -1;
  if not Exec(SqlSetupPath, SqlParams, '', SW_SHOW, ewWaitUntilTerminated, ResultCode) then
  begin
    LogInstallerMessage('ERROR: Failed to launch SQL Server setup executable.');
    Result := False;
    Exit;
  end;

  LogInstallerMessage('SQL Server setup process completed with exit code: ' + IntToStr(ResultCode));
  if (ResultCode <> 0) and (ResultCode <> 3010) then // 3010 = ERROR_SUCCESS_REBOOT_REQUIRED
  begin
    LogInstallerMessage('ERROR: SQL Server Express setup reported failure code: ' + IntToStr(ResultCode));
    MsgBox('Microsoft SQL Server Express installation failed (Exit code: ' + IntToStr(ResultCode) + ').' + #13#10 +
           'Please check Windows Event Log and installer logs for details.', mbError, MB_OK);
    Result := False;
    Exit;
  end;

  // 4. Verify and start service
  Sleep(3000);
  if not EnsureServiceStarted('MSSQL$CLOVENT') then
  begin
    LogInstallerMessage('ERROR: Service MSSQL$CLOVENT could not be started after installation.');
    Result := False;
    Exit;
  end;

  DetectedSqlInstance := 'CLOVENT';
  DetectedSqlServer := '.\CLOVENT';
  LogInstallerMessage('SQL Server Express installed and verified on ' + DetectedSqlServer);
  Result := True;
end;

// Provision database using Clovent.Installer.Provisioner.exe
function ProvisionDatabase(): Boolean;
var
  ProvExe, ProvParams, ProvLog: String;
  ResultCode: Integer;
begin
  WizardForm.StatusLabel.Caption := 'Provisioning database schemas and applying migrations...';
  LogInstallerMessage('Executing production database provisioning service...');

  ProvExe := ExpandConstant('{tmp}\Clovent.Installer.Provisioner.exe');
  ProvLog := ExpandConstant('{commonappdata}\Clovent\BusinessOperatingSystem\Logs\installer-provisioning.log');

  if not FileExists(ProvExe) then
  begin
    LogInstallerMessage('ERROR: Database provisioner executable not found at: ' + ProvExe);
    Result := False;
    Exit;
  end;

  ProvParams := Format('--server "%s" --database "Clovent_BusinessOperatingSystem" --auth windows --provision --log-file "%s"', [DetectedSqlServer, ProvLog]);

  LogInstallerMessage('Running: ' + ProvExe + ' ' + ProvParams);
  ResultCode := -1;
  if not Exec(ProvExe, ProvParams, '', SW_HIDE, ewWaitUntilTerminated, ResultCode) then
  begin
    LogInstallerMessage('ERROR: Failed to launch database provisioner executable.');
    Result := False;
    Exit;
  end;

  LogInstallerMessage('Database provisioner completed with exit code: ' + IntToStr(ResultCode));
  if ResultCode <> 0 then
  begin
    LogInstallerMessage('ERROR: Database provisioning failed with exit code: ' + IntToStr(ResultCode));
    MsgBox('Database provisioning failed (Exit code: ' + IntToStr(ResultCode) + ').' + #13#10 +
           'Details have been logged to: ' + ProvLog, mbError, MB_OK);
    Result := False;
    Exit;
  end;

  LogInstallerMessage('Database provisioning, EF Core migrations, and PaymentMethodSeeder verified successfully.');
  Result := True;
end;

function InitializeSetup(): Boolean;
var
  PrevVersion: String;
begin
  LogInstallerMessage('================================================================================');
  LogInstallerMessage('CLOVENT BUSINESS OPERATING SYSTEM - SETUP INITIALIZING (Version {#MyAppVersion})');
  LogInstallerMessage('OS Version: ' + GetWindowsVersionString);
  LogInstallerMessage('================================================================================');

  // Detect whether CBOS is already installed
  if RegQueryStringValue(HKEY_LOCAL_MACHINE, 'SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{#MyAppId}_is1', 'DisplayVersion', PrevVersion) then
  begin
    IsUpgradeMode := True;
    LogInstallerMessage('Detected existing CBOS installation (Version: ' + PrevVersion + '). Running in UPGRADE/REINSTALL mode.');
  end
  else
  begin
    IsUpgradeMode := False;
    LogInstallerMessage('No previous CBOS installation detected. Running in FRESH INSTALLATION mode.');
  end;

  // Detect SQL Server
  if DetectSqlServer() then
  begin
    SqlNeedsInstall := False;
    LogInstallerMessage('Local SQL Server available: ' + DetectedSqlServer + ' (' + DetectedSqlInstance + ')');
  end
  else
  begin
    SqlNeedsInstall := True;
    LogInstallerMessage('No compatible SQL Server instance found. Setup will install SQL Server Express (CLOVENT).');
  end;

  Result := True;
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssInstall then
  begin
    LogInstallerMessage('Entering installation stage (ssInstall)...');

    // If SQL Server needs to be installed, install it prior to payload deployment
    if SqlNeedsInstall then
    begin
      if not InstallSqlServerExpress() then
      begin
        RaiseException('Setup cannot continue because SQL Server Express installation failed.');
      end;
      SqlNeedsInstall := False;
    end;
  end
  else if CurStep = ssPostInstall then
  begin
    LogInstallerMessage('Payload deployed to application folder. Proceeding with database provisioning...');

    // Run database provisioning, schema migrations, payment method seeding, and security configuration
    if not ProvisionDatabase() then
    begin
      RaiseException('Setup cannot continue because database provisioning failed.');
    end;

    LogInstallerMessage('Setup completed successfully.');
  end;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
  if CurUninstallStep = usPostUninstall then
  begin
    // Policy: Preserve customer SQL database, backups, licenses, and configuration
    LogInstallerMessage('Uninstaller removed application binaries and shortcuts.');
    LogInstallerMessage('Customer database [Clovent_BusinessOperatingSystem] and %ProgramData% configurations remain preserved.');
  end;
end;
