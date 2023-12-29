@echo off
set VERSIONSUFFIX=alpha0031

echo Building %VERSIONSUFFIX%

dotnet build -c:Release --version-suffix %VERSIONSUFFIX% SharpGLTF.Core\SharpGLTF.Core.csproj
dotnet build -c:Release --version-suffix %VERSIONSUFFIX% SharpGLTF.Runtime\SharpGLTF.Runtime.csproj
dotnet build -c:Release --version-suffix %VERSIONSUFFIX% SharpGLTF.Toolkit\SharpGLTF.Toolkit.csproj

dotnet build -c:Release --version-suffix %VERSIONSUFFIX% SharpGLTF.Ext.Agi\SharpGLTF.Ext.Agi.csproj
dotnet build -c:Release --version-suffix %VERSIONSUFFIX% SharpGLTF.Ext.3DTiles\SharpGLTF.Ext.3DTiles.csproj

pause