@echo off
setlocal
dotnet publish USBTurboTransfer.csproj -c Release -r win-x64 --self-contained true ^
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true ^
  -p:EnableCompressionInSingleFile=true -o publish
if errorlevel 1 (
  echo Publish failed. Install the .NET 8 SDK and try again.
  pause
  exit /b 1
)
echo.
echo Published to the publish folder.
pause
