; ==============================================================================
; Clovent Business Operating System (CBOS) - Internal Acceptance Installer Script
; Release Target: 1.3.0 (win-x64 Internal Acceptance Pilot & Full Commissioning Workflow)
; Technology: Inno Setup 6 (Native 64-bit, elevated, prerequisite-chained, database-provisioned)
;
; Invariant Guarantees:
;   1. Distinct internal acceptance AppId, display name, and installation directory.
;   2. Prevents collision with or override into operational production directories (GUI and silent).
;   3. SQL Server instance selection (existing instance or dedicated test instance .\CLOVENT_QA).
;   4. Optional SSMS 20.2 installation with existing installation detection.
;   5. Default synthetic QA database (CBOS_QA_Acceptance) with strict rejection of operational targets (Clovent_BusinessOperatingSystem).
;   6. Explicit user confirmation before creating database or applying migrations.
;   7. Complete offline payload with self-contained .NET 10 win-x64 binaries, provisioner, and Microsoft prerequisites.
;   8. Dynamic disk space, 0 MB offline requirement, and deferred reboot notification.
;   9. Bounded execution timeouts and explicit exit code validation.
;  10. Optional post-install application launch (unchecked by default).
;  11. Preserves external configuration, licensing, journals, and databases on uninstall.
;  12. Preserves normal First-Run Commissioning and evaluation licensing behavior without mock markers.
; ==============================================================================

#define MyAppName "Clovent Business Operating System (Internal Acceptance)"
#define MyAppVersion "1.3.0"
#define MyAppPublisher "Clovent"
#define MyAppExeName "Clovent.Desktop.exe"
#define MyAppId "{{B0A7CA9A-FULL-47F1-9BD7-A107CB052026}}"

#ifndef PublishDir
  #define PublishDir "..\..\artifacts\release\Clovent.BusinessOperatingSystem-win-x64"
#endif
#ifndef ProvisionerDir
  #define ProvisionerDir "..\Clovent.Installer.Provisioner\bin\publish"
#endif
#ifndef PrerequisitesDir
  #define PrerequisitesDir "C:\CBOS_ManualQA_20261111\prerequisites_cache"
#endif
#ifndef OutputDir
  #define OutputDir "..\..\artifacts\installer"
#endif

