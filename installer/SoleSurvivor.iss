; Inno Setup 6 script for Sole Survivor. Run via ..\publish.ps1 -Installer (passes AppVersion),
; or compile directly after publishing.
#ifndef AppVersion
  #define AppVersion "1.0.23"
#endif

[Setup]
AppId={{91792052-73D7-4F65-A185-2A01BD78DE90}
AppName=Sole Survivor
AppVersion={#AppVersion}
AppPublisher=Ian
DefaultDirName={autopf}\Sole Survivor
DefaultGroupName=Sole Survivor
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
OutputDir=Output
OutputBaseFilename=SoleSurvivor-Setup-{#AppVersion}
SetupIconFile=..\src\SoleSurvivor.App\Assets\solesurvivor.ico
UninstallDisplayIcon={app}\SoleSurvivor.exe
Compression=lzma2
SolidCompression=yes
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
WizardStyle=modern

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "..\publish\win-x64\SoleSurvivor.exe"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\Sole Survivor"; Filename: "{app}\SoleSurvivor.exe"
Name: "{autodesktop}\Sole Survivor"; Filename: "{app}\SoleSurvivor.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\SoleSurvivor.exe"; Description: "{cm:LaunchProgram,Sole Survivor}"; Flags: nowait postinstall skipifsilent
