using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// 网络服务管理器 - 单例模式
/// </summary>
public partial class NetworkServiceManager : BaseManager , TickSystem.ITickerUpdate
{
    // 有Tick的Manager都要特别注意初始化时机
    public override ManagerRegister.InitTiming InitTiming => ManagerRegister.InitTiming.OnSceneLoaded;

    [Header("配置")]
    private NetworkConfig config = NetworkConfig.Instance;

    private CoroutineManager coroutineManager = ManagerRefer.CoroutineManager;

    private Queue<Action> _mainThreadActions = new Queue<Action>();
    private readonly object _queueLock = new object();
    private int _activeRequests = 0;
    private string _authToken = "";
    
    // Lockable 相关 - 支持多个不同的 lockable 同时存在
    // key: lockable Transform (null 表示全屏锁定)
    // value: 该 lockable 的活动请求数
    private Dictionary<Transform, int> _lockableRequests = new Dictionary<Transform, int>();
    // 保存每个 lockable 的原始 interactable 状态
    private Dictionary<Transform, bool> _lockableInteractableStates = new Dictionary<Transform, bool>();
    private readonly object _lockableLock = new object();

    #region 事件系统
    public delegate void NetworkEvent(NetworkResponse response);
    public delegate void ResponseEvent(bool result, NetworkResponse response);
    
    // 全局事件
    public event NetworkEvent OnRequestStarted;
    public event NetworkEvent OnRequestCompleted;
    public event NetworkEvent OnRequestFailed;
    public event Action<int> OnActiveRequestsChanged;

    // 请求特定事件（通过requestId监听）
    private Dictionary<string, ResponseEvent> _responseEvents = new Dictionary<string, ResponseEvent>();
    #endregion

    #region 初始化

    public override void OnRegister()
    {
        base.OnRegister();

        TickController.RegisterTick(this);
        
        Debug.Log($"[NetworkService] 初始化完成，基础URL: {config.baseUrl}");
    }

    public void Tick()
    {
        // 处理主线程回调
        const int maxActionsPerFrame = 100; // 每帧最多处理100个回调，避免卡顿
        int processedCount = 0;
        
        while (processedCount < maxActionsPerFrame)
        {
            Action action = null;
            
            // 只在出队时加锁，减少锁持有时间
            lock (_queueLock)
            {
                if (_mainThreadActions.Count > 0)
                {
                    action = _mainThreadActions.Dequeue();
                }
            }
            
            // 如果队列为空，退出循环
            if (action == null)
                break;
            
            // 在锁外执行回调，避免阻塞其他线程
            try
            {
                action.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogError($"主线程回调执行失败: {e}");
            }
            
            processedCount++;
        }
        
        // 如果还有未处理的任务，下一帧继续处理
        if (_mainThreadActions.Count > 0 && processedCount >= maxActionsPerFrame)
        {
            Debug.LogWarning($"[NetworkService] 主线程回调队列积压，剩余 {_mainThreadActions.Count} 个任务");
        }
    }

    public override void OnUnregister()
    {
        base.OnUnregister();
        
        TickController.UnRegisterTick(this);
        
        ClearAllRequests();
    }

    #endregion

    #region 公共方法

    /// <summary>
    /// 发送HTTP请求
    /// </summary>
    /// <param name="baseRequestParam">请求参数</param>
    /// <param name="showLoading">是否显示加载圈，false 表示后台静默请求</param>
    /// <param name="lockable">需要锁定的 Transform，null 表示全屏锁定</param>
    /// <param name="callback">请求完成回调</param>
    public string SendRequest(BaseRequestParam baseRequestParam, bool showLoading = true, Transform lockable = null,
        ResponseEvent callback = null)
    {
        string requestId = Guid.NewGuid().ToString();
        
        // 启动协程处理请求
        coroutineManager.StartManagedCoroutine(ProcessRequest(baseRequestParam, showLoading, lockable, callback), this);
        
        return requestId;
    }
    
    /// <summary>
    /// 清除所有请求
    /// </summary>
    private void ClearAllRequests()
    {
        coroutineManager.StopManagedCoroutines(this);
        _activeRequests = 0;
        NotifyActiveRequestsChanged();
    }

