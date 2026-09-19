#define MyAppName "Comet"
#ifndef MyAppVersion
  #define MyAppVersion "0.3.0-dev"
#endif
#define MyAppExeName "Comet.exe"

[Setup]
AppId={{D8F62919-24FD-46DF-88CE-4EE9B6FA580B}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher=Comet contributors
DefaultDirName={localappdata}\Programs\Comet
DefaultGroupName=Comet
OutputDir=..\artifacts\installer
OutputBaseFilename=Comet-v{#MyAppVersion}-win-x64-Setup
Compression=lzma2
SolidCompression=yes
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=lowest
ChangesAssociations=yes
DisableProgramGroupPage=yes
WizardStyle=modern
UninstallDisplayIcon={app}\{#MyAppExeName}

[Files]
Source: "..\artifacts\win-x64\*"; DestDir: "{app}"; Flags: recursesubdirs ignoreversion

[Icons]
Name: "{group}\Comet"; Filename: "{app}\{#MyAppExeName}"
Name: "{userdesktop}\Comet"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Tasks]
Name: "assoc"; Description: "Register Comet as an available app for ZIP/CBZ/RAR/CBR/7Z/CB7 files"; Flags: unchecked
Name: "desktopicon"; Description: "Create a desktop shortcut"; Flags: unchecked

[Registry]
Root: HKCU; Subkey: "Software\Comet\Capabilities"; ValueType: string; ValueName: "ApplicationName"; ValueData: "Comet"; Tasks: assoc; Flags: uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\Comet\Capabilities"; ValueType: string; ValueName: "ApplicationDescription"; ValueData: "Fast comic archive viewer"; Tasks: assoc
Root: HKCU; Subkey: "Software\Comet\Capabilities\FileAssociations"; ValueType: string; ValueName: ".zip"; ValueData: "Comet.Zip"; Tasks: assoc
Root: HKCU; Subkey: "Software\Comet\Capabilities\FileAssociations"; ValueType: string; ValueName: ".cbz"; ValueData: "Comet.Cbz"; Tasks: assoc
Root: HKCU; Subkey: "Software\Comet\Capabilities\FileAssociations"; ValueType: string; ValueName: ".rar"; ValueData: "Comet.Rar"; Tasks: assoc
Root: HKCU; Subkey: "Software\Comet\Capabilities\FileAssociations"; ValueType: string; ValueName: ".cbr"; ValueData: "Comet.Cbr"; Tasks: assoc
Root: HKCU; Subkey: "Software\Comet\Capabilities\FileAssociations"; ValueType: string; ValueName: ".7z"; ValueData: "Comet.SevenZip"; Tasks: assoc
Root: HKCU; Subkey: "Software\Comet\Capabilities\FileAssociations"; ValueType: string; ValueName: ".cb7"; ValueData: "Comet.Cb7"; Tasks: assoc
Root: HKCU; Subkey: "Software\RegisteredApplications"; ValueType: string; ValueName: "Comet"; ValueData: "Software\Comet\Capabilities"; Tasks: assoc; Flags: uninsdeletevalue
Root: HKCU; Subkey: "Software\Classes\Comet.Zip"; ValueType: string; ValueName: ""; ValueData: "Comet ZIP comic"; Tasks: assoc; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Classes\Comet.Zip\DefaultIcon"; ValueType: string; ValueName: ""; ValueData: "{app}\{#MyAppExeName},0"; Tasks: assoc
Root: HKCU; Subkey: "Software\Classes\Comet.Zip\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"" ""%1"""; Tasks: assoc
Root: HKCU; Subkey: "Software\Classes\Comet.Cbz"; ValueType: string; ValueName: ""; ValueData: "Comet CBZ comic"; Tasks: assoc; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Classes\Comet.Cbz\DefaultIcon"; ValueType: string; ValueName: ""; ValueData: "{app}\{#MyAppExeName},0"; Tasks: assoc
Root: HKCU; Subkey: "Software\Classes\Comet.Cbz\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"" ""%1"""; Tasks: assoc
Root: HKCU; Subkey: "Software\Classes\.zip\OpenWithProgids"; ValueType: none; ValueName: "Comet.Zip"; Tasks: assoc; Flags: uninsdeletevalue
Root: HKCU; Subkey: "Software\Classes\.cbz\OpenWithProgids"; ValueType: none; ValueName: "Comet.Cbz"; Tasks: assoc; Flags: uninsdeletevalue
Root: HKCU; Subkey: "Software\Classes\Comet.Rar"; ValueType: string; ValueName: ""; ValueData: "Comet RAR comic"; Tasks: assoc; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Classes\Comet.Rar\DefaultIcon"; ValueType: string; ValueName: ""; ValueData: "{app}\{#MyAppExeName},0"; Tasks: assoc
Root: HKCU; Subkey: "Software\Classes\Comet.Rar\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"" ""%1"""; Tasks: assoc
Root: HKCU; Subkey: "Software\Classes\Comet.Cbr"; ValueType: string; ValueName: ""; ValueData: "Comet CBR comic"; Tasks: assoc; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Classes\Comet.Cbr\DefaultIcon"; ValueType: string; ValueName: ""; ValueData: "{app}\{#MyAppExeName},0"; Tasks: assoc
Root: HKCU; Subkey: "Software\Classes\Comet.Cbr\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"" ""%1"""; Tasks: assoc
Root: HKCU; Subkey: "Software\Classes\Comet.SevenZip"; ValueType: string; ValueName: ""; ValueData: "Comet 7Z comic"; Tasks: assoc; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Classes\Comet.SevenZip\DefaultIcon"; ValueType: string; ValueName: ""; ValueData: "{app}\{#MyAppExeName},0"; Tasks: assoc
Root: HKCU; Subkey: "Software\Classes\Comet.SevenZip\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"" ""%1"""; Tasks: assoc
Root: HKCU; Subkey: "Software\Classes\Comet.Cb7"; ValueType: string; ValueName: ""; ValueData: "Comet CB7 comic"; Tasks: assoc; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Classes\Comet.Cb7\DefaultIcon"; ValueType: string; ValueName: ""; ValueData: "{app}\{#MyAppExeName},0"; Tasks: assoc
Root: HKCU; Subkey: "Software\Classes\Comet.Cb7\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"" ""%1"""; Tasks: assoc
Root: HKCU; Subkey: "Software\Classes\.rar\OpenWithProgids"; ValueType: none; ValueName: "Comet.Rar"; Tasks: assoc; Flags: uninsdeletevalue
Root: HKCU; Subkey: "Software\Classes\.cbr\OpenWithProgids"; ValueType: none; ValueName: "Comet.Cbr"; Tasks: assoc; Flags: uninsdeletevalue
Root: HKCU; Subkey: "Software\Classes\.7z\OpenWithProgids"; ValueType: none; ValueName: "Comet.SevenZip"; Tasks: assoc; Flags: uninsdeletevalue
Root: HKCU; Subkey: "Software\Classes\.cb7\OpenWithProgids"; ValueType: none; ValueName: "Comet.Cb7"; Tasks: assoc; Flags: uninsdeletevalue

[Run]
Filename: "ms-settings:defaultapps?registeredAppUser=Comet"; Description: "Open Windows Default Apps settings for Comet"; Flags: postinstall shellexec skipifsilent; Tasks: assoc
