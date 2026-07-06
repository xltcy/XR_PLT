const express = require("express");
const http = require("http");
const fs = require("fs");
const path = require("path");
const crypto = require("crypto");
const QRCode = require("qrcode");
const { WebSocket, WebSocketServer } = require("ws");

const PORT = Number(process.env.PORT || 37621);
const PUBLIC_BASE_URL = process.env.PUBLIC_BASE_URL || "";
const ADMIN_TOKEN = process.env.ADMIN_TOKEN || "";
const MAX_MESSAGE_LENGTH = Number(process.env.MAX_MESSAGE_LENGTH || 80);
const SEND_INTERVAL_MS = Number(process.env.SEND_INTERVAL_MS || 1200);
const LOG_DIR = process.env.LOG_DIR || path.join(__dirname, "logs");

const app = express();
const server = http.createServer(app);
const wss = new WebSocketServer({ noServer: true });

const sessions = new Map();
const rateLimits = new Map();

fs.mkdirSync(LOG_DIR, { recursive: true });
app.use(express.json({ limit: "32kb" }));

app.get("/", (req, res) => {
  const session = normalizeSession(req.query.session || "default");
  res.redirect(`/s/${encodeURIComponent(session)}`);
});

app.get("/health", (req, res) => {
  res.json({ ok: true, port: PORT, time: new Date().toISOString() });
});

app.get("/s/:session", (req, res) => {
  const session = normalizeSession(req.params.session);
  res.type("html").send(renderClientPage(session));
});

app.get("/api/qrcode", async (req, res) => {
  const session = normalizeSession(req.query.session || "default");
  const targetUrl = buildPublicUrl(req, `/s/${encodeURIComponent(session)}`);
  try {
    const png = await QRCode.toBuffer(targetUrl, {
      type: "png",
      margin: 1,
      width: 320,
      errorCorrectionLevel: "M"
    });
    res.type("png").send(png);
  } catch (error) {
    res.status(500).json({ error: error.message });
  }
});

app.post("/api/admin/clear", (req, res) => {
  if (!isAdminAllowed(req.body && req.body.token)) {
    res.status(401).json({ error: "invalid admin token" });
    return;
  }

  const session = normalizeSession(req.body.session || "default");
  broadcastToUnity(session, { type: "clear", sessionId: session, createdAt: new Date().toISOString() });
  appendLog(session, { type: "clear", createdAt: new Date().toISOString() });
  res.json({ ok: true });
});

server.on("upgrade", (req, socket, head) => {
  const url = new URL(req.url, `http://${req.headers.host}`);
  if (url.pathname !== "/ws") {
    socket.destroy();
    return;
  }

  wss.handleUpgrade(req, socket, head, (ws) => {
    wss.emit("connection", ws, req, url);
  });
});

wss.on("connection", (ws, req, url) => {
  const session = normalizeSession(url.searchParams.get("session") || "default");
  const role = normalizeRole(url.searchParams.get("role") || "client");
  const userId = normalizeUserId(url.searchParams.get("userId") || randomId("user"));
  const nickname = normalizeNickname(url.searchParams.get("nickname") || "访客");

  const client = { ws, session, role, userId, nickname, ip: getClientIp(req) };
  getSession(session).clients.add(client);

  ws.send(JSON.stringify({
    type: "hello",
    sessionId: session,
    role,
    userId,
    serverTime: new Date().toISOString()
  }));

  ws.on("message", (data) => handleSocketMessage(client, data));
  ws.on("close", () => getSession(session).clients.delete(client));
  ws.on("error", () => getSession(session).clients.delete(client));
});

server.listen(PORT, "0.0.0.0", () => {
  console.log(`[BarrageServer] listening on 0.0.0.0:${PORT}`);
  console.log(`[BarrageServer] sample page: http://127.0.0.1:${PORT}/s/default`);
});

