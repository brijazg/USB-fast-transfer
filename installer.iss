#define MyAppName "USB Turbo Transfer"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "USB Turbo Transfer"
#define MyAppExeName "USB Turbo Transfer.exe"

[Setup]
AppId={{D35A60C0-7B16-4E7A-8B7A-6C0F7D3A5A10}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\USB Turbo Transfer
DefaultGroupName={#MyAppName}
OutputDir=Output
OutputBaseFilename=USB-Turbo-Transfer-Setup
Compression=lzma
SolidCompression=yes
WizardStyle=modern
ArchitecturesInstallIn64BitMode=x64
UninstallDisplayIcon={app}\{#MyAppExeName}

[Files]
Source: "publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional icons:"

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Launch {#MyAppName}"; Flags: postinstall nowait skipifsilent
