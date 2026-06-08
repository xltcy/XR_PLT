using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using TickSystem;
using UnityEngine;

public enum PptCommand
{
    Next,
    Previous,
    First,
    Last,
    Goto,
    Start,
    End
}

[Serializable]
public class PptRemoteMessage
{
    public string command = "next";
    public int slideNumber = -1;
    public long timestamp;
}

[Serializable]
public class PptRemoteDiscoveryMessage
{
    public string service;
    public int port;
    public string machine;
    public long timestamp;
}

public enum PptRemoteConnectionState
{
    Disconnected,
    Searching,
    Connected
}

public class PptRemoteController : BaseController, ITickerUpdate
{
    private const string DiscoveryServiceName = "XR_PLT_PPT_REMOTE";

    [Header("Windows PPT Receiver")]
    [SerializeField] private string windowsHost = "192.168.1.100";
    [SerializeField] private int windowsPort = 3414;

    [Header("Discovery")]
    [SerializeField] private bool enableDiscovery = true;
    [SerializeField] private int discoveryPort = 3415;
    [SerializeField] private float connectionTimeoutSeconds = 5f;

    private UdpClient udpClient;
    private UdpClient discoveryClient;
    private IPEndPoint remoteEndPoint;
    private readonly object discoveryLock = new object();
    private IPEndPoint pendingDiscoveredEndPoint;
    private DateTime lastDiscoveryTimeUtc = DateTime.MinValue;
    private bool isSearching;

    public override void OnRegister()
    {
        base.OnRegister();

        udpClient = new UdpClient();
        SetRemoteEndPoint(windowsHost, windowsPort, false);

        if (enableDiscovery)
        {
            StartDiscovery();
        }

        this.AddEventListener(EventConstant.PPT_CONTROL, OnPptControlEvent);
        TickController.RegisterTick(this);
    }

    public override void OnUnregister()
    {
        TickController.UnRegisterTick(this);
        this.RemoveEventListener(EventConstant.PPT_CONTROL, OnPptControlEvent);
        discoveryClient?.Close();
        discoveryClient = null;
        udpClient?.Close();
        udpClient = null;

        base.OnUnregister();
    }

    public void Tick()
    {
        IPEndPoint discoveredEndPoint = null;
        lock (discoveryLock)
        {
            if (pendingDiscoveredEndPoint != null)
            {
                discoveredEndPoint = pendingDiscoveredEndPoint;
                pendingDiscoveredEndPoint = null;
            }
        }

        if (discoveredEndPoint != null)
        {
            SetRemoteEndPoint(discoveredEndPoint.Address.ToString(), discoveredEndPoint.Port, true);
            lastDiscoveryTimeUtc = DateTime.UtcNow;
            isSearching = false;
        }
    }

    public void RefreshConnection()
    {
        lastDiscoveryTimeUtc = DateTime.MinValue;
        isSearching = true;

        if (enableDiscovery && discoveryClient == null)
        {
            StartDiscovery();
        }

        Debug.Log($"[PptRemoteController] Refresh PPT connection. State: {GetConnectionState()}, target: {GetConnectionDescription()}");
    }

    public PptRemoteConnectionState GetConnectionState()
    {
        if (!enableDiscovery)
        {
            return remoteEndPoint != null ? PptRemoteConnectionState.Connected : PptRemoteConnectionState.Disconnected;
        }

        if (lastDiscoveryTimeUtc != DateTime.MinValue)
        {
            double elapsedSeconds = (DateTime.UtcNow - lastDiscoveryTimeUtc).TotalSeconds;
            if (elapsedSeconds <= connectionTimeoutSeconds)
            {
                return PptRemoteConnectionState.Connected;
            }
        }

        return isSearching ? PptRemoteConnectionState.Searching : PptRemoteConnectionState.Disconnected;
    }

    public bool IsConnected()
    {
        return GetConnectionState() == PptRemoteConnectionState.Connected;
    }

    public string GetConnectionDescription()
    {
        string endpoint = remoteEndPoint != null ? $"{remoteEndPoint.Address}:{remoteEndPoint.Port}" : "none";
        return $"{GetConnectionState()} ({endpoint})";
    }

