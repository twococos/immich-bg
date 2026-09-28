; Inno Setup script for Immich BG. Build with publish.ps1 (it publishes the exe first).
; Per-user install (no admin rights), same as the app's "start with Windows" entry (HKCU).

#ifndef AppVersion
  #define AppVersion "0.1.0"
#endif
#define AppName "Immich BG"
#define AppExe "ImmichBg.exe"

[Setup]
; Never change AppId: it identifies the app for upgrades and uninstall.
AppId={{7a2b69f9-c52a-4bbc-8286-0eeb12e30db4}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher=Immich BG
DefaultDirName={localappdata}\Programs\ImmichBg
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
DisableDirPage=yes
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.17763
OutputDir=Output
OutputBaseFilename=ImmichBg-Setup-{#AppVersion}
SetupIconFile=..\src\ImmichBg\app.ico
UninstallDisplayIcon={app}\{#AppExe}
UninstallDisplayName={#AppName}
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
; Close a running copy during upgrades (it has no unsaved state).
CloseApplications=force
RestartApplications=no

[Languages]
Name: "ca"; MessagesFile: "compiler:Languages\Catalan.isl"
Name: "es"; MessagesFile: "compiler:Languages\Spanish.isl"
Name: "en"; MessagesFile: "compiler:Default.isl"

[CustomMessages]
ca.DeleteSettings=Vols esborrar també la configuració i els registres d'Immich BG?%n%nLes fotos ja descarregades es quedaran a les seves carpetes.
es.DeleteSettings=¿Quieres borrar también la configuración y los registros de Immich BG?%n%nLas fotos ya descargadas se quedarán en sus carpetas.
en.DeleteSettings=Also delete Immich BG settings and logs?%n%nPhotos already downloaded stay in their folders.

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "..\publish\{#AppExe}"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\{#AppName}"; Filename: "{app}\{#AppExe}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExe}"; Tasks: desktopicon

[Registry]
; The app creates this "start with Windows" value itself; remove it on uninstall.
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueName: "ImmichBg"; ValueType: none; Flags: uninsdeletevalue

[Run]
Filename: "{app}\{#AppExe}"; Description: "{cm:LaunchProgram,{#AppName}}"; Flags: nowait postinstall skipifsilent

[UninstallRun]
Filename: "{sys}\taskkill.exe"; Parameters: "/F /IM {#AppExe}"; Flags: runhidden; RunOnceId: "StopApp"

[Code]
procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
  if (CurUninstallStep = usPostUninstall) and not UninstallSilent then
    if MsgBox(CustomMessage('DeleteSettings'), mbConfirmation, MB_YESNO) = IDYES then
    begin
      DelTree(ExpandConstant('{userappdata}\ImmichBg'), True, True, True);
      DelTree(ExpandConstant('{localappdata}\ImmichBg'), True, True, True);
    end;
end;
