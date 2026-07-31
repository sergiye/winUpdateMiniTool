@echo off

FOR /F "tokens=* USEBACKQ" %%F IN (`"%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe" -latest -prerelease -products * -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe`) DO (
SET msbuild="%%F"
)
ECHO %msbuild%

@%msbuild% winUpdateMiniTool.sln /t:restore /p:RestorePackagesConfig=true
@%msbuild% winUpdateMiniTool.sln /t:Rebuild /p:DebugType=None /p:Configuration=Release /p:TargetFramework=net472 /p:RuntimeIdentifier=win-x64
md winUpdateMiniTool\bin\publish\
copy /Y winUpdateMiniTool\bin\net472\win-x64\* winUpdateMiniTool\bin\publish\

rem @%msbuild% winUpdateMiniTool.sln /t:restore /p:RestorePackagesConfig=true /p:TargetFramework=net8.0-windows /p:RuntimeIdentifier=win-x64
@%msbuild% winUpdateMiniTool/winUpdateMiniTool.csproj /t:Publish /p:DebugType=None /p:Configuration=Release /p:TargetFramework=net8.0-windows /p:RuntimeIdentifier=win-x64 /p:SelfContained=false /p:PublishSingleFile=true /p:AssemblyName=winUpdateMiniTool.core /p:PublishDir=bin\publish\ /p:DeleteExistingFiles=false
@%msbuild% winUpdateMiniTool/winUpdateMiniTool.csproj /t:Publish /p:DebugType=None /p:Configuration=Release /p:TargetFramework=net8.0-windows /p:RuntimeIdentifier=win-arm64 /p:SelfContained=false /p:PublishSingleFile=true /p:AssemblyName=winUpdateMiniTool.arm64 /p:PublishDir=bin\publish\ /p:DeleteExistingFiles=false

if errorlevel 1 goto error

goto exit
:error
pause
:exit
