@echo off
setlocal

cd /d "%~dp0..\.."
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0Start-BarrageServer.ps1" -Port 37621

echo.
echo Press any key to close this window.
pause >nul