function handleSocketMessage(client, data) {
  let payload;
  try {
    payload = JSON.parse(data.toString("utf8"));
  } catch {
    sendError(client.ws, "invalid json");
    return;
  }

  if (payload.type === "ping") {
    client.ws.send(JSON.stringify({ type: "pong", serverTime: new Date().toISOString() }));
    return;
  }

  if (payload.type === "clear") {
    if (!isAdminAllowed(payload.token)) {
      sendError(client.ws, "invalid admin token");
      return;
    }
    broadcastToUnity(client.session, { type: "clear", sessionId: client.session, createdAt: new Date().toISOString() });
    appendLog(client.session, { type: "clear", createdAt: new Date().toISOString() });
    return;
  }

  if (payload.type !== "send") {
    sendError(client.ws, "unsupported message type");
    return;
  }

  const rateKey = `${client.ip}:${client.userId}`;
  const now = Date.now();
  const lastSend = rateLimits.get(rateKey) || 0;
  if (now - lastSend < SEND_INTERVAL_MS) {
    sendError(client.ws, "send too frequently");
    return;
  }
  rateLimits.set(rateKey, now);

  const content = normalizeContent(payload.content);
  if (!content) {
    sendError(client.ws, "empty content");
    return;
  }

  const message = {
    id: randomId("msg"),
    sessionId: client.session,
    userId: normalizeUserId(payload.userId || client.userId),
    nickname: normalizeNickname(payload.nickname || client.nickname),
    content,
    createdAt: new Date().toISOString()
  };

  appendLog(client.session, { type: "barrage", message });
  broadcastToUnity(client.session, { type: "barrage", message });
  broadcastToClients(client.session, { type: "accepted", message });
}

function getSession(session) {
  if (!sessions.has(session)) {
    sessions.set(session, { clients: new Set() });
  }
  return sessions.get(session);
}

function broadcastToUnity(session, payload) {
  for (const client of getSession(session).clients) {
    if (client.role === "unity") {
      sendJson(client.ws, payload);
    }
  }
}

function broadcastToClients(session, payload) {
  for (const client of getSession(session).clients) {
    if (client.role === "client") {
      sendJson(client.ws, payload);
    }
  }
}

function sendJson(ws, payload) {
  if (ws.readyState === WebSocket.OPEN) {
    ws.send(JSON.stringify(payload));
  }
}

function sendError(ws, message) {
  sendJson(ws, { type: "error", error: message, serverTime: new Date().toISOString() });
}

function appendLog(session, record) {
  const file = path.join(LOG_DIR, `${safeFileName(session)}.jsonl`);
  fs.appendFile(file, `${JSON.stringify(record)}\n`, (error) => {
    if (error) {
      console.error("[BarrageServer] write log failed:", error.message);
    }
  });
}

function normalizeContent(value) {
  return String(value || "")
    .replace(/\s+/g, " ")
    .trim()
    .slice(0, MAX_MESSAGE_LENGTH);
}

function normalizeSession(value) {
  const text = String(value || "default").trim();
  return /^[a-zA-Z0-9_-]{1,64}$/.test(text) ? text : "default";
}

function normalizeRole(value) {
  return value === "unity" ? "unity" : "client";
}

function normalizeUserId(value) {
  return String(value || randomId("user")).replace(/[^\w.-]/g, "").slice(0, 64) || randomId("user");
}

function normalizeNickname(value) {
  return String(value || "访客").replace(/\s+/g, " ").trim().slice(0, 20) || "访客";
}

function randomId(prefix) {
  return `${prefix}_${crypto.randomBytes(8).toString("hex")}`;
}

function safeFileName(value) {
  return value.replace(/[^\w.-]/g, "_");
}

function getClientIp(req) {
  return String(req.headers["x-forwarded-for"] || req.socket.remoteAddress || "unknown").split(",")[0].trim();
}

function isAdminAllowed(token) {
  return ADMIN_TOKEN.length === 0 || token === ADMIN_TOKEN;
}

function buildPublicUrl(req, pathName) {
  if (PUBLIC_BASE_URL) {
    return `${PUBLIC_BASE_URL.replace(/\/$/, "")}${pathName}`;
  }

  const protocol = req.headers["x-forwarded-proto"] || "http";
  return `${protocol}://${req.headers.host}${pathName}`;
}

