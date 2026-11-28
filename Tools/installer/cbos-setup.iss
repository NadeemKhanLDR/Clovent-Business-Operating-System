; ==============================================================================
; Clovent Business Operating System (CBOS) - Production Installer Script
; Release Target: 1.3.0 (win-x64 Release Candidate Pilot Deployment)
; Technology: Inno Setup 6 (Native 64-bit, elevated, prerequisite-chained)
; ==============================================================================

#define MyAppName "Clovent Business Operating System"
#define MyAppVersion "1.3.0"
#define MyAppPublisher "Clovent"
#define MyAppExeName "Clovent.Desktop.exe"
#define MyAppId "{{C107E47D-CB05-47F1-9BD7-A107CB052026}}"
#define QaAppId "{B0A7CA9A-FULL-47F1-9BD7-A107CB052026}"
#define LegacyQaAppId "{9D4B8E12-CB05-47F1-9BD7-A107CB052026}"

#ifndef PublishDir
  #define PublishDir "..\..\artifacts\release\Clovent.BusinessOperatingSystem-win-x64"
#endif
#ifndef ProvisionerDir
  #define ProvisionerDir "..\Clovent.Installer.Provisioner\bin\publish"
#endif
#ifndef PrerequisitesDir
  #define PrerequisitesDir "..\..\prerequisites"
#endif
#ifndef OutputDir
  #define OutputDir "..\..\artifacts\installer"
#endif
#ifndef OutputBaseFilename
  #define OutputBaseFilename "Clovent.BusinessOperatingSystem-1.3.0-Production-Setup"
#endif
#ifndef EmbedPrerequisites
  #define EmbedPrerequisites 1
#endif
#ifndef SetupIconPath
  #define SetupIconPath "..\..\src\Clovent.Desktop\Resources\cbos.ico"
#endif

