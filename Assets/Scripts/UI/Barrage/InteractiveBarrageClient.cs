using System;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TickSystem;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 互动弹幕 Unity WebSocket 客户端。
/// 
/// 整条弹幕链路分为三段：
/// 1. 手机网页连接后端服务器，发送弹幕文本。
/// 2. 后端服务器按 session 把弹幕广播给 role=unity 的 WebSocket 连接。
/// 3. 本组件接收服务器推送，保存本地记录，并把消息交给 <see cref="BarrageDisplay"/> 播放。
/// 
/// 本组件负责：
/// - 维护 WebSocket 连接和自动重连。
/// - 解析服务器推送的 JSON 消息。
/// - 保存最近收到的弹幕记录。
/// - 生成手机网页二维码并控制 p_qr_code 节点显示。
/// - 提供 Debug 面板可调用的 Configure 方法，用于运行时切换服务器 IP。
/// </summary>
public class InteractiveBarrageClient : BaseStateComponent, ITickerInterval
{
    [Header("连接配置")]
    /// <summary>
    /// 弹幕后端服务器 IP 或域名。
    /// DebugBarrageComponent 会读取和修改这个值。
    /// </summary>
    [SerializeField] private string serverIp = "1.92.83.226";

    /// <summary>
    /// 弹幕后端监听端口。当前后端默认端口是 37621。
    /// </summary>
    [SerializeField] private int serverPort = 37621;

    /// <summary>
    /// 是否使用 HTTPS/WSS。
    /// false 时使用 http/ws，true 时使用 https/wss。
    /// </summary>
    [SerializeField] private bool useHttps;

    /// <summary>
    /// 会话 ID。手机网页和 Unity 必须使用相同 session，服务器才会把弹幕转发到当前 Unity。
    /// </summary>
    [SerializeField] private string sessionId = "default";

    /// <summary>
    /// 是否在 Start 时自动连接弹幕后端。
    /// </summary>
    [SerializeField] private bool connectOnStart = true;

    /// <summary>
    /// 断线或连接失败后的重连间隔。
    /// </summary>
    [SerializeField] private float reconnectDelay = 3f;

    /// <summary>
    /// TickController 调用 TickInterval 的间隔。
    /// 这个组件没有使用 Update，而是通过项目统一 TickSystem 做周期性处理。
    /// </summary>
    [SerializeField] private float tickInterval = 1f;

    [Header("显示与记录")]
    /// <summary>
    /// 实际显示弹幕的组件。未绑定时会在当前物体上查找。
    /// </summary>
    [SerializeField] private BarrageDisplay barrageDisplay;

    /// <summary>
    /// Unity 本地保存的最近弹幕记录上限。
    /// 记录用于调试、统计或后续扩展，不影响屏幕显示队列。
    /// </summary>
    [SerializeField] private int maxLocalRecords = 500;

    /// <summary>
    /// 每次 Tick 最多处理多少条服务器消息。
    /// 避免一次性处理大量消息导致单帧卡顿。
    /// </summary>
    [SerializeField] private int maxMessagesPerFrame = 20;

    [Header("二维码")]
    /// <summary>
    /// 二维码根节点。需求是“连接上服务器后显示 p_qr_code”。
    /// 该字段通过 ComponentBinder 按节点名自动绑定。
    /// </summary>
    [BindChild("p_qr_code")]
    [SerializeField] private GameObject qrCodeRoot;

    /// <summary>
    /// 显示二维码贴图的 RawImage。
    /// 如果根节点下没有 RawImage，且允许自动创建，则会运行时创建。
    /// </summary>
    [SerializeField] private RawImage qrCodeImage;

    /// <summary>
    /// 连接成功后是否自动生成二维码。
    /// </summary>
    [SerializeField] private bool loadQrCodeOnStart = true;

    /// <summary>
    /// p_qr_code 节点没有 RawImage 时，是否自动创建一个 RawImage 子节点。
    /// </summary>
    [SerializeField] private bool createQrCodeImageIfMissing = true;

    /// <summary>
    /// 自动创建二维码节点时使用的默认尺寸。
    /// 如果 prefab 中已有 p_qr_code 布局，则不会强行覆盖现有布局。
    /// </summary>
    [SerializeField] private Vector2 qrCodeSize = new Vector2(220f, 220f);

