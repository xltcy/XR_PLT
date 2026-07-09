# 互动弹幕服务器

手机扫码进入网页发送弹幕，Unity 通过 WebSocket 接收同一个 `session` 的消息。

默认端口：`37621`

默认访问地址：
- 手机网页：`http://<server-host>:37621/s/default`
- Unity WebSocket：`ws://<server-host>:37621/ws?role=unity&session=default`
- 健康检查：`http://<server-host>:37621/health`

## 部署文件

部署到 Ubuntu 或 Windows 服务器时，请复制整个 `Tools/BarrageServer` 目录，不要只复制 `scripts/windows` 或 `scripts/ubuntu`。

服务运行至少需要这些文件：
- `server.js`
- `package.json`
- `package-lock.json`
- `scripts/`

如果只复制脚本目录，启动脚本会找不到 `package.json`，`npm install` 就会在错误目录执行。

## 普通启动

这种方式会跟随当前终端退出，不适合远程部署：

```bash
cd Tools/BarrageServer
npm install
PORT=37621 npm start
```

## Ubuntu 后台运行

这个方案不会写入系统目录，只在 `Tools/BarrageServer` 内写入 PID 和日志。SSH 断开后服务仍会继续运行。

启动：

```bash
cd Tools/BarrageServer
PORT=37621 bash scripts/ubuntu/start-background.sh
```

查看状态：

```bash
bash scripts/ubuntu/status-background.sh
```

停止：

```bash
bash scripts/ubuntu/stop-background.sh
```

日志位置：

```text
Tools/BarrageServer/logs/server.out.log
Tools/BarrageServer/logs/server.err.log
```

PID 文件：

```text
Tools/BarrageServer/barrage-server.pid
```

注意：这个方案可以脱离 SSH 终端继续运行，但不会在系统重启后自动恢复。如果需要开机自启，再考虑 `systemd`、`pm2 startup` 或计划任务。

## Windows 后台运行

如果通过远程桌面操作，推荐直接双击下面的脚本，窗口会停留，方便查看错误：

```text
Tools/BarrageServer/scripts/windows/start-barrage-server.bat
Tools/BarrageServer/scripts/windows/status-barrage-server.bat
Tools/BarrageServer/scripts/windows/stop-barrage-server.bat
```

也可以在 PowerShell 中执行：

```powershell
cd Tools\BarrageServer
.\scripts\windows\Start-BarrageServer.ps1 -Port 37621
```

如果是第一次运行，脚本会自动安装依赖；也可以手动指定：

```powershell
.\scripts\windows\Start-BarrageServer.ps1 -InstallDependencies
```

查看状态：

```powershell
.\scripts\windows\Status-BarrageServer.ps1
```

停止：

```powershell
.\scripts\windows\Stop-BarrageServer.ps1
```

Windows 版本会把进程 PID 写到：

```text
Tools/BarrageServer/barrage-server.pid
```

日志会写到：

```text
Tools/BarrageServer/logs/server.out.log
Tools/BarrageServer/logs/server.err.log
```

## Windows 防火墙

如果直接通过端口访问，需要在云服务器安全组和 Windows 防火墙中放开 `TCP 37621`。

如果后续使用 Nginx/IIS 做 HTTPS 反向代理，则外部通常只需要放开 `TCP 443`，内部再转发到本服务的 `37621`。

## 环境变量

- `PORT`：监听端口，默认 `37621`。
- `PUBLIC_BASE_URL`：可选。手机访问用的公网或校园网地址；不填时，服务端会根据请求的 `Host` 自动生成链接。
- `ADMIN_TOKEN`：清屏接口 token；为空时不校验。
- `MAX_MESSAGE_LENGTH`：单条弹幕最大长度，默认 `80`。
- `SEND_INTERVAL_MS`：同一用户发送间隔，默认 `1200` 毫秒。
- `LOG_DIR`：消息日志目录，默认 `Tools/BarrageServer/logs`。

## 外网访问建议

如果需要外网访问，建议使用 Nginx/HTTPS 反向代理到本服务，并把 `PUBLIC_BASE_URL` 配成外网域名，例如：

```bash
PUBLIC_BASE_URL=https://your-domain.example.com
```
