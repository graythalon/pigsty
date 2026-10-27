@echo off
setlocal
echo ==========================================================
echo  Pigsty - Windows Installer (Zero Admin Rights Needed)
echo ==========================================================
echo.

set "TARGET_DIR=%LOCALAPPDATA%\Programs\Pigsty"
set "EXE_SRC=%~dp0dist\Pigsty.exe"

if not exist "%EXE_SRC%" (
    echo [INFO] dist\Pigsty.exe not found. Building now...
    call "%~dp0build.bat"
)

if not exist "%EXE_SRC%" (
    echo [ERROR] Could not find or build Pigsty.exe.
    pause
    exit /b 1
)

echo [1/3] Copying application files to %TARGET_DIR%...
if not exist "%TARGET_DIR%" mkdir "%TARGET_DIR%"
copy /y "%EXE_SRC%" "%TARGET_DIR%\Pigsty.exe" >nul
copy /y "%~dp0uninstall.bat" "%TARGET_DIR%\uninstall.bat" >nul

echo [2/3] Creating Desktop and Start Menu shortcuts...
powershell -NoProfile -Command ^
  "$ws = New-Object -ComObject WScript.Shell; " ^
  "$desktop = [Environment]::GetFolderPath('Desktop'); " ^
  "$startMenu = [Environment]::GetFolderPath('Programs'); " ^
  "$exe = '%TARGET_DIR%\Pigsty.exe'; " ^
  "$s1 = $ws.CreateShortcut(\"$desktop\Pigsty.lnk\"); $s1.TargetPath = $exe; $s1.Description = 'Pigsty - Android Storage Manager'; $s1.Save(); " ^
  "$s2 = $ws.CreateShortcut(\"$startMenu\Pigsty.lnk\"); $s2.TargetPath = $exe; $s2.Description = 'Pigsty - Android Storage Manager'; $s2.Save(); "

echo [3/3] Registering in Windows Add/Remove Programs...
powershell -NoProfile -Command ^
  "$regPath = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\Pigsty'; " ^
  "if (!(Test-Path $regPath)) { New-Item -Path $regPath -Force | Out-Null }; " ^
  "Set-ItemProperty -Path $regPath -Name 'DisplayName' -Value 'Pigsty (Android Storage Manager)'; " ^
  "Set-ItemProperty -Path $regPath -Name 'DisplayVersion' -Value '1.0.0'; " ^
  "Set-ItemProperty -Path $regPath -Name 'Publisher' -Value 'Pigsty Open Source'; " ^
  "Set-ItemProperty -Path $regPath -Name 'DisplayIcon' -Value '%TARGET_DIR%\Pigsty.exe'; " ^
  "Set-ItemProperty -Path $regPath -Name 'UninstallString' -Value 'cmd.exe /c \"\"%TARGET_DIR%\uninstall.bat\"\"'; "

echo.
echo ==========================================================
echo  [SUCCESS] Pigsty has been installed successfully!
echo  You can launch it from your Desktop or Start Menu.
echo ==========================================================
echo.
pause
