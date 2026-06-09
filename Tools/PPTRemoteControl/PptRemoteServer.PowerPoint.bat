@echo off
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0scripts\PptRemoteServer.ps1" -ControlMode PowerPoint %*
if errorlevel 1 pause