    /// <summary>
    /// 自动创建二维码节点时使用的默认锚点位置。
    /// </summary>
    [SerializeField] private Vector2 qrCodeAnchoredPosition = new Vector2(-140f, -140f);

    /// <summary>
    /// 可选的手机网页 URL 覆盖。
    /// 为空时由 serverIp/serverPort/sessionId 拼接；不为空时可以使用 {session} 占位符。
    /// </summary>
    [SerializeField] private string webPageUrlOverride;

    /// <summary>
    /// 后台接收线程写入的原始 JSON 消息队列。
    /// 队列内容只在主线程 Tick 中消费，避免在后台线程直接操作 Unity 对象。
    /// </summary>
    private readonly Queue<string> pendingPayloads = new Queue<string>();

    /// <summary>
    /// 本地保存的最近弹幕记录。
    /// </summary>
    private readonly List<InteractiveBarrageMessage> localRecords = new List<InteractiveBarrageMessage>();

    /// <summary>
    /// pendingPayloads 会被后台 ReceiveLoop 和主线程 Tick 同时访问，因此需要加锁。
    /// </summary>
    private readonly object pendingLock = new object();

    private ClientWebSocket webSocket;
    private CancellationTokenSource cancellationTokenSource;
    private float nextReconnectTime;
    private bool isConnecting;
    private bool isQuitting;
    private Texture2D qrCodeTexture;
    private bool qrCodeLoaded;

    /// <summary>
    /// 后台线程不能直接操作 Unity UI，因此用这些标志让主线程 Tick 执行 UI 操作。
    /// </summary>
    private volatile bool pendingQrCodeRefresh;
    private volatile bool pendingQrCodeHide;
    private volatile bool pendingReconnectSchedule;

    public IReadOnlyList<InteractiveBarrageMessage> LocalRecords => localRecords;
    public string SessionId => sessionId;
    public string ServerIp => GetServerHost();
    public int ServerPort => serverPort;
    public bool IsConnected => webSocket != null && webSocket.State == WebSocketState.Open;
    public float TickIntervalTime => Mathf.Max(0.5f, tickInterval);

    protected override void Awake()
    {
        base.Awake();

        if (!barrageDisplay)
        {
            barrageDisplay = GetComponent<BarrageDisplay>();
        }

        EnsureQrCodeRootAndImage();
        SetQrCodeVisible(false);
        TickController.RegisterTick(this);
    }

    private void Start()
    {
        if (connectOnStart)
        {
            Connect();
        }
    }

    /// <summary>
    /// 由 TickController 按 tickInterval 周期调用。
    /// 这里集中处理主线程任务：二维码显示、重连调度、消息派发。
    /// </summary>
    public void TickInterval()
    {
        if (pendingQrCodeHide)
        {
            pendingQrCodeHide = false;
            SetQrCodeVisible(false);
        }

        if (pendingQrCodeRefresh)
        {
            pendingQrCodeRefresh = false;
            RefreshQrCode();
        }
        else if (loadQrCodeOnStart && IsConnected && !qrCodeLoaded)
        {
            RefreshQrCode();
        }

        if (pendingReconnectSchedule)
        {
            pendingReconnectSchedule = false;
            nextReconnectTime = Time.realtimeSinceStartup + reconnectDelay;
        }

        ProcessPendingPayloads();

        if (connectOnStart && !isQuitting && !isConnecting && !IsConnected && Time.realtimeSinceStartup >= nextReconnectTime)
        {
            Connect();
        }
    }

    protected override void OnDestroy()
    {
        isQuitting = true;
        TickController.UnRegisterTick(this);
        Disconnect();
        ClearQrCodeTexture();
        base.OnDestroy();
    }

    /// <summary>
    /// 修改服务器地址、端口和 session，并按需立即重连。
    /// DebugBarrageComponent 的刷新 IP 按钮会调用这个方法。
    /// </summary>
    public void Configure(string ip, int port, string newSessionId, bool reconnectImmediately = true)
    {
        serverIp = string.IsNullOrWhiteSpace(ip) ? serverIp : ip.Trim();
        serverPort = port > 0 ? port : serverPort;
        sessionId = string.IsNullOrWhiteSpace(newSessionId) ? "default" : newSessionId.Trim();

        if (reconnectImmediately)
        {
            Disconnect();
            Connect();
        }

        pendingQrCodeRefresh = loadQrCodeOnStart;
    }