    public void SendNext()
    {
        SendCommand(PptCommand.Next);
    }

    public void SendPrevious()
    {
        SendCommand(PptCommand.Previous);
    }

    public void SendCommand(PptCommand command, int slideNumber = -1)
    {
        SendCommand(command.ToString().ToLowerInvariant(), slideNumber);
    }

    public void SendCommand(string command, int slideNumber = -1)
    {
        if (udpClient == null || remoteEndPoint == null)
        {
            Debug.LogWarning("[PptRemoteController] UDP client is not initialized.");
            return;
        }

        var message = new PptRemoteMessage
        {
            command = string.IsNullOrEmpty(command) ? "next" : command.ToLowerInvariant(),
            slideNumber = slideNumber,
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };

        string json = JsonUtility.ToJson(message);
        byte[] bytes = Encoding.UTF8.GetBytes(json);
        udpClient.Send(bytes, bytes.Length, remoteEndPoint);

        Debug.Log($"[PptRemoteController] Sent PPT command: {json}");
    }

    private void StartDiscovery()
    {
        try
        {
            discoveryClient = new UdpClient(discoveryPort);
            discoveryClient.BeginReceive(OnDiscoveryReceived, null);
            isSearching = true;
            Debug.Log($"[PptRemoteController] Listening for PPT receiver discovery on UDP port {discoveryPort}.");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[PptRemoteController] Failed to start discovery: {e.Message}");
        }
    }

    private void OnDiscoveryReceived(IAsyncResult result)
    {
        if (discoveryClient == null) return;

        try
        {
            IPEndPoint senderEndPoint = new IPEndPoint(IPAddress.Any, 0);
            byte[] bytes = discoveryClient.EndReceive(result, ref senderEndPoint);
            string json = Encoding.UTF8.GetString(bytes);
            var message = JsonUtility.FromJson<PptRemoteDiscoveryMessage>(json);

            if (message != null && message.service == DiscoveryServiceName && message.port > 0)
            {
                lock (discoveryLock)
                {
                    pendingDiscoveredEndPoint = new IPEndPoint(senderEndPoint.Address, message.port);
                }
            }
        }
        catch (ObjectDisposedException)
        {
            return;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[PptRemoteController] Discovery receive failed: {e.Message}");
        }
        finally
        {
            try
            {
                discoveryClient?.BeginReceive(OnDiscoveryReceived, null);
            }
            catch
            {
                // Ignore shutdown races.
            }
        }
    }

    private void SetRemoteEndPoint(string host, int port, bool discovered)
    {
        if (string.IsNullOrEmpty(host)) return;

        try
        {
            var address = IPAddress.Parse(host);
            if (remoteEndPoint != null && remoteEndPoint.Address.Equals(address) && remoteEndPoint.Port == port)
            {
                return;
            }

            remoteEndPoint = new IPEndPoint(address, port);
            windowsHost = host;
            windowsPort = port;

            string source = discovered ? "discovered" : "configured";
            Debug.Log($"[PptRemoteController] PPT receiver {source}: {windowsHost}:{windowsPort}");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[PptRemoteController] Invalid PPT receiver endpoint {host}:{port}. {e.Message}");
        }
    }

    private void OnPptControlEvent(EventData eventData)
    {
        if (eventData == null) return;

        var programEventData = eventData.GetData<SceneController.ProgramEventData>();
        var pptParam = programEventData?.actionData?.eventData as ProgramEvent.PptControlEventParam;
        if (pptParam != null)
        {
            SendCommand(pptParam.command, pptParam.slideNumber);
            return;
        }

        if (eventData.Data is PptRemoteMessage message)
        {
            SendCommand(message.command, message.slideNumber);
            return;
        }

        if (eventData.Data is PptCommand command)
        {
            SendCommand(command);
            return;
        }

        if (eventData.Data is string commandText)
        {
            SendCommand(commandText);
            return;
        }

        SendCommand(PptCommand.Next);
    }
}
