@echo off
setlocal
echo ==========================================================
echo  Pigsty - Android Storage Manager Build
echo ==========================================================

set MSBUILD="C:\Windows\Microsoft.NET\Framework64\v4.0.30319\msbuild.exe"
if not exist %MSBUILD% (
    set MSBUILD="C:\Windows\Microsoft.NET\Framework\v4.0.30319\msbuild.exe"
)

if not exist %MSBUILD% (
    echo [ERROR] MSBuild.exe was not found. Please ensure .NET Framework is installed.
    exit /b 1
)

echo Building Pigsty (Release)...
%MSBUILD% src\Pigsty.csproj /p:Configuration=Release /v:minimal /nologo

if %ERRORLEVEL% neq 0 (
    echo [ERROR] Build failed!
    exit /b %ERRORLEVEL%
)

if not exist dist mkdir dist
copy /y bin\Release\Pigsty.exe dist\Pigsty.exe >nul

echo.
echo [SUCCESS] Build succeeded! Executable located at: dist\Pigsty.exe
echo.
pause