    /// <summary>
    /// 本地生成二维码贴图并显示到 p_qr_code。
    /// 二维码内容是手机网页 URL，手机扫码后进入当前 session 的弹幕页面。
    /// </summary>
    public void RefreshQrCode()
    {
        if (!isActiveAndEnabled)
        {
            return;
        }

        EnsureQrCodeRootAndImage();
        if (!qrCodeImage)
        {
            Debug.LogWarning("[InteractiveBarrageClient] QR code image is missing. Please add a RawImage under p_qr_code or enable auto creation.");
            qrCodeLoaded = false;
            return;
        }

        string webPageUrl = BuildWebPageUrl();
        QrCodeManager qrCodeManager = ManagerRefer.Get<QrCodeManager>();
        Texture2D texture = qrCodeManager?.GenerateTexture(webPageUrl, 8, 4);
        if (!texture)
        {
            Debug.LogWarning($"[InteractiveBarrageClient] Generate QR code failed, url={webPageUrl}");
            qrCodeLoaded = false;
            return;
        }

        ClearQrCodeTexture();
        qrCodeTexture = texture;
        qrCodeImage.texture = qrCodeTexture;
        qrCodeLoaded = true;
        SetQrCodeVisible(true);
        Debug.Log($"[InteractiveBarrageClient] QR code generated: {webPageUrl}", this);
    }

    /// <summary>
    /// 主动建立 WebSocket 连接。
    /// 连接成功后启动后台 ReceiveLoop 持续接收服务器推送。
    /// </summary>
    public async void Connect()
    {
        if (isConnecting || IsConnected)
        {
            return;
        }

        isConnecting = true;
        cancellationTokenSource = new CancellationTokenSource();
        webSocket = new ClientWebSocket();

        try
        {
            Uri uri = BuildUnityWebSocketUri();
            await webSocket.ConnectAsync(uri, cancellationTokenSource.Token).ConfigureAwait(false);
            _ = ReceiveLoop(webSocket, cancellationTokenSource.Token);
            pendingQrCodeRefresh = loadQrCodeOnStart;
            Debug.Log($"[InteractiveBarrageClient] Connected: {uri}");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[InteractiveBarrageClient] Connect failed: {e.Message}");
            DisposeSocket();
            ScheduleReconnect();
        }
        finally
        {
            isConnecting = false;
        }
    }

