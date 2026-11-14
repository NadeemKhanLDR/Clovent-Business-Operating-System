; ==============================================================================
; Clovent Business Operating System (CBOS) - Internal QA Installer Script
; Release Target: 1.3.0 (win-x64 Internal QA Qualification Only)
; Technology: Inno Setup 6 (Native 64-bit, elevated, files-only payload)
; Safety Constraints:
;   - Installs application binaries and config templates only.
;   - NO automatic SQL Server download or installation.
;   - NO automatic database creation, migration, restore, or provisioning.
;   - NO provisioner executable included or invoked.
;   - NO normal-mode shortcuts or automatic launch. QA shortcut requires explicit validated QA root.
;   - NO modification of shared production %ProgramData%\Clovent\BusinessOperatingSystem or its ACLs.
;   - Distinct QA AppId and installation directory to prevent replacing operational installation.
;   - Preserves all business databases and external QA instances on uninstall.
; ==============================================================================

#define MyAppName "Clovent Business Operating System (Internal QA)"
#define MyAppVersion "1.3.0"
#define MyAppPublisher "Clovent"
#define MyAppExeName "Clovent.Desktop.exe"
#define MyAppId "{{B0A7CA9A-QA01-47F1-9BD7-A107CB052026}}"

#ifndef PublishDir
  #define PublishDir "..\..\artifacts\release\Clovent.BusinessOperatingSystem-win-x64"
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
DefaultDirName={autopf}\Clovent\Business Operating System QA
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
OutputDir={#OutputDir}
OutputBaseFilename=Clovent.BusinessOperatingSystem-1.3.0-QA-Setup
UninstallDisplayIcon={app}\{#MyAppExeName}
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
; 1. Main application payload (Self-contained .NET 10 win-x64 release binaries)
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

; 2. Configuration reference templates for administrators
Source: "templates\*"; DestDir: "{app}\templates"; Flags: ignoreversion recursesubdirs createallsubdirs

; 3. Internal QA Launch script requiring validated QA root and approved target
Source: "Launch-QA.cmd"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
; Only QA-isolated launch shortcuts requiring validated QA root and approved target; NO normal-mode shortcuts.
Name: "{autoprograms}\{#MyAppName}\Launch CBOS (QA Mode)"; Filename: "{app}\Launch-QA.cmd"; IconFilename: "{app}\Resources\cbos.ico"
Name: "{autoprograms}\{#MyAppName}\Uninstall {#MyAppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName} (QA Mode)"; Filename: "{app}\Launch-QA.cmd"; IconFilename: "{app}\Resources\cbos.ico"; Tasks: desktopicon

; Explicit safety invariant: Setup contains no auto-run directives.
; CBOS is never launched automatically upon setup completion.

[Code]
function NextButtonClick(CurPageID: Integer): Boolean;
var
  TargetDir: String;
  ForbiddenOperationalDir: String;
  RegInstallLoc: String;
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
             'The internal QA installer cannot be installed into the operational production directory:' + #13#10 +
             ExpandConstant('{autopf}\Clovent\Business Operating System') + #13#10#13#10 +
             'Please choose an isolated QA destination directory.', mbCriticalError, MB_OK);
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
               'The QA variant cannot overwrite the registered operational installation.', mbCriticalError, MB_OK);
        Result := False;
        Exit;
      end;
    end;
  end;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
  if CurUninstallStep = usPostUninstall then
  begin
    // Policy: Preserve all business databases, operational data, and external QA instance configurations.
    Log('Uninstaller removed QA application binaries and shortcuts.');
    Log('All business databases and external QA instances remain preserved.');
  end;
end;
