@echo off
chcp 65001 >nul
setlocal

:menu
cls
echo ==========================================
echo XR_PLT PPT 远程控制防火墙菜单
echo UDP 端口: 3414
echo ==========================================
echo 1. 检测防火墙和端口监听状态
echo 2. 开放 UDP 3414 入站端口
echo 3. 关闭本工具创建的 UDP 3414 规则
echo 4. 退出
echo.
set /p choice=请输入数字并按回车: 

if "%choice%"=="1" goto status
if "%choice%"=="2" goto open
if "%choice%"=="3" goto close
if "%choice%"=="4" goto end

echo 输入无效，请重新选择。
pause
goto menu

:status
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0PptRemoteFirewall.ps1" -Action Status
pause
goto menu

:open
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0PptRemoteFirewall.ps1" -Action Open
pause
goto menu

:close
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0PptRemoteFirewall.ps1" -Action Close
pause
goto menu

:end
endlocal