    /// <summary>
    /// 添加请求事件监听
    /// </summary>
    public void AddResponseListener(string networkConstant, ResponseEvent listener)
    {
        if (!_responseEvents.ContainsKey(networkConstant))
        {
            _responseEvents[networkConstant] = null;
        }
        _responseEvents[networkConstant] += listener;
    }

    /// <summary>
    /// 移除请求事件监听
    /// </summary>
    public void RemoveResponseListener(string requestId, ResponseEvent listener)
    {
        if (_responseEvents.ContainsKey(requestId))
        {
            _responseEvents[requestId] -= listener;
        }
    }
    
    
    public string BuildUrl(string endpoint)
    {
        if (endpoint.StartsWith("http://") || endpoint.StartsWith("https://"))
            return endpoint;
        
        return $"{config.baseUrl.TrimEnd('/')}/{endpoint.TrimStart('/')}";
    }

    public string BuildFullUrl(string baseUrl, Dictionary<string, string> queryParams)
    {
        if (queryParams == null || queryParams.Count == 0)
            return baseUrl;

        var queryBuilder = new System.Text.StringBuilder(baseUrl.TrimEnd('/'));
        queryBuilder.Append("/?");

        bool isFirst = true;
        foreach (var param in queryParams)
        {
            if (!isFirst) queryBuilder.Append("&");
            queryBuilder.Append($"{UnityWebRequest.EscapeURL(param.Key)}={UnityWebRequest.EscapeURL(param.Value)}");
            isFirst = false;
        }

        return queryBuilder.ToString();
    }
    #endregion

    #region 私有方法
    private IEnumerator ProcessRequest(BaseRequestParam baseRequestParam, bool showLoading = true,
        Transform lockable = null, ResponseEvent callback = null)
    {
        yield return ExecuteRequestWithRetry(baseRequestParam, showLoading, lockable, callback);
    }

    /// <summary>
    /// 网络请求处理器（包含重试逻辑）
    /// </summary>
    private IEnumerator ExecuteRequestWithRetry(BaseRequestParam baseRequestParam, bool showLoading = true,
        Transform lockable = null, ResponseEvent callback = null)
    {
        _activeRequests++;
        NotifyActiveRequestsChanged();
        
        // 处理 lockable 加载状态
        if (showLoading)
        {
            SetLockableLoadingStatus(lockable, true);
        }

        long totalStartTime = DateTime.Now.Ticks;
        NetworkResponse finalResponse = null;
        
        try
        {
            // 触发开始事件
            OnRequestStarted?.Invoke(new NetworkResponse
            {
                localData = baseRequestParam.localData, 
            });
            
            int attempt = 0;
            bool shouldRetry = true;
            
            while (shouldRetry && attempt <= (baseRequestParam.retryOnFailure ? config.maxRetries : 0))
            {
                attempt++;
                
                if (attempt > 1)
                {
                    Debug.LogWarning($"[NetworkService] 请求重试 ({attempt - 1}/{config.maxRetries}): {baseRequestParam.url}");
                    yield return new WaitForSeconds(config.retryDelay);
                }
                
                // 执行单次请求
                var attempt1 = attempt;
                yield return ExecuteSingleRequest(baseRequestParam, response =>
                {
                    finalResponse = response;
                
                    LogResponse(response);
                
                    if (response.success)
                    {
                        shouldRetry = false; // 成功，停止重试
                    }
                    else
                    {
                        // 判断是否应该继续重试
                        shouldRetry = attempt1 <= config.maxRetries && 
                                      baseRequestParam.retryOnFailure &&
                                      IsRetryableError(response.statusCode);
                    
                        if (!shouldRetry)
                        {
                            // Debug.LogError($"[NetworkService] 请求失败，停止重试: {response.error}");
                        }
                    }
                });
            }
        }
        finally
        {
            // 确保请求计数减少
            _activeRequests--;
            NotifyActiveRequestsChanged();
            
            // 处理 lockable 加载状态
            if (showLoading)
            {
                SetLockableLoadingStatus(lockable, false);
            }
            
            // 计算总时间
            if (finalResponse != null)
            {
                finalResponse.responseTime = (DateTime.Now.Ticks - totalStartTime) / TimeSpan.TicksPerMillisecond;
            }
            
            // 执行最终回调
            if (finalResponse != null)
            {
                InvokeCallback(baseRequestParam.networkConstant, finalResponse, callback);
            }
            else
            {
                // 创建错误响应
                var errorResponse = new NetworkResponse
                {
                    success = false,
                    error = "请求执行异常",
                    localData = baseRequestParam.localData,
                    responseTime = (DateTime.Now.Ticks - totalStartTime) / TimeSpan.TicksPerMillisecond,
                };
                InvokeCallback(baseRequestParam.networkConstant, errorResponse, callback);
            }
        }
    }

