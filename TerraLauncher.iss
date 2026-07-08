; Inno Setup script for TerraLauncher.
;
; Prerequisite: a self-contained publish of the app must already exist at
; .\publish (see the "Publishing" section of the README for the dotnet
; publish command). This script does not build the app itself.
;
; Build locally with Inno Setup 6 (https://jrsoftware.org/isinfo.php), run
; from the repo root:
;   iscc TerraLauncher.iss
;   iscc /DMyAppVersion=1.2.3 TerraLauncher.iss
;
; CI builds this automatically for tagged releases (see .github/workflows/release.yml).

#ifndef MyAppVersion
  #define MyAppVersion "0.0.0"
#endif
#define MyAppName "TerraLauncher"
#define MyAppPublisher "MrPoloGit"
#define MyAppExeName "TerraLauncher.exe"
#define MyAppURL "https://github.com/MrPoloGit/TerraLauncher"

[Setup]
AppId={{4C2B8B2E-6E7B-4E9B-9B3E-3B9E7E9B0A11}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
; TerraLauncher stores its config (TerraLauncher.xml) and all downloaded
; instances directly next to the exe, so it needs a directory the user can
; write to without elevation - installing under Program Files would break
; the in-app downloader for non-admin users. Defaults to a per-user
; location instead, matching how portable-style apps like VS Code install.
DefaultDirName={localappdata}\Programs\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
OutputDir=installer-output
OutputBaseFilename=TerraLauncher-{#MyAppVersion}-Setup
SetupIconFile=TerraLauncher\App.ico
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
UninstallDisplayIcon={app}\{#MyAppExeName}

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "publish\*"; DestDir: "{app}"; Excludes: "*.pdb"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\Uninstall {#MyAppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

; Deliberately no [UninstallDelete] for {app}: TerraLauncher.xml, Instances\,
; and Tools\ live there too, and those can be several GB of downloaded games.
; The default uninstaller only removes the files it installed, leaving user
; data untouched (and leaves the folder behind if it isn't empty). Worlds,
; Players, and Mods for downloaded instances live outside {app} entirely,
; under Documents\My Games\TerraLauncher\Instances, so they're unaffected
; either way.