    /// <summary>
    /// 立即断开 WebSocket 连接并停止接收任务。
    /// </summary>
    public void Disconnect()
    {
        try
        {
            cancellationTokenSource?.Cancel();
            if (webSocket != null && webSocket.State == WebSocketState.Open)
            {
                webSocket.Abort();
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[InteractiveBarrageClient] Disconnect failed: {e.Message}");
        }
        finally
        {
            DisposeSocket();
            SetQrCodeVisible(false);
        }
    }

    /// <summary>
    /// 构造 Unity 端 WebSocket 地址。
    /// role=unity 表示当前连接是屏幕接收端，服务器只会把弹幕广播给同 session 的 unity 连接。
    /// </summary>
    private Uri BuildUnityWebSocketUri()
    {
        string scheme = useHttps ? "wss" : "ws";
        string url = $"{scheme}://{GetServerHost()}:{serverPort}/ws?role=unity&session={Uri.EscapeDataString(sessionId)}";
        return new Uri(url);
    }

    /// <summary>
    /// 构造手机访问网页地址，用于二维码生成。
    /// </summary>
    private string BuildWebPageUrl()
    {
        if (!string.IsNullOrWhiteSpace(webPageUrlOverride))
        {
            return webPageUrlOverride.Replace("{session}", Uri.EscapeDataString(sessionId));
        }

        string scheme = useHttps ? "https" : "http";
        return $"{scheme}://{GetServerHost()}:{serverPort}/?session={Uri.EscapeDataString(sessionId)}";
    }

    /// <summary>
    /// 获取服务器主机名。serverIp 为空时使用本机地址兜底。
    /// </summary>
    private string GetServerHost()
    {
        return string.IsNullOrWhiteSpace(serverIp) ? "127.0.0.1" : serverIp.Trim();
    }

    /// <summary>
    /// 确保二维码根节点和 RawImage 存在。
    /// 优先使用 prefab 中的 p_qr_code；缺少 RawImage 时可自动创建子节点。
    /// </summary>
    private void EnsureQrCodeRootAndImage()
    {
        if (qrCodeRoot)
        {
            if (!qrCodeImage)
            {
                qrCodeImage = qrCodeRoot.GetComponentInChildren<RawImage>(true);
            }

            if (!qrCodeImage)
            {
                qrCodeImage = qrCodeRoot.GetComponent<RawImage>();
            }

            if (!qrCodeImage)
            {
                qrCodeImage = CreateQrCodeImageUnderRoot(qrCodeRoot.transform);
            }

            ConfigureQrCodeImage(false);
            return;
        }

        if (qrCodeImage)
        {
            qrCodeRoot = qrCodeImage.gameObject;
            ConfigureQrCodeImage(false);
            return;
        }

        if (!createQrCodeImageIfMissing)
        {
            return;
        }

        Canvas canvas = GetComponentInParent<Canvas>();
        if (!canvas)
        {
            GameObject canvasObject = new GameObject("BarrageQrCodeCanvas");
            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            canvasObject.AddComponent<GraphicRaycaster>();
        }

        GameObject qrCodeObject = new GameObject("p_qr_code");
        qrCodeObject.transform.SetParent(canvas.transform, false);
        qrCodeRoot = qrCodeObject;
        qrCodeImage = CreateQrCodeImageUnderRoot(qrCodeObject.transform);
        ConfigureQrCodeImage(true);
        SetQrCodeVisible(false);
    }

    /// <summary>
    /// 在指定根节点下创建一个铺满父节点的 RawImage，用于承载二维码贴图。
    /// </summary>
    private RawImage CreateQrCodeImageUnderRoot(Transform root)
    {
        GameObject imageObject = new GameObject("p_qr_code_image");
        imageObject.transform.SetParent(root, false);
        RawImage image = imageObject.AddComponent<RawImage>();

        RectTransform rectTransform = image.rectTransform;
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        return image;
    }

    /// <summary>
    /// 配置二维码 RawImage 的通用显示属性。
    /// 如果是 prefab 已有布局，不强制覆盖 RectTransform；如果是自动创建节点，则应用默认布局。
    /// </summary>
    private void ConfigureQrCodeImage(bool applyDefaultLayout)
    {
        if (!qrCodeImage)
        {
            return;
        }

        qrCodeImage.raycastTarget = false;
        qrCodeImage.color = Color.white;

        if (!applyDefaultLayout)
        {
            return;
        }

        RectTransform rectTransform = qrCodeImage.rectTransform;
        rectTransform.anchorMin = new Vector2(1f, 1f);
        rectTransform.anchorMax = new Vector2(1f, 1f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = qrCodeSize;
        rectTransform.anchoredPosition = qrCodeAnchoredPosition;
    }

    /// <summary>
    /// 清理当前二维码贴图，防止重复生成时纹理泄漏。
    /// </summary>
    private void ClearQrCodeTexture()
    {
        if (qrCodeImage)
        {
            qrCodeImage.texture = null;
        }

        qrCodeLoaded = false;

        if (qrCodeTexture)
        {
            Destroy(qrCodeTexture);
            qrCodeTexture = null;
        }
    }

    /// <summary>
    /// 控制二维码节点显示隐藏。
    /// </summary>
    private void SetQrCodeVisible(bool visible)
    {
        if (qrCodeRoot)
        {
            qrCodeRoot.SetActive(visible);
        }
        else if (qrCodeImage)
        {
            qrCodeImage.gameObject.SetActive(visible);
        }
    }

    /// <summary>
    /// WebSocket 后台接收循环。
    /// 注意：这个方法不直接操作 Unity UI，只把收到的 JSON 放入 pendingPayloads，
    /// 后续由主线程 TickInterval 调用 ProcessPendingPayloads 处理。
    /// </summary>
    private async Task ReceiveLoop(ClientWebSocket socket, CancellationToken token)
    {
        byte[] buffer = new byte[4096];

        try
        {
            while (!token.IsCancellationRequested && socket.State == WebSocketState.Open)
            {
                using MemoryStream stream = new MemoryStream();
                WebSocketReceiveResult result;
                do
                {
                    result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), token).ConfigureAwait(false);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        ScheduleReconnect();
                        return;
                    }

                    stream.Write(buffer, 0, result.Count);
                } while (!result.EndOfMessage);

                string payload = Encoding.UTF8.GetString(stream.ToArray());
                lock (pendingLock)
                {
                    pendingPayloads.Enqueue(payload);
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception e)
        {
            if (!isQuitting)
            {
                Debug.LogWarning($"[InteractiveBarrageClient] Receive failed: {e.Message}");
                ScheduleReconnect();
            }
        }
    }

    /// <summary>
    /// 主线程处理后台收到的服务器消息。
    /// 每次最多处理 maxMessagesPerFrame 条，避免单帧处理过多 JSON 导致卡顿。
    /// </summary>
    private void ProcessPendingPayloads()
    {
        int count = 0;
        while (count < maxMessagesPerFrame)
        {
            string payload;
            lock (pendingLock)
            {
                if (pendingPayloads.Count == 0)
                {
                    break;
                }

                payload = pendingPayloads.Dequeue();
            }

            HandleServerPayload(payload);
            count++;
        }
    }

    /// <summary>
    /// 解析服务器推送包并按 type 分发。
    /// 当前支持：
    /// - barrage：新增弹幕。
    /// - clear：清屏。
    /// - hello：服务器握手确认。
    /// - error：服务器错误提示。
    /// </summary>
    private void HandleServerPayload(string payload)
    {
        InteractiveBarrageEnvelope envelope;
        try
        {
            envelope = JsonUtility.FromJson<InteractiveBarrageEnvelope>(payload);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[InteractiveBarrageClient] Invalid payload: {e.Message}, {payload}");
            return;
        }

        if (envelope == null || string.IsNullOrEmpty(envelope.type))
        {
            return;
        }

        switch (envelope.type)
        {
            case "barrage":
                AddRecord(envelope.message);
                barrageDisplay?.EnqueueBarrage(envelope.message);
                break;
            case "clear":
                localRecords.Clear();
                barrageDisplay?.Clear();
                break;
            case "hello":
                Debug.Log($"[InteractiveBarrageClient] Server hello, session={envelope.sessionId}");
                break;
            case "error":
                Debug.LogWarning($"[InteractiveBarrageClient] Server error: {envelope.error}");
                break;
        }
    }

    /// <summary>
    /// 把弹幕消息加入本地记录，并按 maxLocalRecords 裁剪旧记录。
    /// </summary>
    private void AddRecord(InteractiveBarrageMessage message)
    {
        if (message == null || string.IsNullOrEmpty(message.content))
        {
            return;
        }

        localRecords.Add(message);
        while (localRecords.Count > maxLocalRecords)
        {
            localRecords.RemoveAt(0);
        }
    }

    /// <summary>
    /// 安排下一次重连。
    /// 该方法可能从后台接收循环触发，所以只设置标志，具体时间计算放在主线程 Tick 中执行。
    /// </summary>
    private void ScheduleReconnect()
    {
        DisposeSocket();
        qrCodeLoaded = false;
        pendingQrCodeHide = true;
        pendingReconnectSchedule = true;
    }

    /// <summary>
    /// 释放 WebSocket 和取消令牌资源。
    /// </summary>
    private void DisposeSocket()
    {
        try
        {
            webSocket?.Dispose();
        }
        catch
        {
        }

        webSocket = null;

        try
        {
            cancellationTokenSource?.Dispose();
        }
        catch
        {
        }

        cancellationTokenSource = null;
    }
}

/// <summary>
/// Unity 本地使用的弹幕消息结构。
/// 字段名与后端 JSON 保持一致，方便 JsonUtility 直接反序列化。
/// </summary>
[Serializable]
public class InteractiveBarrageMessage
{
    public string id;
    public string sessionId;
    public string userId;
    public string nickname;
    public string content;
    public string createdAt;
}

/// <summary>
/// 后端 WebSocket 推送包。
/// type 决定消息语义；type=barrage 时 message 有效，type=clear 时用于清屏。
/// </summary>
[Serializable]
public class InteractiveBarrageEnvelope
{
    public string type;
    public string sessionId;
    public string role;
    public string userId;
    public string error;
    public InteractiveBarrageMessage message;
}