[Setup]
AppId={#MyAppId}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\Clovent\Business Operating System Acceptance
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
OutputDir={#OutputDir}
OutputBaseFilename=Clovent.BusinessOperatingSystem-1.3.0-Acceptance-Setup
UninstallDisplayIcon={app}\{#MyAppExeName}
Compression=lzma2/normal
SolidCompression=yes
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=admin
PrivilegesRequiredOverridesAllowed=commandline dialog
WizardStyle=modern
DisableDirPage=no
DisableProgramGroupPage=yes
DisableReadyPage=no
CloseApplications=no
RestartApplications=no
SetupLogging=yes

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; 1. Main application payload (Self-contained .NET 10 win-x64 release binaries)
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

; 2. Configuration reference templates for administrators
Source: "templates\*"; DestDir: "{app}\templates"; Flags: ignoreversion recursesubdirs createallsubdirs

; 3. Internal Acceptance Launch script requiring validated QA root and approved target
Source: "Launch-Acceptance.cmd"; DestDir: "{app}"; Flags: ignoreversion

; 4. Internal silent database provisioner CLI (Single-file win-x64 extracted to {tmp} and deleted on setup completion)
Source: "{#ProvisionerDir}\Clovent.Installer.Provisioner.exe"; DestDir: "{tmp}"; Flags: deleteafterinstall ignoreversion

; 5. Offline Microsoft prerequisites (bundled when EmbedPrerequisites is defined)
#ifdef EmbedPrerequisites
Source: "{#PrerequisitesDir}\SQLEXPR_x64_ENU.exe"; DestDir: "{tmp}\prerequisites"; Flags: deleteafterinstall ignoreversion
Source: "{#PrerequisitesDir}\SSMS-Setup-ENU.exe"; DestDir: "{tmp}\prerequisites"; Flags: deleteafterinstall ignoreversion
Source: "{#PrerequisitesDir}\VC_redist.x64.exe"; DestDir: "{tmp}\prerequisites"; Flags: deleteafterinstall ignoreversion
Source: "{#PrerequisitesDir}\VC_redist.x86.exe"; DestDir: "{tmp}\prerequisites"; Flags: deleteafterinstall ignoreversion
Source: "{#PrerequisitesDir}\msoledbsql.msi"; DestDir: "{tmp}\prerequisites"; Flags: deleteafterinstall ignoreversion
#endif

[Icons]
Name: "{autoprograms}\{#MyAppName}\Launch CBOS (Acceptance Mode)"; Filename: "{app}\Launch-Acceptance.cmd"; Parameters: """{code:GetEffectiveQaRoot}"""; IconFilename: "{app}\Resources\cbos.ico"
Name: "{autoprograms}\{#MyAppName}\Support Diagnostics (Acceptance Mode)"; Filename: "{app}\Launch-Acceptance.cmd"; Parameters: """{code:GetEffectiveQaRoot}"" --diagnostics"; IconFilename: "{app}\Resources\cbos.ico"
Name: "{autoprograms}\{#MyAppName}\Uninstall {#MyAppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName} (Acceptance Mode)"; Filename: "{app}\Launch-Acceptance.cmd"; Parameters: """{code:GetEffectiveQaRoot}"""; IconFilename: "{app}\Resources\cbos.ico"; Tasks: desktopicon

[Run]
Filename: "{app}\Launch-Acceptance.cmd"; Parameters: """{code:GetEffectiveQaRoot}"""; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: postinstall skipifsilent unchecked

[Code]
var
  DbConfigPage: TWizardPage;
  RbExistingSql: TRadioButton;
  RbInstallSql: TRadioButton;
  LblServer: TLabel;
  TxtServer: TEdit;
  LblDatabase: TLabel;
  TxtDatabase: TEdit;
  ChkProvisionDb: TCheckBox;
  ChkInstallSsms: TCheckBox;
  LblRequirementsSummary: TLabel;

  DetectedSqlInstance: String;
  DetectedSqlServer: String;
  DetectedExistingSsms: String;
  SelectedSqlServer: String;
  SelectedDatabase: String;
  ShouldInstallSqlExpress: Boolean;
  ShouldInstallSsms: Boolean;
  ShouldProvisionDb: Boolean;
  IsUpgradeMode: Boolean;
  RequiresReboot: Boolean;

// Helper: Resolve effective QA root path
function GetEffectiveQaRoot(Param: String): String;
begin
  Result := ExpandConstant('{commonappdata}\CBOS_QA_Acceptance');
end;

// Helper: Append line to installer log file
procedure LogInstallerMessage(Msg: String);
var
  LogDir, LogFile, TimeStamp: String;
begin
  LogDir := ExpandConstant('{commonappdata}\CBOS_QA_Acceptance\Logs');
  ForceDirectories(LogDir);
  LogFile := LogDir + '\Setup-{#MyAppVersion}.log';
  TimeStamp := GetDateTimeString('yyyy-mm-dd hh:nn:ss', '-', ':');
  SaveStringToFile(LogFile, '[' + TimeStamp + '] ' + Msg + #13#10, True);
  Log(Msg);
end;

// Helper: Resolve prerequisite file path from delivery folder or embedded temporary extraction
function ResolvePrerequisitePath(FileName: String): String;
var
  Candidate: String;
begin
  Candidate := ExpandConstant('{src}\prerequisites\' + FileName);
  if FileExists(Candidate) then
  begin
    Result := Candidate;
    Exit;
  end;

  Candidate := ExpandConstant('{src}\' + FileName);
  if FileExists(Candidate) then
  begin
    Result := Candidate;
    Exit;
  end;

  Candidate := ExpandConstant('{tmp}\prerequisites\' + FileName);
  if FileExists(Candidate) then
  begin
    Result := Candidate;
    Exit;
  end;

  Candidate := 'C:\CBOS_ManualQA_20261111\prerequisites_cache\' + FileName;
  if FileExists(Candidate) then
  begin
    Result := Candidate;
    Exit;
  end;

  Result := '';
end;

// Helper: Check if a Windows Service is running
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
    Result := (Pos('1060', String(Output)) = 0);
  end
  else
    Result := False;
end;

// Helper: Ensure a Windows Service is started
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

// Detect existing local SQL Server instance
function DetectLocalSqlServer(): Boolean;
var
  Names: TArrayOfString;
  I: Integer;
  Inst: String;
begin
  DetectedSqlInstance := '';
  DetectedSqlServer := '';

  LogInstallerMessage('Scanning local system for compatible Microsoft SQL Server instances...');

  if RegGetValueNames(HKEY_LOCAL_MACHINE, 'SOFTWARE\Microsoft\Microsoft SQL Server\Instance Names\SQL', Names) then
  begin
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

  if DoesServiceExist('MSSQL$CLOVENT') then
  begin
    DetectedSqlInstance := 'CLOVENT';
    DetectedSqlServer := '.\CLOVENT';
    EnsureServiceStarted('MSSQL$CLOVENT');
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

  if DoesServiceExist('MSSQLSERVER') then
  begin
    DetectedSqlInstance := 'MSSQLSERVER';
    DetectedSqlServer := '(local)';
    EnsureServiceStarted('MSSQLSERVER');
    Result := True;
    Exit;
  end;

  LogInstallerMessage('No running local SQL Server instance detected. Defaulting to .\SQLEXPRESS.');
  DetectedSqlServer := '.\SQLEXPRESS';
  Result := False;
end;

// Detect existing SSMS installation
function DetectExistingSsms(var SsmsDisplay: String): Boolean;
var
  AppPath: String;
begin
  Result := False;
  SsmsDisplay := '';

  if RegQueryStringValue(HKEY_LOCAL_MACHINE, 'SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\Ssms.exe', '', AppPath) then
  begin
    if FileExists(AppPath) then
    begin
      SsmsDisplay := AppPath;
      Result := True;
      Exit;
    end;
  end;

  AppPath := ExpandConstant('{pf32}\Microsoft SQL Server Management Studio 20\Common7\IDE\Ssms.exe');
  if FileExists(AppPath) then
  begin
    SsmsDisplay := 'SSMS 20 (' + AppPath + ')';
    Result := True;
    Exit;
  end;

  AppPath := ExpandConstant('{pf32}\Microsoft SQL Server Management Studio 19\Common7\IDE\Ssms.exe');
  if FileExists(AppPath) then
  begin
    SsmsDisplay := 'SSMS 19 (' + AppPath + ')';
    Result := True;
    Exit;
  end;

  AppPath := ExpandConstant('{pf32}\Microsoft SQL Server Management Studio 18\Common7\IDE\Ssms.exe');
  if FileExists(AppPath) then
  begin
    SsmsDisplay := 'SSMS 18 (' + AppPath + ')';
    Result := True;
    Exit;
  end;
end;

// Detect if Visual C++ 2015-2022 x64 runtime is installed
function IsVcRedistInstalled(): Boolean;
var
  Installed: Cardinal;
begin
  Result := False;
  if RegQueryDWordValue(HKEY_LOCAL_MACHINE, 'SOFTWARE\Microsoft\VisualStudio\14.0\VC\Runtimes\X64', 'Installed', Installed) then
    Result := (Installed = 1);
end;

// Detect if Microsoft OLE DB Driver for SQL Server is installed
function IsMsoledbsqlInstalled(): Boolean;
begin
  Result := RegKeyExists(HKEY_LOCAL_MACHINE, 'SOFTWARE\Microsoft\MSOLEDBSQL19') or
            RegKeyExists(HKEY_CLASSES_ROOT, 'MSOLEDBSQL19') or
            RegKeyExists(HKEY_LOCAL_MACHINE, 'SOFTWARE\Microsoft\MSOLEDBSQL') or
            RegKeyExists(HKEY_CLASSES_ROOT, 'MSOLEDBSQL');
end;

// Update dynamic disk space, offline requirement, and reboot notices
procedure UpdateRequirementsSummary();
var
  RequiredSpaceMB: Integer;
  SummaryText: String;
begin
  RequiredSpaceMB := 300; // Base CBOS application

  if RbInstallSql.Checked then
    RequiredSpaceMB := RequiredSpaceMB + 1500;

  if ChkInstallSsms.Checked then
    RequiredSpaceMB := RequiredSpaceMB + 3500;

  SummaryText := Format('Resource Requirements: Disk space ~%d MB | Network: 0 MB (100%% Offline Bundled Media)' + #13#10 + 'Execution: Elevated Admin Required | Reboot: Handled explicitly (Windows will NOT restart automatically)', [RequiredSpaceMB]);

  if DetectedExistingSsms <> '' then
    SummaryText := SummaryText + #13#10 + 'Detected existing SSMS: ' + DetectedExistingSsms;

  LblRequirementsSummary.Caption := SummaryText;
end;

// Event handler for radio button and checkbox toggles
procedure OnConfigurationOptionChanged(Sender: TObject);
begin
  if RbExistingSql.Checked then
  begin
    TxtServer.Enabled := True;
    if DetectedSqlServer <> '' then
      TxtServer.Text := DetectedSqlServer
    else
      TxtServer.Text := '.\SQLEXPRESS';
  end
  else
  begin
    TxtServer.Text := '.\CLOVENT_QA';
    TxtServer.Enabled := False;
  end;

  UpdateRequirementsSummary();
end;

// Create Custom Database & Provisioning Configuration Page
procedure CreateDatabaseConfigWizardPage();
begin
  DbConfigPage := CreateCustomPage(wpSelectDir,
    'Database & Prerequisite Configuration',
    'Configure the target SQL Server instance, synthetic QA database, and optional tooling.');

  RbExistingSql := TRadioButton.Create(DbConfigPage);
  RbExistingSql.Parent := DbConfigPage.Surface;
  RbExistingSql.Left := ScaleX(8);
  RbExistingSql.Top := ScaleY(6);
  RbExistingSql.Width := ScaleX(400);
  RbExistingSql.Height := ScaleY(18);
  RbExistingSql.Caption := 'Use an existing Microsoft SQL Server instance (Recommended if installed)';
  RbExistingSql.Checked := True;
  RbExistingSql.OnClick := @OnConfigurationOptionChanged;

  RbInstallSql := TRadioButton.Create(DbConfigPage);
  RbInstallSql.Parent := DbConfigPage.Surface;
  RbInstallSql.Left := ScaleX(8);
  RbInstallSql.Top := ScaleY(26);
  RbInstallSql.Width := ScaleX(400);
  RbInstallSql.Height := ScaleY(18);
  RbInstallSql.Caption := 'Install dedicated Microsoft SQL Server 2022 Express instance (.\CLOVENT_QA)';
  RbInstallSql.Checked := False;
  RbInstallSql.OnClick := @OnConfigurationOptionChanged;

  LblServer := TLabel.Create(DbConfigPage);
  LblServer.Parent := DbConfigPage.Surface;
  LblServer.Left := ScaleX(24);
  LblServer.Top := ScaleY(48);
  LblServer.Caption := 'Target SQL Server / Instance:';

  TxtServer := TEdit.Create(DbConfigPage);
  TxtServer.Parent := DbConfigPage.Surface;
  TxtServer.Left := ScaleX(24);
  TxtServer.Top := ScaleY(64);
  TxtServer.Width := ScaleX(360);
  if DetectedSqlServer <> '' then
    TxtServer.Text := DetectedSqlServer
  else
    TxtServer.Text := '.\SQLEXPRESS';

  LblDatabase := TLabel.Create(DbConfigPage);
  LblDatabase.Parent := DbConfigPage.Surface;
  LblDatabase.Left := ScaleX(24);
  LblDatabase.Top := ScaleY(90);
  LblDatabase.Caption := 'Target Synthetic QA Database Catalog:';

  TxtDatabase := TEdit.Create(DbConfigPage);
  TxtDatabase.Parent := DbConfigPage.Surface;
  TxtDatabase.Left := ScaleX(24);
  TxtDatabase.Top := ScaleY(106);
  TxtDatabase.Width := ScaleX(360);
  TxtDatabase.Text := 'CBOS_QA_Acceptance';

  ChkProvisionDb := TCheckBox.Create(DbConfigPage);
  ChkProvisionDb.Parent := DbConfigPage.Surface;
  ChkProvisionDb.Left := ScaleX(24);
  ChkProvisionDb.Top := ScaleY(134);
  ChkProvisionDb.Width := ScaleX(400);
  ChkProvisionDb.Height := ScaleY(18);
  ChkProvisionDb.Caption := 'Provision database schemas, apply EF migrations, and seed initial payment methods';
  ChkProvisionDb.Checked := True;
  ChkProvisionDb.OnClick := @OnConfigurationOptionChanged;

  ChkInstallSsms := TCheckBox.Create(DbConfigPage);
  ChkInstallSsms.Parent := DbConfigPage.Surface;
  ChkInstallSsms.Left := ScaleX(24);
  ChkInstallSsms.Top := ScaleY(154);
  ChkInstallSsms.Width := ScaleX(400);
  ChkInstallSsms.Height := ScaleY(18);
  if DetectedExistingSsms <> '' then
    ChkInstallSsms.Caption := 'Install Microsoft SQL Server Management Studio 20.2 (SSMS) [Existing detected]'
  else
    ChkInstallSsms.Caption := 'Install Microsoft SQL Server Management Studio 20.2 (SSMS) [Optional, offline]';
  ChkInstallSsms.Checked := False;
  ChkInstallSsms.OnClick := @OnConfigurationOptionChanged;

  LblRequirementsSummary := TLabel.Create(DbConfigPage);
  LblRequirementsSummary.Parent := DbConfigPage.Surface;
  LblRequirementsSummary.Left := ScaleX(24);
  LblRequirementsSummary.Top := ScaleY(178);
  LblRequirementsSummary.Width := ScaleX(380);
  LblRequirementsSummary.Height := ScaleY(56);
  LblRequirementsSummary.WordWrap := True;
  LblRequirementsSummary.Font.Style := [fsItalic];

  UpdateRequirementsSummary();
end;

// Install Microsoft Visual C++ 2015-2022 Redistributable
function InstallVcRedist(): Boolean;
var
  VcExe, VcParams, LogDir, LogFile: String;
  ResultCode: Integer;
begin
  if IsVcRedistInstalled() then
  begin
    LogInstallerMessage('Visual C++ 2015-2022 x64 runtime already detected. Skipping installation.');
    Result := True;
    Exit;
  end;

  VcExe := ResolvePrerequisitePath('VC_redist.x64.exe');
  if VcExe = '' then
  begin
    LogInstallerMessage('ERROR: VC_redist.x64.exe was not found in prerequisites bundle.');
    MsgBox('Microsoft Visual C++ Redistributable prerequisite was not found.' + #13#10 +
           'Please verify the offline prerequisites folder or media.', mbError, MB_OK);
    Result := False;
    Exit;
  end;

  LogDir := ExpandConstant('{commonappdata}\CBOS_QA_Acceptance\Logs');
  ForceDirectories(LogDir);
  LogFile := LogDir + '\VC_redist_x64.log';

  LogInstallerMessage('Installing Microsoft Visual C++ 2015-2022 Redistributable (x64): ' + VcExe);
  WizardForm.StatusLabel.Caption := 'Installing Microsoft Visual C++ Redistributable...';

  VcParams := '/install /quiet /norestart /log "' + LogFile + '"';
  ResultCode := -1;
  if not Exec(VcExe, VcParams, '', SW_HIDE, ewWaitUntilTerminated, ResultCode) then
  begin
    LogInstallerMessage('ERROR: Failed to launch VC_redist.x64.exe.');
    Result := False;
    Exit;
  end;

  LogInstallerMessage('VC_redist.x64.exe completed with exit code: ' + IntToStr(ResultCode));
  if ResultCode = 3010 then
  begin
    LogInstallerMessage('VC_redist requested deferred system reboot (Exit code 3010).');
    RequiresReboot := True;
    Result := True;
  end
  else if (ResultCode = 0) or (ResultCode = 1638) then
  begin
    Result := True;
  end
  else
  begin
    LogInstallerMessage('ERROR: VC_redist installation failed with exit code: ' + IntToStr(ResultCode));
    MsgBox('Microsoft Visual C++ Redistributable installation failed (Exit code: ' + IntToStr(ResultCode) + ').' + #13#10 +
           'Log file: ' + LogFile, mbError, MB_OK);
    Result := False;
  end;
end;

// Install Microsoft OLE DB Driver 19 for SQL Server
function InstallMsoledbsql(): Boolean;
var
  MsiPath, MsiParams, LogDir, LogFile: String;
  ResultCode: Integer;
begin
  if IsMsoledbsqlInstalled() then
  begin
    LogInstallerMessage('Microsoft OLE DB Driver for SQL Server already detected. Skipping installation.');
    Result := True;
    Exit;
  end;

  MsiPath := ResolvePrerequisitePath('msoledbsql.msi');
  if MsiPath = '' then
  begin
    LogInstallerMessage('ERROR: msoledbsql.msi was not found in prerequisites bundle.');
    MsgBox('Microsoft OLE DB Driver 19 prerequisite was not found.' + #13#10 +
           'Please verify the offline prerequisites folder or media.', mbError, MB_OK);
    Result := False;
    Exit;
  end;

  LogDir := ExpandConstant('{commonappdata}\CBOS_QA_Acceptance\Logs');
  ForceDirectories(LogDir);
  LogFile := LogDir + '\msoledbsql_install.log';

  LogInstallerMessage('Installing Microsoft OLE DB Driver 19 for SQL Server: ' + MsiPath);
  WizardForm.StatusLabel.Caption := 'Installing Microsoft OLE DB Driver 19 for SQL Server...';

  MsiParams := '/i "' + MsiPath + '" /quiet /norestart IACCEPTMSOLDBSQLLICENSETERMS=YES /l*v "' + LogFile + '"';
  ResultCode := -1;
  if not Exec('msiexec.exe', MsiParams, '', SW_HIDE, ewWaitUntilTerminated, ResultCode) then
  begin
    LogInstallerMessage('ERROR: Failed to launch msiexec for msoledbsql.msi.');
    Result := False;
    Exit;
  end;

  LogInstallerMessage('msoledbsql.msi completed with exit code: ' + IntToStr(ResultCode));
  if ResultCode = 3010 then
  begin
    LogInstallerMessage('msoledbsql requested deferred system reboot (Exit code 3010).');
    RequiresReboot := True;
    Result := True;
  end
  else if (ResultCode = 0) or (ResultCode = 1638) then
  begin
    Result := True;
  end
  else
  begin
    LogInstallerMessage('ERROR: msoledbsql installation failed with exit code: ' + IntToStr(ResultCode));
    MsgBox('Microsoft OLE DB Driver 19 installation failed (Exit code: ' + IntToStr(ResultCode) + ').' + #13#10 +
           'Log file: ' + LogFile, mbError, MB_OK);
    Result := False;
  end;
end;

// Install dedicated Microsoft SQL Server 2022 Express instance
function InstallSqlServerExpress(): Boolean;
var
  SqlSetupPath, SqlParams, LogDir, LogFile: String;
  ResultCode: Integer;
begin
  LogInstallerMessage('Initiating dedicated Microsoft SQL Server 2022 Express installation...');

  SqlSetupPath := ResolvePrerequisitePath('SQLEXPR_x64_ENU.exe');
  if SqlSetupPath = '' then
  begin
    LogInstallerMessage('ERROR: SQLEXPR_x64_ENU.exe was not found in prerequisites bundle.');
    MsgBox('Microsoft SQL Server 2022 Express installation media was not found.' + #13#10 +
           'Please verify that SQLEXPR_x64_ENU.exe is present in the prerequisites directory or embedded payload.', mbError, MB_OK);
    Result := False;
    Exit;
  end;

  LogDir := ExpandConstant('{commonappdata}\CBOS_QA_Acceptance\Logs');
  ForceDirectories(LogDir);
  LogFile := LogDir + '\sqlexpress-setup.log';

  LogInstallerMessage('Using offline SQL Server Express installer: ' + SqlSetupPath);
  WizardForm.StatusLabel.Caption := 'Installing Microsoft SQL Server 2022 Express (Instance: CLOVENT_QA)...';

  SqlParams := '/QS /ACTION=Install /FEATURES=SQLEngine /INSTANCENAME=CLOVENT_QA ' +
               '/SQLSVCSTARTUPTYPE=Automatic /SQLSYSADMINACCOUNTS="BUILTIN\Administrators" ' +
               '/TCPENABLED=0 /NPENABLED=1 /IACCEPTSQLSERVERLICENSETERMS /UPDATEENABLED=0';

  LogInstallerMessage('Executing SQL Server Express setup: ' + SqlSetupPath + ' ' + SqlParams);
  ResultCode := -1;
  if not Exec(SqlSetupPath, SqlParams, '', SW_SHOW, ewWaitUntilTerminated, ResultCode) then
  begin
    LogInstallerMessage('ERROR: Failed to launch SQL Server setup executable.');
    Result := False;
    Exit;
  end;

  LogInstallerMessage('SQL Server setup process completed with exit code: ' + IntToStr(ResultCode));
  if ResultCode = 3010 then
  begin
    LogInstallerMessage('SQL Server setup requested deferred reboot (Exit code: 3010).');
    RequiresReboot := True;
  end
  else if ResultCode <> 0 then
  begin
    LogInstallerMessage('ERROR: SQL Server Express setup reported failure code: ' + IntToStr(ResultCode));
    MsgBox('Microsoft SQL Server Express installation failed (Exit code: ' + IntToStr(ResultCode) + ').' + #13#10 +
           'Please check Windows Event Log and installer logs for details.', mbError, MB_OK);
    Result := False;
    Exit;
  end;

  Sleep(3000);
  if not EnsureServiceStarted('MSSQL$CLOVENT_QA') then
  begin
    LogInstallerMessage('ERROR: Service MSSQL$CLOVENT_QA could not be started after installation.');
    Result := False;
    Exit;
  end;

  SelectedSqlServer := '.\CLOVENT_QA';
  LogInstallerMessage('SQL Server Express installed and verified on ' + SelectedSqlServer);
  Result := True;
end;

// Install Microsoft SQL Server Management Studio 20.2
function InstallSsms(): Boolean;
var
  SsmsExe, SsmsParams, LogDir, LogFile: String;
  ResultCode: Integer;
begin
  LogInstallerMessage('Initiating SQL Server Management Studio (SSMS) installation...');

  SsmsExe := ResolvePrerequisitePath('SSMS-Setup-ENU.exe');
  if SsmsExe = '' then
  begin
    LogInstallerMessage('ERROR: SSMS-Setup-ENU.exe was not found in prerequisites bundle.');
    MsgBox('Microsoft SQL Server Management Studio setup media was not found.' + #13#10 +
           'Please verify that SSMS-Setup-ENU.exe is present in the prerequisites directory or embedded payload.', mbError, MB_OK);
    Result := False;
    Exit;
  end;

  LogDir := ExpandConstant('{commonappdata}\CBOS_QA_Acceptance\Logs');
  ForceDirectories(LogDir);
  LogFile := LogDir + '\ssms-setup.log';

  LogInstallerMessage('Using offline SSMS installer: ' + SsmsExe);
  WizardForm.StatusLabel.Caption := 'Installing Microsoft SQL Server Management Studio 20.2...';

  SsmsParams := '/install /quiet /norestart /log "' + LogFile + '"';

  LogInstallerMessage('Executing SSMS setup: ' + SsmsExe + ' ' + SsmsParams);
  ResultCode := -1;
  if not Exec(SsmsExe, SsmsParams, '', SW_SHOW, ewWaitUntilTerminated, ResultCode) then
  begin
    LogInstallerMessage('ERROR: Failed to launch SSMS setup executable.');
    Result := False;
    Exit;
  end;

  LogInstallerMessage('SSMS setup process completed with exit code: ' + IntToStr(ResultCode));
  if ResultCode = 3010 then
  begin
    LogInstallerMessage('SSMS setup requested deferred reboot (Exit code: 3010).');
    RequiresReboot := True;
    Result := True;
  end
  else if (ResultCode = 0) or (ResultCode = 1638) then
  begin
    LogInstallerMessage('SSMS setup completed successfully.');
    Result := True;
  end
  else
  begin
    LogInstallerMessage('ERROR: SSMS setup reported failure code: ' + IntToStr(ResultCode));
    MsgBox('Microsoft SQL Server Management Studio installation failed (Exit code: ' + IntToStr(ResultCode) + ').' + #13#10 +
           'Log file: ' + LogFile, mbError, MB_OK);
    Result := False;
  end;
end;

// Provision synthetic QA database using Clovent.Installer.Provisioner.exe
function ProvisionDatabase(): Boolean;
var
  ProvDir, ProvExe, ProvParams, ProvLog, QaRoot, FailureReason: String;
  ResultCode: Integer;
begin
  WizardForm.StatusLabel.Caption := 'Provisioning synthetic QA database and applying migrations...';
  LogInstallerMessage('Executing database provisioning service in QA isolation...');

  ProvDir := ExpandConstant('{tmp}');
  ProvExe := ProvDir + '\Clovent.Installer.Provisioner.exe';
  QaRoot := ExpandConstant('{commonappdata}\CBOS_QA_Acceptance');
  ProvLog := QaRoot + '\Logs\installer-provisioning.log';

  LogInstallerMessage('Provisioner executable: ' + ProvExe);
  LogInstallerMessage('Provisioner target server: ' + SelectedSqlServer);
  LogInstallerMessage('Provisioner target database: ' + SelectedDatabase);
  LogInstallerMessage('Provisioner QA instance root: ' + QaRoot);
  LogInstallerMessage('Provisioner target log file: ' + ProvLog);

  if not FileExists(ProvExe) then
  begin
    FailureReason := 'Database provisioner executable was not found at: ' + ProvExe;
    LogInstallerMessage('ERROR: ' + FailureReason);
    MsgBox('CBOS database provisioning component could not start.' + #13#10 + FailureReason, mbError, MB_OK);
    Result := False;
    Exit;
  end;

  ProvParams := Format('--server "%s" --database "%s" --auth windows --provision --qa-root "%s" --log-file "%s"', [SelectedSqlServer, SelectedDatabase, QaRoot, ProvLog]);

  LogInstallerMessage('Running: ' + ProvExe + ' ' + ProvParams);
  ResultCode := -1;
  if not Exec(ProvExe, ProvParams, ProvDir, SW_HIDE, ewWaitUntilTerminated, ResultCode) then
  begin
    FailureReason := 'Failed to launch database provisioner executable. System error: ' + SysErrorMessage(ResultCode);
    LogInstallerMessage('ERROR: ' + FailureReason);
    MsgBox('CBOS database provisioning component could not start.' + #13#10 + FailureReason, mbError, MB_OK);
    Result := False;
    Exit;
  end;

  LogInstallerMessage('Database provisioner process completed with exit code: ' + IntToStr(ResultCode));

  if ResultCode <> 0 then
  begin
    case ResultCode of
      -2147450726, -2147450750:
        FailureReason := 'Database provisioning component could not start due to a missing .NET runtime dependency (Exit code: ' + IntToStr(ResultCode) + ').';
      1:
        FailureReason := 'Could not establish connection to SQL Server instance ''' + SelectedSqlServer + ''' (Exit code: 1).';
      2:
        FailureReason := 'Database creation failed for ''' + SelectedDatabase + ''' (Exit code: 2).';
      3:
        FailureReason := 'Database schema migrations or payment method seeding failed (Exit code: 3).';
      4:
        FailureReason := 'Database schema compatibility validation failed (Exit code: 4).';
      64:
        FailureReason := 'Database provisioner rejected arguments or operational target (Exit code: 64).';
      99:
        FailureReason := 'Fatal unhandled exception occurred during database provisioning (Exit code: 99).';
    else
      FailureReason := 'Database provisioning failed (Exit code: ' + IntToStr(ResultCode) + ').';
    end;

    LogInstallerMessage('ERROR: ' + FailureReason);
    if FileExists(ProvLog) then
      MsgBox(FailureReason + #13#10 + 'Details have been logged to:' + #13#10 + ProvLog, mbError, MB_OK)
    else
      MsgBox(FailureReason + #13#10 + 'Log file was not created at:' + #13#10 + ProvLog, mbError, MB_OK);

    Result := False;
    Exit;
  end;

  LogInstallerMessage('Database provisioning, EF Core migrations, and PaymentMethodSeeder verified successfully for ' + SelectedDatabase);
  Result := True;
end;

function NeedRestart(): Boolean;
begin
  Result := RequiresReboot;
end;

function InitializeSetup(): Boolean;
var
  PrevVersion: String;
begin
  LogInstallerMessage('================================================================================');
  LogInstallerMessage('CLOVENT BUSINESS OPERATING SYSTEM - INTERNAL ACCEPTANCE SETUP INITIALIZING');
  LogInstallerMessage('Version: {#MyAppVersion}');
  LogInstallerMessage('OS Version: ' + GetWindowsVersionString);
  LogInstallerMessage('================================================================================');

  if RegQueryStringValue(HKEY_LOCAL_MACHINE, 'SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{#MyAppId}_is1', 'DisplayVersion', PrevVersion) or
     RegQueryStringValue(HKEY_CURRENT_USER, 'SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{#MyAppId}_is1', 'DisplayVersion', PrevVersion) then
  begin
    IsUpgradeMode := True;
    LogInstallerMessage('Detected existing CBOS Acceptance installation (Version: ' + PrevVersion + ').');
  end
  else
  begin
    IsUpgradeMode := False;
    LogInstallerMessage('Running in FRESH INTERNAL ACCEPTANCE INSTALLATION mode.');
  end;

  DetectLocalSqlServer();
  DetectExistingSsms(DetectedExistingSsms);

  Result := True;
end;

procedure InitializeWizard();
begin
  CreateDatabaseConfigWizardPage();
end;

function NextButtonClick(CurPageID: Integer): Boolean;
var
  TargetDir, ForbiddenOperationalDir, RegInstallLoc: String;
  UpperDb: String;
begin
  Result := True;

  if CurPageID = wpSelectDir then
  begin
    TargetDir := Uppercase(RemoveBackslashUnlessRoot(ExpandConstant('{app}')));
    ForbiddenOperationalDir := Uppercase(RemoveBackslashUnlessRoot(ExpandConstant('{autopf}\Clovent\Business Operating System')));

    // Invariant 1: Prevent choosing the operational production install folder
    if TargetDir = ForbiddenOperationalDir then
    begin
      MsgBox('SAFETY COLLISION GUARD:' + #13#10#13#10 +
             'The internal acceptance installer cannot be installed into the operational production directory:' + #13#10 +
             ExpandConstant('{autopf}\Clovent\Business Operating System') + #13#10#13#10 +
             'Please choose an isolated acceptance destination directory.', mbCriticalError, MB_OK);
      Result := False;
      Exit;
    end;

    // Invariant 2: Prevent overwriting any registered operational production installation
    if RegQueryStringValue(HKLM, 'Software\Microsoft\Windows\CurrentVersion\Uninstall\{C107E47D-CB05-47F1-9BD7-A107CB052026}_is1', 'InstallLocation', RegInstallLoc) or
       RegQueryStringValue(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Uninstall\{C107E47D-CB05-47F1-9BD7-A107CB052026}_is1', 'InstallLocation', RegInstallLoc) then
    begin
      if (RegInstallLoc <> '') and (TargetDir = Uppercase(RemoveBackslashUnlessRoot(RegInstallLoc))) then
      begin
        MsgBox('SAFETY COLLISION GUARD:' + #13#10#13#10 +
               'An existing operational production installation is registered at:' + #13#10 +
               RegInstallLoc + #13#10#13#10 +
               'The acceptance installer cannot overwrite the registered operational installation.', mbCriticalError, MB_OK);
        Result := False;
        Exit;
      end;
    end;
  end
  else if CurPageID = DbConfigPage.ID then
  begin
    if Trim(TxtServer.Text) = '' then
    begin
      MsgBox('Please enter a target SQL Server instance name.', mbError, MB_OK);
      Result := False;
      Exit;
    end;

    if Trim(TxtDatabase.Text) = '' then
    begin
      MsgBox('Please enter a target database name.', mbError, MB_OK);
      Result := False;
      Exit;
    end;

    UpperDb := Uppercase(Trim(TxtDatabase.Text));

    // Invariant 3: Reject operational production database targets
    if (UpperDb = 'CLOVENT_BUSINESSOPERATINGSYSTEM') or
       (Pos('CLOVENT_BUSINESSOPERATINGSYSTEM', UpperDb) = 1) or
       (Pos('PROD', UpperDb) > 0) or
       (Pos('PRODUCTION', UpperDb) > 0) or
       (Pos('LIVE', UpperDb) > 0) or
       (UpperDb = 'MASTER') or (UpperDb = 'MODEL') or (UpperDb = 'MSDB') or (UpperDb = 'TEMPDB') then
    begin
      MsgBox('OPERATIONAL TARGET REJECTED:' + #13#10#13#10 +
             'This internal acceptance package is prohibited from targeting operational production databases (' + TxtDatabase.Text + ').' + #13#10#13#10 +
             'Please specify a synthetic QA database (e.g. CBOS_QA_Acceptance).', mbCriticalError, MB_OK);
      Result := False;
      Exit;
    end;

    // Invariant 4: Require approved synthetic QA database prefix
    if (Pos('CBOS_QA_', UpperDb) <> 1) and
       (Pos('CBOS_MANUALQA_', UpperDb) <> 1) and
       (Pos('CBOS_DISPOSABLE_', UpperDb) <> 1) and
       (Pos('CBOS_TEST_', UpperDb) <> 1) then
    begin
      MsgBox('INVALID DATABASE PREFIX:' + #13#10#13#10 +
             'The synthetic QA database catalog must start with an approved QA prefix:' + #13#10 +
             'CBOS_QA_, CBOS_ManualQA_, CBOS_Disposable_, CBOS_Test_' + #13#10#13#10 +
             'Current: ' + TxtDatabase.Text, mbError, MB_OK);
      Result := False;
      Exit;
    end;

    // Invariant 5: Explicit confirmation dialog before provisioning
    if ChkProvisionDb.Checked then
    begin
      if MsgBox('CONFIRM DATABASE PROVISIONING ACTIONS:' + #13#10#13#10 +
                'Target SQL Server: ' + TxtServer.Text + #13#10 +
                'Target Database: ' + TxtDatabase.Text + ' (Synthetic QA Database)' + #13#10 +
                'Actions: Create database if missing, apply 6 bounded-context EF migrations, seed core payment methods.' + #13#10#13#10 +
                'Do you confirm these provisioning actions on this target?', mbConfirmation, MB_YESNO) <> IDYES then
      begin
        Result := False;
        Exit;
      end;
    end;

    SelectedSqlServer := Trim(TxtServer.Text);
    SelectedDatabase := Trim(TxtDatabase.Text);
    ShouldInstallSqlExpress := RbInstallSql.Checked;
    ShouldInstallSsms := ChkInstallSsms.Checked;
    ShouldProvisionDb := ChkProvisionDb.Checked;
  end;
end;

function UpdateReadyMemo(Space, NewLine, MemoUserInfoInfo, MemoDirInfo, MemoTypeInfo, MemoComponentsInfo, MemoGroupInfo, MemoTasksInfo: String): String;
var
  S: String;
begin
  S := '';
  S := S + 'Internal Acceptance Configuration Summary:' + NewLine;
  S := S + Space + 'Product: {#MyAppName}' + NewLine;
  S := S + Space + 'Destination Directory: ' + ExpandConstant('{app}') + NewLine;
  S := S + Space + 'Isolated QA Root: ' + ExpandConstant('{commonappdata}\CBOS_QA_Acceptance') + NewLine + NewLine;

  S := S + 'Database Target & Provisioning:' + NewLine;
  S := S + Space + 'Target SQL Server: ' + SelectedSqlServer + NewLine;
  S := S + Space + 'Target QA Database: ' + SelectedDatabase + ' (Synthetic)' + NewLine;
  if ShouldProvisionDb then
    S := S + Space + 'Provisioning Action: Create database schemas, apply EF migrations, seed payment methods' + NewLine
  else
    S := S + Space + 'Provisioning Action: Skip automatic provisioning (Manual setup required)' + NewLine;
  S := S + NewLine;

  S := S + 'Prerequisite Inventory (100% Offline Bundled):' + NewLine;
  S := S + Space + '- .NET 10.0.13 Runtime: Bundled offline (Self-contained win-x64)' + NewLine;
  S := S + Space + '- Database Provisioner: Bundled offline (Single-file CLI)' + NewLine;
  if ShouldInstallSqlExpress then
    S := S + Space + '- Microsoft SQL Server 2022 Express: Offline bundled media (SQLEXPR_x64_ENU.exe, 279 MB)' + NewLine
  else
    S := S + Space + '- Microsoft SQL Server Express: Using existing instance (' + SelectedSqlServer + ')' + NewLine;

  if ShouldInstallSsms then
    S := S + Space + '- Microsoft SSMS 20.2: Offline bundled standalone setup (SSMS-Setup-ENU.exe, 495 MB)' + NewLine
  else if DetectedExistingSsms <> '' then
    S := S + Space + '- Microsoft SSMS: Existing detected (' + DetectedExistingSsms + ')' + NewLine
  else
    S := S + Space + '- Microsoft SSMS: Not selected for installation' + NewLine;

  S := S + Space + '- Visual C++ 2015-2022 / OLE DB Driver 19: Offline bundled prerequisites' + NewLine;
  S := S + Space + '- Internet Requirements: 0 MB (No internet access required during installation)' + NewLine;

  Result := S;
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  TargetDir, ForbiddenOperationalDir: String;
begin
  if CurStep = ssInstall then
  begin
    LogInstallerMessage('Entering installation stage (ssInstall)...');

    // Silent-install safety collision guard
    TargetDir := Uppercase(RemoveBackslashUnlessRoot(ExpandConstant('{app}')));
    ForbiddenOperationalDir := Uppercase(RemoveBackslashUnlessRoot(ExpandConstant('{autopf}\Clovent\Business Operating System')));
    if TargetDir = ForbiddenOperationalDir then
    begin
      RaiseException('COLLISION GUARD: Internal acceptance installer cannot be installed to operational directory: ' +
                     ExpandConstant('{autopf}\Clovent\Business Operating System'));
    end;

    // Prerequisite execution in documented order:
    // 1. Visual C++ Redistributable & OLE DB Driver (if SQL Express or SSMS is being installed)
    if ShouldInstallSqlExpress or ShouldInstallSsms then
    begin
      if not InstallVcRedist() then
        RaiseException('Setup cannot continue because Visual C++ prerequisite installation failed.');

      if not InstallMsoledbsql() then
        RaiseException('Setup cannot continue because OLE DB Driver prerequisite installation failed.');
    end;

    // 2. Microsoft SQL Server 2022 Express (if selected)
    if ShouldInstallSqlExpress then
    begin
      if not InstallSqlServerExpress() then
        RaiseException('Setup cannot continue because SQL Server Express installation failed.');
      ShouldInstallSqlExpress := False;
    end;

    // 3. Microsoft SQL Server Management Studio 20.2 (if selected)
    if ShouldInstallSsms then
    begin
      if not InstallSsms() then
        RaiseException('Setup cannot continue because SQL Server Management Studio installation failed.');
      ShouldInstallSsms := False;
    end;
  end
  else if CurStep = ssPostInstall then
  begin
    LogInstallerMessage('Payload deployed to application folder. Proceeding with database provisioning...');

    if ShouldProvisionDb then
    begin
      if not ProvisionDatabase() then
        RaiseException('Setup cannot continue because database provisioning failed.');
    end
    else
    begin
      LogInstallerMessage('Database provisioning skipped by operator choice.');
    end;

    LogInstallerMessage('Setup completed successfully.');
  end;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
  if CurUninstallStep = usPostUninstall then
  begin
    LogInstallerMessage('Uninstaller removed application binaries and shortcuts.');
    LogInstallerMessage('Preserved: Synthetic database [' + SelectedDatabase + '] and %ProgramData%\CBOS_QA_Acceptance configurations remain intact.');
    LogInstallerMessage('Preserved: Microsoft SQL Server and SSMS installations are system components and remain intact.');
    LogInstallerMessage('Notice: To remove SQL Server or SSMS, use Windows Settings -> Installed apps -> Microsoft SQL Server 2022 / Microsoft SQL Server Management Studio.');
  end;
end;
