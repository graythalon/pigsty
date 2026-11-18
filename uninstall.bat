@echo off
setlocal
echo ==========================================================
echo  Pigsty - Uninstaller
echo ==========================================================
echo.

set "TARGET_DIR=%LOCALAPPDATA%\Programs\Pigsty"

echo [1/3] Removing shortcuts...
powershell -NoProfile -Command ^
  "$desktop = [Environment]::GetFolderPath('Desktop'); " ^
  "$startMenu = [Environment]::GetFolderPath('Programs'); " ^
  "Remove-Item -Force \"$desktop\Pigsty.lnk\" -ErrorAction SilentlyContinue; " ^
  "Remove-Item -Force \"$startMenu\Pigsty.lnk\" -ErrorAction SilentlyContinue; "

echo [2/3] Removing registry entries...
powershell -NoProfile -Command ^
  "Remove-Item -Recurse -Force 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\Pigsty' -ErrorAction SilentlyContinue; "

echo [3/3] Removing application files...
if exist "%TARGET_DIR%\Pigsty.exe" del /f /q "%TARGET_DIR%\Pigsty.exe"

echo.
echo [SUCCESS] Pigsty has been uninstalled.
start "" /b cmd /c "ping 127.0.0.1 -n 2 >nul & rmdir /s /q ""%TARGET_DIR%"" 2>nul"
exit /b 0
