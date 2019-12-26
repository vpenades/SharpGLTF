@echo off

:: Check WMIC is available
WMIC.EXE Alias /? >NUL 2>&1 || GOTO s_error

:: Use WMIC to retrieve date and time
FOR /F "skip=1 tokens=1-6" %%G IN ('WMIC Path Win32_LocalTime Get Day^,Hour^,Minute^,Month^,Second^,Year /Format:table') DO (
   IF "%%~L"=="" goto s_done
      Set _YYYY=%%L
      Set _MM=00%%J
      Set _DD=00%%G
      Set _HOUR=00%%H
      SET _MINUTE=00%%I
)
:s_done

:: Pad digits with leading zeros
      Set _MM=%_MM:~-2%
      Set _DD=%_DD:~-2%
      Set _HOUR=%_HOUR:~-2%
      Set _MINUTE=%_MINUTE:~-2%

:: Display the date/time in ISO 8601 format:
Set _ISODATE=%_YYYY%-%_MM%-%_DD% %_HOUR%:%_MINUTE%
Echo %_ISODATE%

set VERSIONSUFFIX=Preview-%_YYYY%%_MM%%_DD%-%_HOUR%%_MINUTE%

echo Building %VERSIONSUFFIX%

dotnet build -c:Release --version-suffix %VERSIONSUFFIX% SharpGLTF.Core\SharpGLTF.Core.csproj
dotnet build -c:Release --version-suffix %VERSIONSUFFIX% SharpGLTF.Toolkit\SharpGLTF.Toolkit.csproj

rem dotnet nuget push "SharpGLTF.Core/bin/Release/SharpGLTF.Core.1.0.0-%VERSIONSUFFIX%.nupkg" --source "github"
rem dotnet nuget push "SharpGLTF.Toolkit/bin/Release/SharpGLTF.Toolkit.1.0.0-%VERSIONSUFFIX%.nupkg" --source "github"

pause