    /// <summary>
    /// 执行单次请求（不包含重试）
    /// </summary>
    private IEnumerator ExecuteSingleRequest(BaseRequestParam baseRequestParam, Action<NetworkResponse> onComplete)
    {
        long startTime = DateTime.Now.Ticks;
        var response = new NetworkResponse
        {
            localData = baseRequestParam.localData,
        };
        string fullUrl = BuildFullUrl(baseRequestParam.url, baseRequestParam.queryParams);
        UnityWebRequest request = CreateUnityWebRequest(fullUrl, baseRequestParam);
        AddHeadersToRequest(request, baseRequestParam.headers);

        if (config.enableLogging)
        {
            LogRequest(request, baseRequestParam);
        }

        // 同步发送请求（简化版本）
        request.timeout = baseRequestParam.timeout;
        var asyncOperation = request.SendWebRequest();

        // 等待请求完成
        while (!asyncOperation.isDone)
        {
            yield return null; // 每帧检查一次，不阻塞主线程

            // 可以在这里添加超时检查
            if ((DateTime.Now.Ticks - startTime) / TimeSpan.TicksPerMillisecond > baseRequestParam.timeout * 1000)
            {
                request.Abort();
                response.success = false;
                response.error = "请求超时";
                break;
            }
        }

        response.request = request;
        response.statusCode = (int)request.responseCode;
        response.success = request.result == UnityWebRequest.Result.Success;
        response.error = request.error;
        response.rawResponse = request.downloadHandler?.text;

        // 确保回调被执行
        onComplete?.Invoke(response);
        
        request.Dispose();
    }

    /// <summary>
    /// 判断错误是否可重试
    /// </summary>
    private bool IsRetryableError(int statusCode)
    {
        // 网络错误、超时、服务器错误可以重试
        return statusCode == 0 || // 网络连接错误
               statusCode == 408 || // 请求超时
               statusCode == 429 || // 请求过多
               statusCode >= 500; // 服务器错误
    }
        
    private UnityWebRequest CreateUnityWebRequest(string url, BaseRequestParam baseRequestParam)
    {
        switch (baseRequestParam.method.ToUpper())
        {
            case "POST":
            case "PUT":
                if (baseRequestParam.FormDataFields != null && baseRequestParam.FormDataFields.Count > 0)
                {
                    // 新的 FormData 请求（支持多种数据类型）
                    return CreateFormDataRequest(url, baseRequestParam.method, baseRequestParam.FormDataFields, baseRequestParam.customBoundary);
                }
                else
                {
                    // JSON请求
                    var request = new UnityWebRequest(url, baseRequestParam.method);
                    if (baseRequestParam.requestData != null)
                    {
                        string json = JsonConvert.SerializeObject(baseRequestParam.requestData);
                        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
                        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                        request.downloadHandler = new DownloadHandlerBuffer();
                        request.SetRequestHeader("Content-Type", "application/json");
                    }
                    return request;
                }
            case "DELETE":
                return UnityWebRequest.Delete(url);

            default: // GET
                return UnityWebRequest.Get(url);
        }
    }
    
