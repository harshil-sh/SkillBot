[Setup]
AppName=SkillBot
AppVersion=1.0.0
DefaultDirName={autopf}\SkillBot
DefaultGroupName=SkillBot
OutputBaseFilename=SkillBot-Setup
Compression=lzma
SolidCompression=yes
WizardStyle=modern

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"

[Files]
Source: "..\publish\win-x64\SkillBot.Api.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\publish\win-x64\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs

[Icons]
Name: "{group}\SkillBot"; Filename: "{app}\SkillBot.Api.exe"
Name: "{group}\{cm:UninstallProgram,SkillBot}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\SkillBot"; Filename: "{app}\SkillBot.Api.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\SkillBot.Api.exe"; Description: "{cm:LaunchProgram,SkillBot}"; Flags: nowait postinstall skipifsilent
