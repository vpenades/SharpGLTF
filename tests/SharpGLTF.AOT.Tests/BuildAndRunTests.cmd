
dotnet publish SharpGLTF.AOT.Tests.csproj -c Release -o bin/Publish

cd bin/Publish

SharpGLTF.AOT.Tests.exe

pause