@echo off
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0PptRemoteServer.ps1" -ControlMode PowerPoint %*
if errorlevel 1 pause
