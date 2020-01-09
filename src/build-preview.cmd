@echo off

set GETTIMEKEY=powershell get-date -format "{yyyyMMdd-HHmm}"
for /f %%i in ('%GETTIMEKEY%') do set TIMEKEY=%%i

set VERSIONSUFFIX=Preview-%TIMEKEY%

echo Building 1.0.0-%VERSIONSUFFIX%

dotnet build -c:Release --version-suffix %VERSIONSUFFIX% /p:Authors=vpenades SharpGLTF.Core\SharpGLTF.Core.csproj
dotnet build -c:Release --version-suffix %VERSIONSUFFIX% /p:Authors=vpenades SharpGLTF.Toolkit\SharpGLTF.Toolkit.csproj

dotnet nuget push "SharpGLTF.Core/bin/Release/SharpGLTF.Core.1.0.0-%VERSIONSUFFIX%.nupkg" -s "github" --force-english-output
dotnet nuget push "SharpGLTF.Toolkit/bin/Release/SharpGLTF.Toolkit.1.0.0-%VERSIONSUFFIX%.nupkg" -s "github" --force-english-output

rem nuget push "SharpGLTF.Core/bin/Release/SharpGLTF.Core.1.0.0-%VERSIONSUFFIX%.nupkg" -src "github"
rem nuget push "SharpGLTF.Toolkit/bin/Release/SharpGLTF.Toolkit.1.0.0-%VERSIONSUFFIX%.nupkg" -src "github"

pause