[Setup]
AppId={#MyAppId}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\Clovent\Business Operating System
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
OutputDir={#OutputDir}
OutputBaseFilename={#OutputBaseFilename}
SetupIconFile={#SetupIconPath}
UninstallDisplayIcon={app}\Clovent.Desktop.exe,0
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
CloseApplications=no
RestartApplications=no
SetupLogging=yes

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Dirs]
; Protected shared storage with least-privilege NTFS ACLs
; Sensitive stores (Config, License, State): Admins/SYSTEM Full Control, standard Users Read-Only
Name: "{commonappdata}\Clovent\BusinessOperatingSystem"; Permissions: system-full admins-full users-readexec
Name: "{commonappdata}\Clovent\BusinessOperatingSystem\Config"; Permissions: system-full admins-full users-readexec
Name: "{commonappdata}\Clovent\BusinessOperatingSystem\License"; Permissions: system-full admins-full users-readexec
Name: "{commonappdata}\Clovent\BusinessOperatingSystem\State"; Permissions: system-full admins-full users-readexec

; Operational stores (Logs, ContinuityJournal, OperationalCache, CartCheckpoints, PrintJobs, Backups): Admins/SYSTEM Full Control, standard Users Modify
Name: "{commonappdata}\Clovent\BusinessOperatingSystem\Logs"; Permissions: system-full admins-full users-modify
Name: "{commonappdata}\Clovent\BusinessOperatingSystem\ContinuityJournal"; Permissions: system-full admins-full users-modify
Name: "{commonappdata}\Clovent\BusinessOperatingSystem\OperationalCache"; Permissions: system-full admins-full users-modify
Name: "{commonappdata}\Clovent\BusinessOperatingSystem\CartCheckpoints"; Permissions: system-full admins-full users-modify
Name: "{commonappdata}\Clovent\BusinessOperatingSystem\PrintJobs"; Permissions: system-full admins-full users-modify
Name: "{commonappdata}\Clovent\BusinessOperatingSystem\Backups"; Permissions: system-full admins-full users-modify

[Files]
; 1. Main application payload (Self-contained .NET 10 win-x64 release binaries)
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

; 2. Application icon asset
Source: "{#SetupIconPath}"; DestDir: "{app}\Resources"; DestName: "cbos.ico"; Flags: ignoreversion

; 3. Configuration reference templates for administrators
Source: "templates\*"; DestDir: "{app}\templates"; Flags: ignoreversion recursesubdirs createallsubdirs

; 3. Internal silent database provisioner CLI (Single-file win-x64 extracted to {tmp} and deleted on setup completion)
Source: "{#ProvisionerDir}\Clovent.Installer.Provisioner.exe"; DestDir: "{tmp}"; Flags: deleteafterinstall ignoreversion

; 4. Embedded Offline Microsoft Prerequisites (when EmbedPrerequisites is defined)
#ifdef EmbedPrerequisites
Source: "{#PrerequisitesDir}\SQLEXPR_x64_ENU.exe"; DestDir: "{tmp}\prerequisites"; Flags: deleteafterinstall ignoreversion
Source: "{#PrerequisitesDir}\SSMS-Setup-ENU.exe"; DestDir: "{tmp}\prerequisites"; Flags: deleteafterinstall ignoreversion
Source: "{#PrerequisitesDir}\VC_redist.x64.exe"; DestDir: "{tmp}\prerequisites"; Flags: deleteafterinstall ignoreversion
Source: "{#PrerequisitesDir}\VC_redist.x86.exe"; DestDir: "{tmp}\prerequisites"; Flags: deleteafterinstall ignoreversion
Source: "{#PrerequisitesDir}\msoledbsql.msi"; DestDir: "{tmp}\prerequisites"; Flags: deleteafterinstall ignoreversion
#endif

[Icons]
Name: "{autoprograms}\{#MyAppName}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\Resources\cbos.ico"
Name: "{autoprograms}\{#MyAppName}\Support Diagnostics"; Filename: "{app}\{#MyAppExeName}"; Parameters: "--diagnostics"; IconFilename: "{app}\Resources\cbos.ico"
Name: "{autoprograms}\{#MyAppName}\Uninstall {#MyAppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\Resources\cbos.ico"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent unchecked

[Code]
var
  DbConfigPage: TWizardPage;
  RbInstallSql: TRadioButton;
  RbExistingSql: TRadioButton;
  LblServer: TLabel;
  TxtServer: TEdit;
  LblAuth: TLabel;
  CmbAuth: TComboBox;
  LblUser: TLabel;
  TxtUser: TEdit;
  LblPassword: TLabel;
  TxtPassword: TEdit;
  LblDatabase: TLabel;
  TxtDatabase: TEdit;
  ChkInstallSsms: TCheckBox;
  LblRequirementsSummary: TLabel;

  DetectedSqlInstance: String;
  DetectedSqlServer: String;
  DetectedExistingSsms: String;
  IsUpgradeMode: Boolean;
  RequiresReboot: Boolean;

  SelectedSqlServer: String;
  SelectedDatabase: String;
  SelectedAuthMode: String;
  SelectedUser: String;
  SelectedPassword: String;
  ShouldInstallSqlExpress: Boolean;
  ShouldInstallSsms: Boolean;

// Return whether a deferred system reboot is required
function NeedRestart(): Boolean;
begin
  Result := RequiresReboot;
end;

// Helper: Convert boolean to string for logging
function BooleanToStr(B: Boolean): String;
begin
  if B then
    Result := 'True'
  else
    Result := 'False';
end;

// Helper: Append line to installer log file in %ProgramData%\Clovent\BusinessOperatingSystem\Logs
procedure LogInstallerMessage(Msg: String);
var
  LogDir, LogFile, TimeStamp: String;
begin
  LogDir := ExpandConstant('{commonappdata}\Clovent\BusinessOperatingSystem\Logs');
  ForceDirectories(LogDir);
  LogFile := LogDir + '\Setup-{#MyAppVersion}.log';
  TimeStamp := GetDateTimeString('yyyy-mm-dd hh:nn:ss', '-', ':');
  SaveStringToFile(LogFile, '[' + TimeStamp + '] ' + Msg + #13#10, True);
  Log(Msg);
end;

// Helper: Check if a process is currently running via tasklist.exe
function IsProcessRunning(ExeName: String): Boolean;
var
  ResultCode: Integer;
  TempFile: String;
  Output: AnsiString;
begin
  TempFile := ExpandConstant('{tmp}\proccheck_' + ExeName + '.txt');
  ResultCode := -1;
  Exec(ExpandConstant('{cmd}'), '/c tasklist /FI "IMAGENAME eq ' + ExeName + '" /NH > "' + TempFile + '" 2>&1', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  if FileExists(TempFile) then
  begin
    LoadStringFromFile(TempFile, Output);
    DeleteFile(TempFile);
    Result := (Pos(LowerCase(ExeName), LowerCase(String(Output))) > 0);
  end
  else
    Result := False;
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

// Detect existing local SQL Server instances
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

  if DoesServiceExist('MSSQL$CLOVENT') then
  begin
    DetectedSqlInstance := 'CLOVENT';
    DetectedSqlServer := '.\CLOVENT';
    EnsureServiceStarted('MSSQL$CLOVENT');
    Result := True;
    Exit;
  end;

  LogInstallerMessage('No local SQL Server instance detected. Defaulting to .\SQLEXPRESS.');
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
  RequiredSpaceMB := 350; // Base CBOS application + Provisioner

  if RbInstallSql.Checked then
    RequiredSpaceMB := RequiredSpaceMB + 1500;

  if ChkInstallSsms.Checked then
    RequiredSpaceMB := RequiredSpaceMB + 3500;

  SummaryText := Format('Resource Requirements: Disk space ~%d MB | Network: 0 MB (100%% Offline Bundled Media)' + #13#10 + 'Execution: Elevated Administrator Required | Reboot: Handled explicitly without forced restart', [RequiredSpaceMB]);

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
    TxtServer.Text := '.\SQLEXPRESS';
    TxtServer.Enabled := False;
  end;

  UpdateRequirementsSummary();
end;

// Event handler for authentication combo box changes
procedure OnAuthModeChanged(Sender: TObject);
var
  IsSqlAuth: Boolean;
begin
  IsSqlAuth := (CmbAuth.ItemIndex = 1);
  LblUser.Enabled := IsSqlAuth;
  TxtUser.Enabled := IsSqlAuth;
  LblPassword.Enabled := IsSqlAuth;
  TxtPassword.Enabled := IsSqlAuth;
end;

// Create Custom Database & Provisioning Configuration Page
procedure CreateDatabaseConfigWizardPage();
begin
  DbConfigPage := CreateCustomPage(wpSelectDir,
    'Database & Prerequisite Configuration',
    'Configure the production Microsoft SQL Server instance, database, and optional tooling.');

  RbExistingSql := TRadioButton.Create(DbConfigPage);
  RbExistingSql.Parent := DbConfigPage.Surface;
  RbExistingSql.Left := ScaleX(0);
  RbExistingSql.Top := ScaleY(5);
  RbExistingSql.Width := ScaleX(450);
  RbExistingSql.Height := ScaleY(20);
  RbExistingSql.Caption := 'Use an existing Microsoft SQL Server instance (recommended if SQL Server is already present)';
  RbExistingSql.Checked := (DetectedSqlServer <> '') and (DetectedSqlInstance <> '');
  RbExistingSql.OnClick := @OnConfigurationOptionChanged;

  RbInstallSql := TRadioButton.Create(DbConfigPage);
  RbInstallSql.Parent := DbConfigPage.Surface;
  RbInstallSql.Left := ScaleX(0);
  RbInstallSql.Top := ScaleY(30);
  RbInstallSql.Width := ScaleX(450);
  RbInstallSql.Height := ScaleY(20);
  RbInstallSql.Caption := 'Install dedicated Microsoft SQL Server 2022 Express instance (.\SQLEXPRESS)';
  RbInstallSql.Checked := not RbExistingSql.Checked;
  RbInstallSql.OnClick := @OnConfigurationOptionChanged;

  LblServer := TLabel.Create(DbConfigPage);
  LblServer.Parent := DbConfigPage.Surface;
  LblServer.Left := ScaleX(20);
  LblServer.Top := ScaleY(60);
  LblServer.Width := ScaleX(120);
  LblServer.Caption := 'SQL Server Instance:';

  TxtServer := TEdit.Create(DbConfigPage);
  TxtServer.Parent := DbConfigPage.Surface;
  TxtServer.Left := ScaleX(150);
  TxtServer.Top := ScaleY(57);
  TxtServer.Width := ScaleX(260);
  if RbExistingSql.Checked then
    TxtServer.Text := DetectedSqlServer
  else
    TxtServer.Text := '.\SQLEXPRESS';
  TxtServer.Enabled := RbExistingSql.Checked;

  LblAuth := TLabel.Create(DbConfigPage);
  LblAuth.Parent := DbConfigPage.Surface;
  LblAuth.Left := ScaleX(20);
  LblAuth.Top := ScaleY(90);
  LblAuth.Width := ScaleX(120);
  LblAuth.Caption := 'Authentication Mode:';

  CmbAuth := TComboBox.Create(DbConfigPage);
  CmbAuth.Parent := DbConfigPage.Surface;
  CmbAuth.Left := ScaleX(150);
  CmbAuth.Top := ScaleY(87);
  CmbAuth.Width := ScaleX(260);
  CmbAuth.Style := csDropDownList;
  CmbAuth.Items.Add('Windows Integrated Authentication');
  CmbAuth.Items.Add('SQL Server Authentication');
  CmbAuth.ItemIndex := 0;
  CmbAuth.OnChange := @OnAuthModeChanged;

  LblUser := TLabel.Create(DbConfigPage);
  LblUser.Parent := DbConfigPage.Surface;
  LblUser.Left := ScaleX(20);
  LblUser.Top := ScaleY(120);
  LblUser.Width := ScaleX(120);
  LblUser.Caption := 'SQL Login Username:';
  LblUser.Enabled := False;

  TxtUser := TEdit.Create(DbConfigPage);
  TxtUser.Parent := DbConfigPage.Surface;
  TxtUser.Left := ScaleX(150);
  TxtUser.Top := ScaleY(117);
  TxtUser.Width := ScaleX(260);
  TxtUser.Text := 'sa';
  TxtUser.Enabled := False;

  LblPassword := TLabel.Create(DbConfigPage);
  LblPassword.Parent := DbConfigPage.Surface;
  LblPassword.Left := ScaleX(20);
  LblPassword.Top := ScaleY(150);
  LblPassword.Width := ScaleX(120);
  LblPassword.Caption := 'SQL Login Password:';
  LblPassword.Enabled := False;

  TxtPassword := TEdit.Create(DbConfigPage);
  TxtPassword.PasswordChar := '*';
  TxtPassword.Parent := DbConfigPage.Surface;
  TxtPassword.Left := ScaleX(150);
  TxtPassword.Top := ScaleY(147);
  TxtPassword.Width := ScaleX(260);
  TxtPassword.Enabled := False;

  LblDatabase := TLabel.Create(DbConfigPage);
  LblDatabase.Parent := DbConfigPage.Surface;
  LblDatabase.Left := ScaleX(20);
  LblDatabase.Top := ScaleY(180);
  LblDatabase.Width := ScaleX(120);
  LblDatabase.Caption := 'Database Name:';

  TxtDatabase := TEdit.Create(DbConfigPage);
  TxtDatabase.Parent := DbConfigPage.Surface;
  TxtDatabase.Left := ScaleX(150);
  TxtDatabase.Top := ScaleY(177);
  TxtDatabase.Width := ScaleX(260);
  TxtDatabase.Text := 'Clovent_BusinessOperatingSystem';

  ChkInstallSsms := TCheckBox.Create(DbConfigPage);
  ChkInstallSsms.Parent := DbConfigPage.Surface;
  ChkInstallSsms.Left := ScaleX(0);
  ChkInstallSsms.Top := ScaleY(210);
  ChkInstallSsms.Width := ScaleX(450);
  ChkInstallSsms.Height := ScaleY(20);
  if DetectedExistingSsms <> '' then
  begin
    ChkInstallSsms.Caption := 'Install Microsoft SQL Server Management Studio 20.2 (Already installed)';
    ChkInstallSsms.Checked := False;
    ChkInstallSsms.Enabled := True;
  end
  else
  begin
    ChkInstallSsms.Caption := 'Install Microsoft SQL Server Management Studio 20.2 (SSMS - Optional Tooling)';
    ChkInstallSsms.Checked := False;
  end;
  ChkInstallSsms.OnClick := @OnConfigurationOptionChanged;

  LblRequirementsSummary := TLabel.Create(DbConfigPage);
  LblRequirementsSummary.Parent := DbConfigPage.Surface;
  LblRequirementsSummary.Left := ScaleX(0);
  LblRequirementsSummary.Top := ScaleY(240);
  LblRequirementsSummary.Width := ScaleX(450);
  LblRequirementsSummary.Height := ScaleY(60);
  LblRequirementsSummary.Font.Style := [fsBold];

  UpdateRequirementsSummary();
end;

// Resolve path to prerequisite file, checking {src}\prerequisites, {src}, {tmp}\prerequisites, and local cache
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

// Install Visual C++ 2015-2022 Redistributable (x64 and x86)
function InstallVcRedist(): Boolean;
var
  VcExe, VcParams, LogDir, LogFile: String;
  ResultCode: Integer;
begin
  Result := True;
  LogDir := ExpandConstant('{commonappdata}\Clovent\BusinessOperatingSystem\Logs');
  ForceDirectories(LogDir);

  if not IsVcRedistInstalled() then
  begin
    LogInstallerMessage('Visual C++ 2015-2022 x64 runtime not detected. Initiating offline installation...');
    VcExe := ResolvePrerequisitePath('VC_redist.x64.exe');
    if VcExe <> '' then
    begin
      WizardForm.StatusLabel.Caption := 'Installing Visual C++ 2015-2022 Redistributable (x64)...';
      LogFile := LogDir + '\vcredist_x64.log';
      VcParams := '/install /quiet /norestart /log "' + LogFile + '"';
      LogInstallerMessage('Executing: ' + VcExe + ' ' + VcParams);
      ResultCode := -1;
      if Exec(VcExe, VcParams, '', SW_SHOW, ewWaitUntilTerminated, ResultCode) then
      begin
        LogInstallerMessage('VC++ x64 installer exit code: ' + IntToStr(ResultCode));
        if ResultCode = 3010 then
        begin
          LogInstallerMessage('VC++ x64 requested deferred reboot (Exit code: 3010).');
          RequiresReboot := True;
        end
        else if (ResultCode <> 0) and (ResultCode <> 1638) then
        begin
          LogInstallerMessage('WARNING: VC++ x64 installer returned code ' + IntToStr(ResultCode));
        end;
      end
      else
        LogInstallerMessage('ERROR: Failed to launch VC++ x64 installer.');
    end
    else
      LogInstallerMessage('WARNING: VC_redist.x64.exe not found in bundle.');
  end
  else
    LogInstallerMessage('Visual C++ 2015-2022 x64 runtime is already installed.');

  // Also check x86 for 32-bit tooling compatibility
  VcExe := ResolvePrerequisitePath('VC_redist.x86.exe');
  if (VcExe <> '') and not IsVcRedistInstalled() then
  begin
    LogFile := LogDir + '\vcredist_x86.log';
    VcParams := '/install /quiet /norestart /log "' + LogFile + '"';
    Exec(VcExe, VcParams, '', SW_SHOW, ewWaitUntilTerminated, ResultCode);
    if ResultCode = 3010 then
      RequiresReboot := True;
  end;
end;

// Install Microsoft OLE DB Driver 19 for SQL Server
function InstallMsoledbsql(): Boolean;
var
  MsiFile, MsiParams, LogDir, LogFile: String;
  ResultCode: Integer;
begin
  Result := True;
  if not IsMsoledbsqlInstalled() then
  begin
    LogInstallerMessage('Microsoft OLE DB Driver for SQL Server not detected. Initiating offline installation...');
    MsiFile := ResolvePrerequisitePath('msoledbsql.msi');
    if MsiFile <> '' then
    begin
      WizardForm.StatusLabel.Caption := 'Installing Microsoft OLE DB Driver 19 for SQL Server...';
      LogDir := ExpandConstant('{commonappdata}\Clovent\BusinessOperatingSystem\Logs');
      ForceDirectories(LogDir);
      LogFile := LogDir + '\oledb19.log';
      MsiParams := '/i "' + MsiFile + '" /qn /norestart /L*v "' + LogFile + '" IACCEPTMSOLEDBSQLLICENSETERMS=YES';
      LogInstallerMessage('Executing: msiexec.exe ' + MsiParams);
      ResultCode := -1;
      if Exec('msiexec.exe', MsiParams, '', SW_HIDE, ewWaitUntilTerminated, ResultCode) then
      begin
        LogInstallerMessage('msiexec exit code for OLE DB Driver: ' + IntToStr(ResultCode));
        if ResultCode = 3010 then
        begin
          LogInstallerMessage('OLE DB Driver requested deferred reboot (Exit code: 3010).');
          RequiresReboot := True;
        end
        else if (ResultCode <> 0) and (ResultCode <> 1638) then
        begin
          LogInstallerMessage('WARNING: OLE DB Driver installer returned code ' + IntToStr(ResultCode));
        end;
      end
      else
        LogInstallerMessage('ERROR: Failed to launch msiexec for OLE DB Driver.');
    end
    else
      LogInstallerMessage('WARNING: msoledbsql.msi not found in bundle.');
  end
  else
    LogInstallerMessage('Microsoft OLE DB Driver for SQL Server is already installed.');
end;

// Install SQL Server 2022 Express silently
function InstallSqlServerExpress(): Boolean;
var
  SqlSetupPath, SqlParams, LogDir, LogFile: String;
  ResultCode: Integer;
begin
  LogInstallerMessage('Initiating Microsoft SQL Server 2022 Express installation...');

  SqlSetupPath := ResolvePrerequisitePath('SQLEXPR_x64_ENU.exe');
  if SqlSetupPath = '' then
  begin
    LogInstallerMessage('ERROR: SQLEXPR_x64_ENU.exe was not found in prerequisites bundle.');
    MsgBox('Microsoft SQL Server 2022 Express setup media was not found.' + #13#10 +
           'Please verify that SQLEXPR_x64_ENU.exe is present in the prerequisites directory or embedded payload.', mbError, MB_OK);
    Result := False;
    Exit;
  end;

  LogDir := ExpandConstant('{commonappdata}\Clovent\BusinessOperatingSystem\Logs');
  ForceDirectories(LogDir);
  LogFile := LogDir + '\sqlexpress_install.log';

  LogInstallerMessage('Using offline SQL Server Express installer: ' + SqlSetupPath);
  WizardForm.StatusLabel.Caption := 'Installing Microsoft SQL Server 2022 Express (Instance: SQLEXPRESS)...';

  SqlParams := '/QS /ACTION=Install /FEATURES=SQLEngine /INSTANCENAME=SQLEXPRESS ' +
               '/SQLSVCSTARTUPTYPE=Automatic /SQLSYSADMINACCOUNTS="BUILTIN\Administrators" ' +
               '/TCPENABLED=0 /NPENABLED=1 /IACCEPTSQLSERVERLICENSETERMS';

  LogInstallerMessage('Executing SQL Server setup: ' + SqlSetupPath + ' ' + SqlParams);
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
  if not EnsureServiceStarted('MSSQL$SQLEXPRESS') then
  begin
    LogInstallerMessage('ERROR: Service MSSQL$SQLEXPRESS could not be started after installation.');
    Result := False;
    Exit;
  end;

  SelectedSqlServer := '.\SQLEXPRESS';
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

  LogDir := ExpandConstant('{commonappdata}\Clovent\BusinessOperatingSystem\Logs');
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

// Provision production database using Clovent.Installer.Provisioner.exe
function ProvisionDatabase(): Boolean;
var
  ProvDir, ProvExe, ProvParams, ProvLog, FailureReason, AuthCreds: String;
  ResultCode: Integer;
begin
  WizardForm.StatusLabel.Caption := 'Provisioning database schemas, applying migrations, and configuring security...';
  LogInstallerMessage('Executing production database provisioning service...');

  ProvDir := ExpandConstant('{tmp}');
  ProvExe := ProvDir + '\Clovent.Installer.Provisioner.exe';
  ProvLog := ExpandConstant('{commonappdata}\Clovent\BusinessOperatingSystem\Logs\installer-provisioning.log');

  LogInstallerMessage('Provisioner executable: ' + ProvExe);
  LogInstallerMessage('Provisioner working directory: ' + ProvDir);
  LogInstallerMessage('Provisioner target server: ' + SelectedSqlServer);
  LogInstallerMessage('Provisioner target database: ' + SelectedDatabase);
  LogInstallerMessage('Provisioner auth mode: ' + SelectedAuthMode);
  LogInstallerMessage('Provisioner log file: ' + ProvLog);

  if not FileExists(ProvExe) then
  begin
    FailureReason := 'Database provisioner executable was not found at: ' + ProvExe;
    LogInstallerMessage('ERROR: ' + FailureReason);
    MsgBox('CBOS database provisioning component could not start.' + #13#10 + FailureReason, mbError, MB_OK);
    Result := False;
    Exit;
  end;

  if SelectedAuthMode = 'sql' then
    AuthCreds := Format('--user "%s" --password "%s"', [SelectedUser, SelectedPassword])
  else
    AuthCreds := '';

  if IsUpgradeMode then
    ProvParams := Format('--server "%s" --database "%s" --auth %s %s --backup-before-migration --log-file "%s" --upgrade', [SelectedSqlServer, SelectedDatabase, SelectedAuthMode, AuthCreds, ProvLog])
  else
    ProvParams := Format('--server "%s" --database "%s" --auth %s %s --backup-before-migration --log-file "%s" --provision', [SelectedSqlServer, SelectedDatabase, SelectedAuthMode, AuthCreds, ProvLog]);

  LogInstallerMessage('Running database provisioner with verified pre-migration backup enabled.');
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
        FailureReason := 'CBOS database provisioning component could not start due to a missing .NET host or runtime dependency (Exit code: ' + IntToStr(ResultCode) + ').';
      1:
        FailureReason := 'Could not establish connection to SQL Server instance ''' + SelectedSqlServer + ''' (Exit code: 1).';
      2:
        FailureReason := 'Database creation failed for ''' + SelectedDatabase + ''' (Exit code: 2).';
      3:
        FailureReason := 'Database schema migrations or payment method seeding failed (Exit code: 3).';
      4:
        FailureReason := 'Database schema compatibility validation failed or incompatible downgrade detected (Exit code: 4).';
      5:
        FailureReason := 'Pre-upgrade or pre-migration database backup failed; schema was not modified (Exit code: 5).';
      64:
        FailureReason := 'Invalid provisioner command-line arguments (Exit code: 64).';
      99:
        FailureReason := 'Fatal unhandled exception occurred during database provisioning (Exit code: 99).';
    else
      FailureReason := 'Database provisioning failed (Exit code: ' + IntToStr(ResultCode) + ').';
    end;

    LogInstallerMessage('ERROR: ' + FailureReason);

    if FileExists(ProvLog) then
    begin
      MsgBox(FailureReason + #13#10 + 'Details have been logged to:' + #13#10 + ProvLog, mbError, MB_OK);
    end
    else
    begin
      MsgBox(FailureReason + #13#10 + 'The provisioning component terminated before creating the log file at:' + #13#10 + ProvLog, mbError, MB_OK);
    end;

    Result := False;
    Exit;
  end;

  LogInstallerMessage('Database provisioning, verified backup, EF Core migrations, and PaymentMethodSeeder verified successfully.');
  Result := True;
end;

// Validation on Next button click
function NextButtonClick(CurPageID: Integer): Boolean;
var
  ConfirmMsg: String;
  SelectedDir: String;
begin
  Result := True;

  if CurPageID = wpSelectDir then
  begin
    SelectedDir := WizardDirValue();
    LogInstallerMessage('Destination directory selected: ' + SelectedDir);

    // Collision guard: strictly prevent installing into QA or Acceptance directories
    if (Pos('Acceptance', SelectedDir) > 0) or (Pos('CBOS_QA', SelectedDir) > 0) or (Pos('QA', SelectedDir) > 0) then
    begin
      MsgBox('The selected installation directory (' + SelectedDir + ') is reserved for QA / Acceptance testing.' + #13#10#13#10 +
             'Please select the standard production directory:' + #13#10 +
             ExpandConstant('{autopf}\Clovent\Business Operating System'),
             mbError, MB_OK);
      Result := False;
      Exit;
    end;
  end
  else if CurPageID = DbConfigPage.ID then
  begin
    ShouldInstallSqlExpress := RbInstallSql.Checked;
    ShouldInstallSsms := ChkInstallSsms.Checked;

    if RbExistingSql.Checked then
      SelectedSqlServer := Trim(TxtServer.Text)
    else
      SelectedSqlServer := '.\SQLEXPRESS';

    SelectedDatabase := Trim(TxtDatabase.Text);

    if SelectedDatabase = '' then
    begin
      MsgBox('Database name cannot be blank. Please enter a valid database name (e.g. Clovent_BusinessOperatingSystem).', mbError, MB_OK);
      Result := False;
      Exit;
    end;

    if CmbAuth.ItemIndex = 1 then
    begin
      SelectedAuthMode := 'sql';
      SelectedUser := Trim(TxtUser.Text);
      SelectedPassword := TxtPassword.Text;

      if SelectedUser = '' then
      begin
        MsgBox('SQL Server authentication requires a valid username.', mbError, MB_OK);
        Result := False;
        Exit;
      end;
    end
    else
    begin
      SelectedAuthMode := 'windows';
      SelectedUser := '';
      SelectedPassword := '';
    end;

    LogInstallerMessage('Operator configured database settings:');
    LogInstallerMessage('  Install SQL Server Express: ' + BooleanToStr(ShouldInstallSqlExpress));
    LogInstallerMessage('  Target SQL Server: ' + SelectedSqlServer);
    LogInstallerMessage('  Authentication: ' + SelectedAuthMode);
    LogInstallerMessage('  Target Database: ' + SelectedDatabase);
    LogInstallerMessage('  Install SSMS: ' + BooleanToStr(ShouldInstallSsms));
  end
  else if CurPageID = wpReady then
  begin
    // Explicit confirmation before executing database creation or migrations
    ConfirmMsg := 'Ready to proceed with Clovent Business Operating System installation and database provisioning.' + #13#10#13#10 +
                  'Target SQL Server: ' + SelectedSqlServer + #13#10 +
                  'Target Database:   ' + SelectedDatabase + #13#10 +
                  'Authentication:    ' + SelectedAuthMode + #13#10#13#10 +
                  'Actions to be performed:' + #13#10 +
                  '- Deploy self-contained Release application payload' + #13#10;

    if ShouldInstallSqlExpress then
      ConfirmMsg := ConfirmMsg + '- Install Microsoft SQL Server 2022 Express (Instance: SQLEXPRESS)' + #13#10;

    ConfirmMsg := ConfirmMsg + '- Provision physical database if missing' + #13#10 +
                  '- Create verified pre-migration backup (COPY_ONLY) if existing database' + #13#10 +
                  '- Apply EF Core migrations and initialize payment methods' + #13#10;

    if ShouldInstallSsms then
      ConfirmMsg := ConfirmMsg + '- Install Microsoft SQL Server Management Studio 20.2 (SSMS)' + #13#10;

    ConfirmMsg := ConfirmMsg + #13#10 + 'Do you want to continue?';

    if MsgBox(ConfirmMsg, mbConfirmation, MB_YESNO) <> IDYES then
    begin
      LogInstallerMessage('Operator cancelled setup at explicit confirmation prompt.');
      Result := False;
      Exit;
    end;
  end;
end;

// Update Ready page summary text
function UpdateReadyMemo(Space, NewLine, MemoUserInfoInfo, MemoDirInfo, MemoTypeInfo, MemoComponentsInfo, MemoGroupInfo, MemoTasksInfo: String): String;
var
  S: String;
begin
  S := '';
  S := S + 'Installation Destination:' + NewLine;
  S := S + Space + ExpandConstant('{app}') + NewLine + NewLine;

  S := S + 'Database & Prerequisite Configuration:' + NewLine;
  if ShouldInstallSqlExpress then
    S := S + Space + 'SQL Server: Install dedicated Microsoft SQL Server 2022 Express (.\SQLEXPRESS)' + NewLine
  else
    S := S + Space + 'SQL Server: Use existing instance (' + SelectedSqlServer + ')' + NewLine;

  S := S + Space + 'Authentication: ' + SelectedAuthMode + NewLine;
  S := S + Space + 'Target Database: ' + SelectedDatabase + NewLine;
  S := S + Space + 'Pre-Migration Backup: Verified COPY_ONLY backup enabled' + NewLine;

  if ShouldInstallSsms then
    S := S + Space + 'Tooling: Install Microsoft SQL Server Management Studio 20.2 (SSMS)' + NewLine;

  S := S + Space + 'Offline Status: 100% Offline (0 MB network download required)' + NewLine;

  Result := S;
end;

// Initialize Setup
function InitializeSetup(): Boolean;
var
  PrevVersion: String;
begin
  RequiresReboot := False;

  LogInstallerMessage('================================================================================');
  LogInstallerMessage('CLOVENT BUSINESS OPERATING SYSTEM - PRODUCTION SETUP INITIALIZING (v{#MyAppVersion})');
  LogInstallerMessage('OS Version: ' + GetWindowsVersionString);
  LogInstallerMessage('================================================================================');

  // Safety check 1: Never silently close operational CBOS process
  if IsProcessRunning('Clovent.Desktop.exe') then
  begin
    MsgBox('Clovent Business Operating System is currently running on this computer.' + #13#10#13#10 +
           'Please save your work and close the application before proceeding with installation.',
           mbError, MB_OK);
    LogInstallerMessage('Setup aborted: Operational CBOS process is currently running.');
    Result := False;
    Exit;
  end;

  // Safety check 2: Prevent incompatible downgrades
  if RegQueryStringValue(HKEY_LOCAL_MACHINE, 'SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{C107E47D-CB05-47F1-9BD7-A107CB052026}}_is1', 'DisplayVersion', PrevVersion) or
     RegQueryStringValue(HKEY_LOCAL_MACHINE, 'SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{C107E47D-CB05-47F1-9BD7-A107CB052026}_is1', 'DisplayVersion', PrevVersion) then
  begin
    IsUpgradeMode := True;
    LogInstallerMessage('Detected existing CBOS installation (Version: ' + PrevVersion + '). Running in UPGRADE mode.');

    if CompareText(PrevVersion, '{#MyAppVersion}') > 0 then
    begin
      MsgBox('A newer version of Clovent Business Operating System (' + PrevVersion + ') is already installed.' + #13#10#13#10 +
             'Downgrading to version {#MyAppVersion} is not supported.', mbError, MB_OK);
      LogInstallerMessage('Setup aborted: Downgrade from ' + PrevVersion + ' to {#MyAppVersion} rejected.');
      Result := False;
      Exit;
    end;
  end
  else
  begin
    IsUpgradeMode := False;
    LogInstallerMessage('No previous CBOS installation detected. Running in FRESH INSTALLATION mode.');
  end;

  // Component detections
  DetectLocalSqlServer();
  DetectExistingSsms(DetectedExistingSsms);

  Result := True;
end;

// Create custom wizard pages
procedure InitializeWizard();
begin
  CreateDatabaseConfigWizardPage();
end;

// Main installation phases
procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssInstall then
  begin
    LogInstallerMessage('Entering pre-payload installation stage (ssInstall)...');

    // 1. Install Visual C++ redistributable runtimes
    InstallVcRedist();

    // 2. Install Microsoft OLE DB Driver 19
    InstallMsoledbsql();

    // 3. Install SQL Server Express if selected
    if ShouldInstallSqlExpress then
    begin
      if not InstallSqlServerExpress() then
      begin
        RaiseException('Setup cannot continue because Microsoft SQL Server Express installation failed.');
      end;
    end;
  end
  else if CurStep = ssPostInstall then
  begin
    LogInstallerMessage('Payload deployed to application folder. Proceeding with database provisioning and tooling...');

    // 4. Run database provisioning, EF Core migrations, verified backup, payment methods, and DPAPI persistence
    if not ProvisionDatabase() then
    begin
      RaiseException('Setup cannot continue because database provisioning failed.');
    end;

    // 5. Install SSMS if selected
    if ShouldInstallSsms then
    begin
      InstallSsms();
    end;

    LogInstallerMessage('Production setup completed successfully.');
  end;
end;

// Uninstallation lifecycle handling
procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
  if CurUninstallStep = usPostUninstall then
  begin
    // Policy: Strictly preserve customer SQL database, backups, licenses, and configuration
    LogInstallerMessage('Uninstaller removed application binaries and shortcuts.');
    LogInstallerMessage('Customer database [' + SelectedDatabase + '], backups, licenses, and %ProgramData% configurations remain preserved.');
  end;
end;
