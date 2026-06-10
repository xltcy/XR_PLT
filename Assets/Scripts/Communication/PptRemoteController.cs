using System;
using System.Net;
using System.Net.NetworkInformation;
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
    public string service;
    public int replyPort = -1;
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

    private static readonly bool EnableSubnetDiscovery = true;
    private const int SubnetDiscoveryStart = 1;
    private const int SubnetDiscoveryEnd = 254;

    private UdpClient udpClient;
    private UdpClient discoveryClient;
    private IPEndPoint remoteEndPoint;
    private readonly object discoveryLock = new object();
    private IPEndPoint pendingDiscoveredEndPoint;
    private DateTime lastDiscoveryTimeUtc = DateTime.MinValue;
    private bool isSearching;
    private bool hasNotifiedConnectionState;
    private PptRemoteConnectionState notifiedConnectionState = PptRemoteConnectionState.Disconnected;

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
            lastDiscoveryTimeUtc = DateTime.UtcNow;
            isSearching = false;
            SetRemoteEndPoint(discoveredEndPoint.Address.ToString(), discoveredEndPoint.Port, true);
        }

        NotifyConnectionStateChanged();
    }

    public void RefreshConnection()
    {
        lastDiscoveryTimeUtc = DateTime.MinValue;
        isSearching = true;

        if (enableDiscovery && discoveryClient == null)
        {
            StartDiscovery();
        }

        SendDiscoveryProbe();
        NotifyConnectionStateChanged();
        Debug.Log($"[PptRemoteController] Refresh PPT connection. State: {GetConnectionState()}, target: {GetConnectionDescription()}");
    }

    public void RefreshConnection(string host)
    {
        ConfigureRemoteHost(host);
        RefreshConnection();
    }

    public void ConfigureRemoteHost(string host, int port = -1)
    {
        if (string.IsNullOrWhiteSpace(host)) return;

        int targetPort = port > 0 ? port : windowsPort;
        SetRemoteEndPoint(host.Trim(), targetPort, false);
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

    private void NotifyConnectionStateChanged()
    {
        PptRemoteConnectionState state = GetConnectionState();
        if (hasNotifiedConnectionState && notifiedConnectionState == state)
        {
            return;
        }

        hasNotifiedConnectionState = true;
        notifiedConnectionState = state;
        this.TriggerEvent(EventConstant.PPT_REMOTE_CONNECTION_CHANGED, state);
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

    private void SendDiscoveryProbe()
    {
        if (udpClient == null) return;

        var message = new PptRemoteMessage
        {
            service = DiscoveryServiceName,
            command = "discover",
            replyPort = discoveryPort,
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };

        string json = JsonUtility.ToJson(message);
        byte[] bytes = Encoding.UTF8.GetBytes(json);

        try
        {
            udpClient.EnableBroadcast = true;
            udpClient.Send(bytes, bytes.Length, new IPEndPoint(IPAddress.Broadcast, windowsPort));
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[PptRemoteController] Broadcast discovery probe failed: {e.Message}");
        }

        if (remoteEndPoint != null)
        {
            try
            {
                udpClient.Send(bytes, bytes.Length, remoteEndPoint);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[PptRemoteController] Direct discovery probe failed: {e.Message}");
            }
        }

        foreach (IPAddress broadcastAddress in GetLocalBroadcastAddresses())
        {
            try
            {
                udpClient.Send(bytes, bytes.Length, new IPEndPoint(broadcastAddress, windowsPort));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[PptRemoteController] Local broadcast discovery probe failed ({broadcastAddress}): {e.Message}");
            }
        }

        if (!EnableSubnetDiscovery) return;

        foreach (string prefix in GetLocalSubnetPrefixes())
        {
            int start = Mathf.Clamp(SubnetDiscoveryStart, 1, 254);
            int end = Mathf.Clamp(SubnetDiscoveryEnd, start, 254);
            for (int i = start; i <= end; i++)
            {
                try
                {
                    udpClient.Send(bytes, bytes.Length, new IPEndPoint(IPAddress.Parse($"{prefix}.{i}"), windowsPort));
                }
                catch
                {
                    // Ignore unreachable or unsupported addresses during a manual scan.
                }
            }
        }
    }

    private static IPAddress[] GetLocalBroadcastAddresses()
    {
        try
        {
            NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();
            var addresses = new System.Collections.Generic.List<IPAddress>();

            foreach (NetworkInterface networkInterface in interfaces)
            {
                if (networkInterface.OperationalStatus != OperationalStatus.Up)
                {
                    continue;
                }

                IPInterfaceProperties properties = networkInterface.GetIPProperties();
                foreach (UnicastIPAddressInformation unicastAddress in properties.UnicastAddresses)
                {
                    IPAddress address = unicastAddress.Address;
                    IPAddress mask = unicastAddress.IPv4Mask;
                    if (address.AddressFamily != AddressFamily.InterNetwork || mask == null || IsIgnoredLocalAddress(address))
                    {
                        continue;
                    }

                    byte[] ipBytes = address.GetAddressBytes();
                    byte[] maskBytes = mask.GetAddressBytes();
                    byte[] broadcastBytes = new byte[ipBytes.Length];
                    for (int i = 0; i < ipBytes.Length; i++)
                    {
                        broadcastBytes[i] = (byte)(ipBytes[i] | ~maskBytes[i]);
                    }

                    IPAddress broadcastAddress = new IPAddress(broadcastBytes);
                    if (!addresses.Contains(broadcastAddress))
                    {
                        addresses.Add(broadcastAddress);
                    }
                }
            }

            return addresses.ToArray();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[PptRemoteController] Failed to get local broadcast addresses: {e.Message}");
            return Array.Empty<IPAddress>();
        }
    }

    private static string[] GetLocalSubnetPrefixes()
    {
        try
        {
            NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();
            var prefixes = new System.Collections.Generic.List<string>();

            foreach (NetworkInterface networkInterface in interfaces)
            {
                if (networkInterface.OperationalStatus != OperationalStatus.Up)
                {
                    continue;
                }

                IPInterfaceProperties properties = networkInterface.GetIPProperties();
                foreach (UnicastIPAddressInformation unicastAddress in properties.UnicastAddresses)
                {
                    IPAddress address = unicastAddress.Address;
                    if (address.AddressFamily != AddressFamily.InterNetwork || IsIgnoredLocalAddress(address))
                    {
                        continue;
                    }

                    string[] parts = address.ToString().Split('.');
                    if (parts.Length != 4)
                    {
                        continue;
                    }

                    string prefix = $"{parts[0]}.{parts[1]}.{parts[2]}";
                    if (!prefixes.Contains(prefix))
                    {
                        prefixes.Add(prefix);
                    }
                }
            }

            return prefixes.ToArray();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[PptRemoteController] Failed to get local subnet prefixes: {e.Message}");
            return Array.Empty<string>();
        }
    }

    private static bool IsIgnoredLocalAddress(IPAddress address)
    {
        string text = address.ToString();
        return IPAddress.IsLoopback(address) || text.StartsWith("169.254.", StringComparison.Ordinal);
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
        if (programEventData != null && !programEventData.isStartAction)
        {
            return;
        }

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