    /// <summary>
    /// 创建FormData请求
    /// </summary>
    private UnityWebRequest CreateFormDataRequest(string url, string method, List<FormField> formFields, string customBoundary = null)
    {
        // 如果指定了自定义 boundary，使用 IMultipartFormSection 方式
        if (!string.IsNullOrEmpty(customBoundary))
        {
            List<IMultipartFormSection> multipartSections = new List<IMultipartFormSection>();
            
            foreach (var field in formFields)
            {
                switch (field.Type)
                {
                    case FormFieldType.Text:
                        multipartSections.Add(new MultipartFormDataSection(field.FieldName, field.StringValue));
                        break;
                    
                    case FormFieldType.Binary:
                    case FormFieldType.File:
                        if (field.BinaryValue != null)
                        {
                            multipartSections.Add(new MultipartFormFileSection(
                                field.FieldName,
                                field.BinaryValue,
                                field.FileName ?? "data.bin",
                                field.MimeType ?? FormField.GetMimeType(field.FileName)
                            ));
                        }
                        break;
                }
            }
            
            // 使用自定义 boundary
            byte[] boundaryBytes = System.Text.Encoding.UTF8.GetBytes(customBoundary);
            UnityWebRequest imfRequest = UnityWebRequest.Post(url, multipartSections, boundaryBytes);
            
            // 设置 Content-Type 头
            imfRequest.SetRequestHeader("Content-Type", "multipart/form-data; boundary=" + customBoundary);
            
            // 如果不是 POST，修改方法
            if (method.ToUpper() != "POST")
            {
                imfRequest.method = method.ToUpper();
            }
            
            return imfRequest;
        }
        
        // 默认使用 WWWForm
        WWWForm form = new WWWForm();
    
        foreach (var field in formFields)
        {
            switch (field.Type)
            {
                case FormFieldType.Text:
                    // 添加文本字段
                    form.AddField(field.FieldName, field.StringValue);
                    break;
                
                case FormFieldType.Binary:
                    // 添加二进制数据（作为文件）
                    if (field.BinaryValue != null)
                    {
                        form.AddBinaryData(
                            field.FieldName,
                            field.BinaryValue,
                            field.FileName ?? "data.bin",
                            field.MimeType ?? "application/octet-stream"
                        );
                    }
                    break;
                
                case FormFieldType.File:
                    // 添加文件数据
                    if (field.BinaryValue != null)
                    {
                        form.AddBinaryData(
                            field.FieldName,
                            field.BinaryValue,
                            field.FileName,
                            field.MimeType ?? FormField.GetMimeType(field.FileName)
                        );
                    }
                    break;
            }
        }
    
        // 创建 UnityWebRequest
        UnityWebRequest wwwRequest = UnityWebRequest.Post(url, form);
    
        // 如果不是 POST，修改方法（PUT 等也支持 FormData）
        if (method.ToUpper() != "POST")
        {
            wwwRequest.method = method.ToUpper();
        }
    
        return wwwRequest;
    }
    
    private void AddHeadersToRequest(UnityWebRequest request, Dictionary<string, string> customHeaders)
    {
        // 添加请求特定headers
        if (customHeaders != null)
        {
            foreach (var header in customHeaders)
            {
                request.SetRequestHeader(header.Key, header.Value);
            }
        }
    }

    private void LogResponse(NetworkResponse response)
    {
        if (response.success)
        {
            if (config.enableLogging)
            {
                Debug.Log($"[NetworkService] 请求成功: {response.statusCode}\n响应: {response.rawResponse}");
            }
        }
        else
        {
            Debug.LogError($"[NetworkService] 请求失败: {response.statusCode} - {response.error}");
            if (!string.IsNullOrEmpty(response.rawResponse))
            {
                Debug.LogError($"错误详情: {response.rawResponse}");
            }
        }
    }

    private void InvokeCallback(string networkConstant, NetworkResponse response, ResponseEvent callback = null)
    {
        RunOnMainThread(() =>
        {
            // 触发完成事件
            if (response.success)
            {
                OnRequestCompleted?.Invoke(response);
                TriggerResponseEvent(networkConstant, true, response);
            }
            else
            {
                OnRequestFailed?.Invoke(response);
                TriggerResponseEvent(networkConstant, false, response);
            }

            // 执行回调
            try
            {
                callback?.Invoke(response.success, response);
            }
            catch (Exception e)
            {
                Debug.LogError($"请求回调执行失败: {e}");
            }
        });
    }

    private void TriggerResponseEvent(string networkConstant, bool result, NetworkResponse response)
    {
        if (_responseEvents.ContainsKey(networkConstant))
        {
            try
            {
                _responseEvents[networkConstant]?.Invoke(result, response);
            }
            catch (Exception e)
            {
                Debug.LogError($"请求事件触发失败: {e}");
            }
        }
    }

