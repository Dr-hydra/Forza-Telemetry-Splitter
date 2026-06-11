; Inno Setup script for Forza Telemetry Splitter
; Produces a single ForzaTelemetrySplitterInstaller.exe with shortcuts, an opt-in
; "start with Windows" choice, and a clean uninstaller. No auto-update by design.
;
; Build:  "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" installer\ForzaTelemetrySplitter.iss
; Expects the published exe at:  publish\ForzaTelemetrySplitter.exe
; (override with /DAppExe=... if your path differs)

#define MyAppName "Forza Telemetry Splitter"
#define MyAppShortName "ForzaTelemetrySplitter"
#define MyAppPublisher "Jake Mismas"
#define MyAppURL "https://github.com/jakemismas/Forza-Telemetry-Splitter"
#define MyAppExeName "ForzaTelemetrySplitter.exe"

; Pull the version straight from the published exe so the installer never drifts.
#ifndef AppExe
  #define AppExe "..\publish\ForzaTelemetrySplitter.exe"
#endif
#define MyAppVersion GetVersionNumbersString(AddBackslash(SourcePath) + AppExe)

[Setup]
; AppId is an OPAQUE, STABLE identifier Inno uses to detect prior installs for in-place upgrades.
; The last segment isn't valid hex, but Inno treats the whole value as a string — it works. Do NOT
; change it: a new AppId would make a new install no longer recognize/upgrade existing v0.x installs.
AppId={{B7E4B2A1-9C3D-4E5F-8A21-FTS0SPLITTER01}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}/issues
AppUpdatesURL={#MyAppURL}/releases

; Per-user install -> NO administrator/UAC prompt, matching the app's no-admin design.
PrivilegesRequired=lowest
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
OutputDir=..\publish
OutputBaseFilename=ForzaTelemetrySplitterInstaller
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
UninstallDisplayIcon={app}\{#MyAppExeName}

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop shortcut"; GroupDescription: "Shortcuts:"
Name: "startupicon"; Description: "Start {#MyAppName} automatically when Windows starts"; GroupDescription: "Startup:"; Flags: unchecked

[Files]
Source: "{#AppExe}"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\Uninstall {#MyAppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Registry]
; Opt-in "start with Windows" (per-user Run key; only written if the task is checked).
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; \
  ValueType: string; ValueName: "{#MyAppShortName}"; ValueData: """{app}\{#MyAppExeName}"""; \
  Flags: uninsdeletevalue; Tasks: startupicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Launch {#MyAppName}"; Flags: nowait postinstall skipifsilent

[UninstallRun]
; Best-effort: stop a running instance before uninstall so files aren't locked.
Filename: "{cmd}"; Parameters: "/C taskkill /IM ""{#MyAppExeName}"" /F"; Flags: runhidden; RunOnceId: "StopFTS"

[Code]
// Stop a running instance before install so the exe isn't locked during copy.
procedure CurStepChanged(CurStep: TSetupStep);
var
  ResultCode: Integer;
begin
  if CurStep = ssInstall then
    Exec(ExpandConstant('{cmd}'), '/C taskkill /IM "{#MyAppExeName}" /F',
         '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
end;
