@echo off
set VERSIONSUFFIX=alpha0031

echo Building %VERSIONSUFFIX%

dotnet build -c:Release --version-suffix %VERSIONSUFFIX% SharpGLTF.Core\SharpGLTF.Core.csproj
dotnet build -c:Release --version-suffix %VERSIONSUFFIX% SharpGLTF.Runtime\SharpGLTF.Runtime.csproj
dotnet build -c:Release --version-suffix %VERSIONSUFFIX% SharpGLTF.Toolkit\SharpGLTF.Toolkit.csproj

dotnet build -c:Release --version-suffix %VERSIONSUFFIX% SharpGLTF.Agi\SharpGLTF.Agi.csproj
dotnet build -c:Release --version-suffix %VERSIONSUFFIX% SharpGLTF.Cesium\SharpGLTF.Cesium.csproj

pause