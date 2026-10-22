@echo off
setlocal
cd /d "%~dp0"
where dotnet >nul 2>nul
if errorlevel 1 (
  echo .NET SDK 8 is required to BUILD this project. It is not required on the target PC after publishing self-contained.
  echo Install the .NET 8 SDK on a build machine, then run this script again.
  pause
  exit /b 1
)
dotnet publish PortableBulkFileRenamer.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -o publish\win-x64
if errorlevel 1 (
  echo Publish failed.
  pause
  exit /b 1
)
echo.
echo Published app: publish\win-x64\BulkFileRenamer.exe
echo Copy the EXE to a writable folder or USB drive to use it as a portable app.
pause
