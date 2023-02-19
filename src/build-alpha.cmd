@echo off
set VERSIONSUFFIX=alpha0029

echo Building %VERSIONSUFFIX%

dotnet build -c:Release --version-suffix %VERSIONSUFFIX% SharpGLTF.Core\SharpGLTF.Core.csproj
dotnet build -c:Release --version-suffix %VERSIONSUFFIX% SharpGLTF.Toolkit\SharpGLTF.Toolkit.csproj

pause