    private void RunOnMainThread(Action action)
    {
        lock (_queueLock)
        {
            _mainThreadActions.Enqueue(action);
        }
    }

    private void LogRequest(UnityWebRequest request, BaseRequestParam baseRequestParam)
    {
        string log = $"[NetworkService] 发送请求:\n" +
                    $"URL: {request.url}\n" +
                    $"Method: {request.method}\n" +
                    $"Headers: {(request.GetRequestHeader("Authorization") != null ? "有认证" : "无认证")}";

        if (baseRequestParam.requestData != null)
        {
            log += $"\nBody: {JsonUtility.ToJson(baseRequestParam.requestData)}";
        }

        Debug.Log(log);
    }

    private void NotifyActiveRequestsChanged()
    {
        RunOnMainThread(() =>
        {
            OnActiveRequestsChanged?.Invoke(_activeRequests);
        });
    }

    /// <summary>
    /// 设置 Lockable 加载状态
    /// </summary>
    /// <param name="lockable">需要锁定的 Transform，null 表示全屏锁定</param>
    /// <param name="isLoading">是否开始加载</param>
    private void SetLockableLoadingStatus(Transform lockable, bool isLoading)
    {
        // 使用特殊的静态占位符代替 null，因为 Dictionary 不允许 null 键
        Transform key = lockable ?? GetFullScreenLockKey();
        
        lock (_lockableLock)
        {
            if (isLoading)
            {
                // 开始加载
                if (!_lockableRequests.ContainsKey(key))
                {
                    _lockableRequests[key] = 0;
                }
                
                int previousCount = _lockableRequests[key];
                _lockableRequests[key]++;
                
                // 只在该 lockable 的第一个请求时显示加载圈
                if (previousCount == 0)
                {
                    ShowLoadingForLockable(lockable);
                }
            }
            else
            {
                // 结束加载
                if (_lockableRequests.ContainsKey(key))
                {
                    _lockableRequests[key]--;
                    
                    // 当该 lockable 的所有请求都完成时，隐藏加载圈
                    if (_lockableRequests[key] <= 0)
                    {
                        HideLoadingForLockable(lockable);
                        _lockableRequests.Remove(key);
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// 获取全屏锁定的键（避免使用 null）
    /// </summary>
    private static Transform _fullScreenLockKey;
    private static Transform GetFullScreenLockKey()
    {
        if (_fullScreenLockKey == null)
        {
            // 创建一个永久的空 GameObject 作为全屏锁定的键
            var go = new GameObject("__FullScreenLockKey__");
            UnityEngine.Object.DontDestroyOnLoad(go);
            go.hideFlags = HideFlags.HideAndDontSave;
            _fullScreenLockKey = go.transform;
        }
        return _fullScreenLockKey;
    }
    
    /// <summary>
    /// 为指定 lockable 显示加载圈
    /// </summary>
    private void ShowLoadingForLockable(Transform lockable)
    {
        if (lockable == null)
        {
            // 全屏锁定
            ManagerRefer.UIManager.Open(UINameConstant.LoadingUIMediator);
        }
        else
        {
            // 保存并禁用 interactable 状态
            SaveAndDisableInteractable(lockable);
            
            // 局部锁定 - 使用 ResourceManager 加载加载圈
            ManagerRefer.GameObjectPoolManager.InstantiateAsync(
                "UIPrefabs/ui_comp_loading_circle",
                lockable,
                loadingCircle =>
                {
                    if (loadingCircle == null)
                    {
                        Debug.LogError("[NetworkService] 加载圈创建失败，降级为全屏加载");
                        ManagerRefer.UIManager.Open(UINameConstant.LoadingUIMediator);
                        return;
                    }

                    // 让加载圈充满父物体
                    loadingCircle.transform.FitParent();
                },
                usePool: true
            );
        }
    }
    
    /// <summary>
    /// 为指定 lockable 隐藏加载圈
    /// </summary>
    private void HideLoadingForLockable(Transform lockable)
    {
        if (lockable == null)
        {
            // 关闭全屏加载
            ManagerRefer.UIManager.Close(UINameConstant.LoadingUIMediator);
        }
        else
        {
            // 关闭局部加载 - 查找并回收加载圈
            // 通过查找 lockable 的子对象来找到加载圈
            if (lockable != null)
            {
                for (int i = lockable.childCount - 1; i >= 0; i--)
                {
                    Transform child = lockable.GetChild(i);
                    // 检查是否是加载圈实例（通过名称匹配）
                    if (child.name.Contains("ui_comp_loading_circle"))
                    {
                        ManagerRefer.GameObjectPoolManager.Recycle(child.gameObject, usePool: true);
                        break;
                    }
                }
                
                // 恢复 interactable 状态
                RestoreInteractable(lockable);
            }
        }
    }
    
    /// <summary>
    /// 保存并禁用 interactable 状态
    /// </summary>
    private void SaveAndDisableInteractable(Transform lockable)
    {
        if (lockable == null) return;
        
        // 检查 Selectable 组件（Button, Toggle, Slider, InputField 等的基类）
        var selectable = lockable.GetComponent<UnityEngine.UI.Selectable>();
        if (selectable != null)
        {
            lock (_lockableLock)
            {
                _lockableInteractableStates[lockable] = selectable.interactable;
            }
            selectable.interactable = false;
            return;
        }
        
        // 检查 CanvasGroup 组件
        var canvasGroup = lockable.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            lock (_lockableLock)
            {
                _lockableInteractableStates[lockable] = canvasGroup.interactable;
            }
            canvasGroup.interactable = false;
            return;
        }
    }
    
    /// <summary>
    /// 恢复 interactable 状态
    /// </summary>
    private void RestoreInteractable(Transform lockable)
    {
        if (lockable == null) return;
        
        bool originalState;
        lock (_lockableLock)
        {
            if (!_lockableInteractableStates.TryGetValue(lockable, out originalState))
            {
                return; // 没有保存的状态，无需恢复
            }
            _lockableInteractableStates.Remove(lockable);
        }
        
        // 检查 Selectable 组件
        var selectable = lockable.GetComponent<UnityEngine.UI.Selectable>();
        if (selectable != null)
        {
            selectable.interactable = originalState;
            return;
        }
        
        // 检查 CanvasGroup 组件
        var canvasGroup = lockable.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.interactable = originalState;
            return;
        }
    }
    #endregion

    #region Editor用展示属性
    public int ActiveRequests => _activeRequests;
    public bool IsRequesting => _activeRequests > 0;
    public string BaseUrl => config.baseUrl;
    
    // Lockable 信息（用于编辑器显示）
    /// <summary>
    /// 获取所有活动的 lockable 及其请求计数
    /// </summary>
    public Dictionary<Transform, int> ActiveLockables
    {
        get
        {
            lock (_lockableLock)
            {
                var result = new Dictionary<Transform, int>();
                Transform fullScreenKey = GetFullScreenLockKey();
                
                foreach (var kvp in _lockableRequests)
                {
                    // 将全屏锁定键转换回 null 以便编辑器显示
                    Transform key = kvp.Key == fullScreenKey ? null : kvp.Key;
                    result[key] = kvp.Value;
                }
                
                return result;
            }
        }
    }
    
    /// <summary>
    /// 是否有全屏锁定的请求
    /// </summary>
    public bool HasFullScreenLock
    {
        get
        {
            lock (_lockableLock)
            {
                Transform key = GetFullScreenLockKey();
                return _lockableRequests.ContainsKey(key) && _lockableRequests[key] > 0;
            }
        }
    }
    
    /// <summary>
    /// 全屏锁定请求数
    /// </summary>
    public int FullScreenLockCount
    {
        get
        {
            lock (_lockableLock)
            {
                Transform key = GetFullScreenLockKey();
                return _lockableRequests.ContainsKey(key) ? _lockableRequests[key] : 0;
            }
        }
    }
    
    /// <summary>
    /// 局部锁定的数量（不包括全屏锁定）
    /// </summary>
    public int LocalLockCount
    {
        get
        {
            lock (_lockableLock)
            {
                return _lockableRequests.Count - (HasFullScreenLock ? 1 : 0);
            }
        }
    }
    #endregion
}