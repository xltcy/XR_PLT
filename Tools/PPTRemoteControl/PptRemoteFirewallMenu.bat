@echo off
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0scripts\PptRemoteFirewall.ps1" -Action Menu %*
if errorlevel 1 pause
