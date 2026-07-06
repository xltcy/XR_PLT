# 互动弹幕服务器

这个目录是给 Ubuntu 部署的弹幕转发服务。手机扫码进入网页发送消息，Unity 通过 WebSocket 接收同一个 session 的消息。

## 安装

```bash
cd Tools/BarrageServer
npm install
```

## 启动

```bash
PORT=37621 PUBLIC_BASE_URL=http://10.243.57.216:37621 npm start
```

如果只在校园网内使用，`PUBLIC_BASE_URL` 填校园网可访问的 IP 或域名。外网使用时，建议通过 Nginx/HTTPS 反向代理到本服务。

## 常用地址

- 手机网页：`http://10.243.57.216:37621/s/default`
- 二维码图片：`http://10.243.57.216:37621/api/qrcode?session=default`
- Unity WebSocket：`ws://10.243.57.216:37621/ws?role=unity&session=default`
- 健康检查：`http://10.243.57.216:37621/health`

## 环境变量

- `PORT`：监听端口，默认 `37621`。
- `PUBLIC_BASE_URL`：二维码使用的公网或校园网访问地址。
- `ADMIN_TOKEN`：清屏接口 token；为空时不校验。
- `MAX_MESSAGE_LENGTH`：单条弹幕最大长度，默认 `80`。
- `SEND_INTERVAL_MS`：同一用户发送间隔，默认 `1200` 毫秒。
- `LOG_DIR`：消息日志目录，默认 `Tools/BarrageServer/logs`。

## 日志

每个 session 会生成一个 JSONL 文件，例如：

```text
logs/default.jsonl
```

其中记录用户、时间、内容和清屏事件，方便后续统计或排查。
