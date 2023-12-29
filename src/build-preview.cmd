@echo off

set GETTIMEKEY=powershell get-date -format "{yyyyMMdd-HHmm}"
for /f %%i in ('%GETTIMEKEY%') do set TIMEKEY=%%i

set VERSIONSUFFIX=Preview-%TIMEKEY%

echo Building 1.0.0-%VERSIONSUFFIX%

dotnet build -c:Release --version-suffix %VERSIONSUFFIX% /p:Authors=vpenades SharpGLTF.Core\SharpGLTF.Core.csproj
dotnet build -c:Release --version-suffix %VERSIONSUFFIX% /p:Authors=vpenades SharpGLTF.Runtime\SharpGLTF.Runtime.csproj
dotnet build -c:Release --version-suffix %VERSIONSUFFIX% /p:Authors=vpenades SharpGLTF.Toolkit\SharpGLTF.Toolkit.csproj

dotnet build -c:Release --version-suffix %VERSIONSUFFIX% /p:Authors=vpenades SharpGLTF.Ext.Agi\SharpGLTF.Ext.Agi.csproj
dotnet build -c:Release --version-suffix %VERSIONSUFFIX% /p:Authors=vpenades SharpGLTF.Ext.3DTiles\SharpGLTF.Ext.3DTiles.csproj

set /p DUMMY=Hit ENTER to publish nuget packages on Github...

dotnet nuget push "SharpGLTF.Core/bin/Release/SharpGLTF.Core.1.0.0-%VERSIONSUFFIX%.nupkg" -s "github" --force-english-output
dotnet nuget push "SharpGLTF.Runtime/bin/Release/SharpGLTF.Runtime.1.0.0-%VERSIONSUFFIX%.nupkg" -s "github" --force-english-output
dotnet nuget push "SharpGLTF.Toolkit/bin/Release/SharpGLTF.Toolkit.1.0.0-%VERSIONSUFFIX%.nupkg" -s "github" --force-english-output

dotnet nuget push "SharpGLTF.Agi/bin/Release/SharpGLTF.Ext.Agi.1.0.0-%VERSIONSUFFIX%.nupkg" -s "github" --force-english-output
dotnet nuget push "SharpGLTF.Cesium/bin/Release/SharpGLTF.Ext.3DTiles.1.0.0-%VERSIONSUFFIX%.nupkg" -s "github" --force-english-output

pause