@echo off
setlocal
cd /d "%~dp0"
where dotnet >nul 2>nul
if errorlevel 1 (
 echo Install the .NET 10 SDK for Windows before building.
 pause
 exit /b 1
)
dotnet publish src\ChestCounter\ChestCounter.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -p:DebugType=none -o dist > build-personal.log 2>&1
if errorlevel 1 (
 type build-personal.log
 echo Build failed. Share the full build-personal.log text for correction.
 pause
 exit /b 1
)
echo Built dist\ChestCounter.exe - test on backed-up data first.
pause
