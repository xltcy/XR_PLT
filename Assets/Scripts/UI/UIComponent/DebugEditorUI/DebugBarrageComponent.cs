using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Debug 面板中的弹幕服务器地址调试组件。
/// 
/// 这个组件只负责“运行时临时切换 Unity 连接的弹幕后端地址”，不负责启动后端服务。
/// 使用方式：
/// 1. 第一次打开 Debug 面板时，把当前 <see cref="InteractiveBarrageClient"/> 正在使用的 IP 填入输入框。
/// 2. 用户修改输入框内容后，点击 refresh ip 按钮。
/// 3. 组件解析输入内容，并调用 <see cref="InteractiveBarrageClient.Configure"/> 重新配置和重连。
/// 
/// 输入格式兼容：
/// - 纯主机：1.2.3.4
/// - 主机加端口：1.2.3.4:37621
/// - 完整 URL：http://1.2.3.4:37621
/// </summary>
public class DebugBarrageComponent : BaseStateComponent
{
    /// <summary>
    /// 刷新服务器 IP 的按钮。
    /// 节点名由 prefab 决定，点击后会触发 <see cref="OnBtnRefreshIpClick"/>。
    /// </summary>
    [BindChild("p_btn_refresh_ip"), ButtonCallback(nameof(OnBtnRefreshIpClick))]
    private Button requestSceneJsonButton;

    /// <summary>
    /// 输入弹幕后端地址的输入框。
    /// 第一次打开组件时会自动填入当前弹幕客户端的服务器 IP。
    /// </summary>
    [BindChild("p_input_field")]
    private InputField inputField;

    /// <summary>
    /// 可选的弹幕客户端引用。
    /// 如果 Inspector 没有手动绑定，运行时会自动在场景对象中查找。
    /// </summary>
    [SerializeField] private InteractiveBarrageClient barrageClient;

    /// <summary>
    /// 防止每次 Debug 面板显示时都覆盖用户刚输入的内容。
    /// 需求是“第一次打开界面时显示当前 IP”，所以只初始化一次。
    /// </summary>
    private bool initializedInput;

    private void OnEnable()
    {
        InitializeInputOnce();
    }

    /// <summary>
    /// refresh ip 按钮回调。
    /// 输入有效时更新弹幕客户端连接配置，并立即断开旧连接、连接新服务器。
    /// </summary>
    private void OnBtnRefreshIpClick()
    {
        InteractiveBarrageClient client = GetBarrageClient();
        if (!client)
        {
            Debug.LogWarning("[DebugBarrageComponent] InteractiveBarrageClient not found.", this);
            return;
        }

        string input = inputField ? inputField.text : string.Empty;
        if (!TryParseServerInput(input, client.ServerPort, out string host, out int port))
        {
            Debug.LogWarning("[DebugBarrageComponent] Barrage server ip is empty or invalid.", this);
            return;
        }

        client.Configure(host, port, client.SessionId, true);

        // 规范化输入框显示：用户输入完整 URL 或 ip:port 后，保留最终解析出的 host。
        if (inputField)
        {
            inputField.text = host;
        }

        Debug.Log($"[DebugBarrageComponent] Barrage server changed to {host}:{port}", this);
    }

    /// <summary>
    /// 第一次显示 Debug 面板时，把当前弹幕服务器 IP 写入输入框。
    /// 如果弹幕客户端尚未创建或输入框未绑定，则保持静默，避免影响其他 Debug UI。
    /// </summary>
    private void InitializeInputOnce()
    {
        if (initializedInput || !inputField)
        {
            return;
        }

        InteractiveBarrageClient client = GetBarrageClient();
        if (!client)
        {
            return;
        }

        inputField.text = client.ServerIp;
        initializedInput = true;
    }

    /// <summary>
    /// 获取场景中的弹幕客户端。
    /// 查找顺序：
    /// 1. 优先使用 Inspector 显式绑定的对象。
    /// 2. 查找当前激活场景中的对象。
    /// 3. 查找 Resources.FindObjectsOfTypeAll 返回的已加载场景对象，兼容隐藏或未激活节点。
    /// </summary>
    private InteractiveBarrageClient GetBarrageClient()
    {
        if (barrageClient)
        {
            return barrageClient;
        }

        barrageClient = FindObjectOfType<InteractiveBarrageClient>();
        if (barrageClient)
        {
            return barrageClient;
        }

        InteractiveBarrageClient[] clients = Resources.FindObjectsOfTypeAll<InteractiveBarrageClient>();
        foreach (InteractiveBarrageClient client in clients)
        {
            if (client && client.gameObject.scene.IsValid())
            {
                barrageClient = client;
                return barrageClient;
            }
        }

        return null;
    }

    /// <summary>
    /// 解析用户输入的服务器地址。
    /// 
    /// 这里把“地址”和“端口”分开返回，是因为 <see cref="InteractiveBarrageClient"/>
    /// 内部仍然用 serverIp + serverPort 组合 WebSocket URL。
    /// 
    /// 如果用户没有输入端口，就沿用当前客户端端口，避免误把 37621 改回 HTTP 默认 80。
    /// </summary>
    private bool TryParseServerInput(string input, int defaultPort, out string host, out int port)
    {
        host = string.Empty;
        port = defaultPort;

        string text = string.IsNullOrWhiteSpace(input) ? string.Empty : input.Trim();
        if (string.IsNullOrEmpty(text))
        {
            return false;
        }

        // 已经是完整 URL，例如 http://1.2.3.4:37621。
        if (Uri.TryCreate(text, UriKind.Absolute, out Uri absoluteUri))
        {
            host = absoluteUri.Host;
            if (!absoluteUri.IsDefaultPort)
            {
                port = absoluteUri.Port;
            }

            return !string.IsNullOrWhiteSpace(host);
        }

        // 兼容用户只输入 1.2.3.4:37621 的情况。
        if (Uri.TryCreate($"http://{text}", UriKind.Absolute, out Uri uriWithScheme))
        {
            host = uriWithScheme.Host;
            if (!uriWithScheme.IsDefaultPort)
            {
                port = uriWithScheme.Port;
            }

            return !string.IsNullOrWhiteSpace(host);
        }

        // 理论上前面的分支已经覆盖大多数合法输入；这里保留兜底，避免过度限制。
        host = text;
        return true;
    }
}
