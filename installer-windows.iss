; TerraLauncher — Inno Setup installer script
; Build the publish output first:
;   dotnet publish TerraLauncher/TerraLauncher.csproj -c Release -r win-x64 --self-contained -o ./publish/win-x64
; Then open this file in Inno Setup Compiler and click Build > Compile.

#define AppName      "TerraLauncher"
#define AppVersion   "1.0.0"
#define AppPublisher "MrPoloGit"
#define AppURL       "https://github.com/MrPoloGit/TerraLauncher"
#define AppExe       "TerraLauncher.exe"
#define SourceDir    "publish\win-x64"

[Setup]
AppId={{A3F2C1D4-7B8E-4F2A-9C5D-1E6B3A4F7D2C}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL={#AppURL}
AppSupportURL={#AppURL}/issues
AppUpdatesURL={#AppURL}/releases
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
AllowNoIcons=yes
; Require admin rights to write to Program Files
PrivilegesRequired=admin
OutputDir=installer-output
OutputBaseFilename=TerraLauncher-Setup-Windows-x64
SetupIconFile=TerraLauncher\App.ico
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
; Minimum Windows 10
MinVersion=10.0.17763
ArchitecturesInstallIn64BitMode=x64compatible

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop shortcut"; GroupDescription: "Additional icons:"; Flags: unchecked

[Files]
; All published files — TerraLauncher.exe plus its self-contained .NET runtime
Source: "{#SourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#AppName}";       Filename: "{app}\{#AppExe}"; IconFilename: "{app}\{#AppExe}"
Name: "{group}\Uninstall {#AppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExe}"; IconFilename: "{app}\{#AppExe}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExe}"; Description: "Launch {#AppName}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
; Remove config + log files written at runtime so uninstall is clean
Type: files; Name: "{app}\TerraLauncher.xml"
Type: files; Name: "{app}\TerraLauncher-launch.log"
Type: files; Name: "{app}\instances.json"
