# Building Windows Installer

## Prerequisites
- Install Inno Setup: https://jrsoftware.org/isdl.php
- .NET 10 SDK

## Steps

1. Build self-contained Windows executable:
   ```
   dotnet publish SkillBot.Api/SkillBot.Api.csproj -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -o publish/win-x64
   dotnet publish SkillBot.Console/SkillBot.Console.csproj -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -o publish/win-x64-console
   ```

2. Create Inno Setup script (SkillBot.iss):
   [See SkillBot.iss in this directory]

3. Compile with Inno Setup:
   ```
   iscc SkillBot.iss
   ```

4. Output: Output/SkillBot-Setup.exe