function renderClientPage(session) {
  return `<!doctype html>
<html lang="zh-CN">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1, viewport-fit=cover">
  <title>欢迎参观虚拟现实国家重点实验室</title>
  <style>
    :root { color-scheme: light; font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif; }
    body { margin: 0; background: #f6f7f9; color: #121417; }
    main { max-width: 520px; margin: 0 auto; padding: 24px 18px 40px; }
    h1 { font-size: 24px; margin: 8px 0 18px; }
    label { display: block; font-size: 14px; color: #4b5563; margin: 14px 0 6px; }
    textarea, button { box-sizing: border-box; width: 100%; border-radius: 8px; font-size: 16px; }
    textarea { border: 1px solid #cfd6df; padding: 12px; background: white; color: #121417; }
    textarea { min-height: 112px; resize: vertical; line-height: 1.5; }
    button { border: 0; padding: 13px 14px; font-weight: 700; }
    button:disabled { background: #9aa7b5; }
    .presets { display: grid; grid-template-columns: 1fr 1fr; gap: 10px; margin-top: 14px; }
    .preset { min-height: 46px; border: 1px solid #c8d2df; background: #ffffff; color: #243041; font-size: 14px; font-weight: 600; }
    .actions { display: grid; grid-template-columns: 2fr 1fr; gap: 10px; margin-top: 14px; }
    .primary { background: #1769e0; color: white; }
    .secondary { border: 1px solid #b9c3d0; background: #eef2f6; color: #273444; }
    .status { margin-top: 14px; min-height: 22px; font-size: 14px; color: #526070; }
    .count { text-align: right; font-size: 13px; color: #6b7280; margin-top: 4px; }
    @media (max-width: 380px) {
      .presets, .actions { grid-template-columns: 1fr; }
    }
  </style>
</head>
<body>
  <main>
    <h1>欢迎参观虚拟现实国家重点实验室</h1>
    <label for="content">弹幕内容</label>
    <textarea id="content" maxlength="${MAX_MESSAGE_LENGTH}" placeholder="请输入要显示在屏幕上的内容"></textarea>
    <div class="count"><span id="count">0</span>/${MAX_MESSAGE_LENGTH}</div>
    <div class="presets" aria-label="预设弹幕">
      <button class="preset" type="button">祝实验室发展越来越好</button>
      <button class="preset" type="button">期待更多科研成果落地</button>
    </div>
    <div class="actions">
      <button id="send" class="primary" type="button">发送弹幕</button>
      <button id="clear" class="secondary" type="button">清除内容</button>
    </div>
    <div class="status" id="status"></div>
  </main>
  <script>
    const session = ${JSON.stringify(session)};
    const userKey = "xrplt_barrage_user_id";
    let userId = localStorage.getItem(userKey);
    if (!userId) {
      userId = "visitor_" + Math.random().toString(16).slice(2) + Date.now().toString(16);
      localStorage.setItem(userKey, userId);
    }

    const content = document.getElementById("content");
    const send = document.getElementById("send");
    const clear = document.getElementById("clear");
    const status = document.getElementById("status");
    const count = document.getElementById("count");
    const presets = Array.from(document.querySelectorAll(".preset"));

    const wsScheme = location.protocol === "https:" ? "wss" : "ws";
    const ws = new WebSocket(wsScheme + "://" + location.host + "/ws?role=client&session=" + encodeURIComponent(session) + "&userId=" + encodeURIComponent(userId));

    ws.onopen = () => status.textContent = "已连接，可以发送弹幕";
    ws.onclose = () => status.textContent = "连接已断开，请刷新页面重试";
    ws.onerror = () => status.textContent = "连接异常，请检查网络";
    ws.onmessage = (event) => {
      const payload = JSON.parse(event.data);
      if (payload.type === "accepted") {
        status.textContent = "发送成功";
        send.disabled = false;
      } else if (payload.type === "error") {
        status.textContent = payload.error || "发送失败";
        send.disabled = false;
      }
    };

    content.addEventListener("input", () => count.textContent = content.value.length);
    presets.forEach((button) => {
      button.addEventListener("click", () => {
        content.value = button.textContent.trim();
        count.textContent = content.value.length;
        content.focus();
      });
    });
    clear.addEventListener("click", () => {
      content.value = "";
      count.textContent = "0";
      status.textContent = "";
      content.focus();
    });
    send.addEventListener("click", () => {
      const text = content.value.trim();
      if (!text) {
        status.textContent = "请输入弹幕内容";
        return;
      }
      if (ws.readyState !== WebSocket.OPEN) {
        status.textContent = "尚未连接服务器";
        return;
      }
      send.disabled = true;
      status.textContent = "发送中...";
      ws.send(JSON.stringify({
        type: "send",
        session,
        userId,
        nickname: "访客",
        content: text
      }));
    });
  </script>
</body>
</html>`;
}
