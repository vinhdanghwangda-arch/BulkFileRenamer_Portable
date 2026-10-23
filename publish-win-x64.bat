@echo off
setlocal
cd /d "%~dp0"
dotnet publish PortableBulkFileRenamer.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -o publish\win-x64
if errorlevel 1 (
  echo Publish failed. Review the errors above.
  exit /b 1
)
echo.
echo Publish completed. Check publish\win-x64 for the EXE.
endlocal
