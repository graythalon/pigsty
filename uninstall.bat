@echo off
setlocal
echo ==========================================================
echo  Pigsty - Uninstaller
echo ==========================================================
echo.

set "TARGET_DIR=%LOCALAPPDATA%\Programs\Pigsty"

echo [1/3] Removing Desktop and Start Menu shortcuts...
powershell -NoProfile -Command ^
  "$desktop = [Environment]::GetFolderPath('Desktop'); " ^
  "$startMenu = [Environment]::GetFolderPath('Programs'); " ^
  "Remove-Item -Force \"$desktop\Pigsty.lnk\" -ErrorAction SilentlyContinue; " ^
  "Remove-Item -Force \"$startMenu\Pigsty.lnk\" -ErrorAction SilentlyContinue; "

echo [2/3] Removing Windows Registry entries...
powershell -NoProfile -Command ^
  "Remove-Item -Recurse -Force 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\Pigsty' -ErrorAction SilentlyContinue; "

echo [3/3] Removing application files...
if exist "%TARGET_DIR%\Pigsty.exe" del /f /q "%TARGET_DIR%\Pigsty.exe"

echo.
echo ==========================================================
echo  [SUCCESS] Pigsty has been uninstalled successfully.
echo ==========================================================
echo.
pause
