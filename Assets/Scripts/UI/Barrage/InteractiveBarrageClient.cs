using System;
using System.Collections;
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
/// 互动弹幕 WebSocket 客户端。
/// 手机网页向 Ubuntu 弹幕服务器发送消息后，服务器会把同一 session 的消息广播给 role=unity 的连接。
/// 本组件负责连接服务器、解析消息、保存最近收到的记录，并把消息推给 BarrageDisplay 播放。
/// </summary>
public class InteractiveBarrageClient : BaseStateComponent, ITickerInterval
{
    [Header("连接配置")]
    [SerializeField] private string serverIp = "10.243.57.216";
    [SerializeField] private int serverPort = 37621;
    [SerializeField] private bool useHttps;
    [SerializeField] private string sessionId = "default";
    [SerializeField] private bool connectOnStart = true;
    [SerializeField] private float reconnectDelay = 3f;
    [SerializeField] private float tickInterval = 1f;

    [Header("显示与记录")]
    [SerializeField] private BarrageDisplay barrageDisplay;
    [SerializeField] private int maxLocalRecords = 500;
    [SerializeField] private int maxMessagesPerFrame = 20;

    [Header("二维码")]
    [BindChild("p_qr_code")]
    [SerializeField] private GameObject qrCodeRoot;
    [SerializeField] private RawImage qrCodeImage;
    [SerializeField] private bool loadQrCodeOnStart = true;
    [SerializeField] private bool createQrCodeImageIfMissing = true;
    [SerializeField] private Vector2 qrCodeSize = new Vector2(220f, 220f);
    [SerializeField] private Vector2 qrCodeAnchoredPosition = new Vector2(-140f, -140f);
    [SerializeField] private string webPageUrlOverride;

    private readonly Queue<string> pendingPayloads = new Queue<string>();
    private readonly List<InteractiveBarrageMessage> localRecords = new List<InteractiveBarrageMessage>();
    private readonly object pendingLock = new object();

    private ClientWebSocket webSocket;
    private CancellationTokenSource cancellationTokenSource;
    private float nextReconnectTime;
    private bool isConnecting;
    private bool isQuitting;
    private Texture2D qrCodeTexture;
    private bool qrCodeLoaded;
    private volatile bool pendingQrCodeRefresh;
    private volatile bool pendingQrCodeHide;
    private volatile bool pendingReconnectSchedule;

    public IReadOnlyList<InteractiveBarrageMessage> LocalRecords => localRecords;
    public string SessionId => sessionId;
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
    /// 修改服务器 IP、端口和 session 后重新连接。
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
    /// 重新从服务器下载二维码图片。
    /// 二维码内容由服务端生成，指向当前 session 的手机弹幕网页。
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
    /// 主动建立 WebSocket 连接。连接成功后后台任务会持续接收服务器推送。
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
    /// 立即断开连接并停止后台接收任务。
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

    private Uri BuildUnityWebSocketUri()
    {
        string scheme = useHttps ? "wss" : "ws";
        string url = $"{scheme}://{GetServerHost()}:{serverPort}/ws?role=unity&session={Uri.EscapeDataString(sessionId)}";
        return new Uri(url);
    }

    private string BuildWebPageUrl()
    {
        if (!string.IsNullOrWhiteSpace(webPageUrlOverride))
        {
            return webPageUrlOverride.Replace("{session}", Uri.EscapeDataString(sessionId));
        }

        string scheme = useHttps ? "https" : "http";
        return $"{scheme}://{GetServerHost()}:{serverPort}/?session={Uri.EscapeDataString(sessionId)}";
    }

    private string GetServerHost()
    {
        return string.IsNullOrWhiteSpace(serverIp) ? "127.0.0.1" : serverIp.Trim();
    }

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

    private void ScheduleReconnect()
    {
        DisposeSocket();
        qrCodeLoaded = false;
        pendingQrCodeHide = true;
        pendingReconnectSchedule = true;
    }

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
/// Unity 本地记录的弹幕消息。字段名与服务器 JSON 保持一致，便于 JsonUtility 直接反序列化。
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
/// 服务器推送包。type=barrage 时 message 有效，type=clear 时用于清屏。
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
