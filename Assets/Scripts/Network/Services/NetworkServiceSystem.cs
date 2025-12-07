using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// 网络服务管理器 - 单例模式
/// </summary>
public class NetworkServiceSystem : Singleton<NetworkServiceSystem>
{
    [Header("配置")]
    private NetworkConfig config = NetworkConfig.Instance;

    private Queue<Action> _mainThreadActions = new Queue<Action>();
    private readonly object _queueLock = new object();
    private int _activeRequests = 0;
    private string _authToken = "";

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
    private void Awake()
    {
        gameObject.AddComponent<PersistentSingleton>();
        
        base.Awake();
        
        DontDestroyOnLoad(gameObject);

        Debug.Log($"[NetworkService] 初始化完成，基础URL: {config.baseUrl}");
    }

    private void Update()
    {
        // 处理主线程回调
        lock (_queueLock)
        {
            while (_mainThreadActions.Count > 0)
            {
                Action action = _mainThreadActions.Dequeue();
                try
                {
                    action?.Invoke();
                }
                catch (Exception e)
                {
                    Debug.LogError($"主线程回调执行失败: {e}");
                }
            }
        }
    }
    #endregion

    #region 公共方法
    /// <summary>
    /// 发送HTTP请求
    /// </summary>
    public string SendRequest(RequestParams requestParams, Transform lockable = null, ResponseEvent callback = null)
    {
        string requestId = Guid.NewGuid().ToString();
        
        // 启动协程处理请求
        StartCoroutine(ProcessRequest(requestParams, lockable, callback));
        
        return requestId;
    }

    /// <summary>
    /// 发送GET请求
    /// </summary>
    public string Get(string endpoint, Transform lockable, ResponseEvent callback = null, Dictionary<string, string> queryParams = null)
    {
        var requestParams = new RequestParams
        {
            url = BuildUrl(endpoint),
            method = "GET",
            queryParams = queryParams ?? new Dictionary<string, string>()
        };

        return SendRequest(requestParams, lockable, callback);
    }

    /// <summary>
    /// 发送POST请求
    /// </summary>
    public string Post(string endpoint, object requestData, Transform lockable = null, ResponseEvent callback = null)
    {
        var requestParams = new RequestParams
        {
            url = BuildUrl(endpoint),
            method = "POST",
            requestData = requestData
        };

        return SendRequest(requestParams, lockable, callback);
    }

    /// <summary>
    /// 发送 FormData 请求（支持多种数据类型）
    /// </summary>
    public string SendFormData(string url, List<FormField> formFields, 
                              string method = "POST", Transform lockable = null,
                              ResponseEvent callback = null)
    {
        var requestParams = new RequestParams
        {
            url = url,
            method = method,
            FormDataFields = formFields
        };
        
        return SendRequest(requestParams, lockable, callback);
    }
    
    /// <summary>
    /// 发送文件上传请求
    /// </summary>
    public string UploadFile(string url, string fieldName, byte[] fileData, 
                            string fileName, Dictionary<string, string> additionalFields = null,
                            Transform lockable = null, ResponseEvent callback = null)
    {
        var formFields = new List<FormField>();
        
        // 添加文件
        formFields.Add(FormField.CreateFile(fieldName, fileData, fileName));
        
        // 添加额外字段
        if (additionalFields != null)
        {
            foreach (var kvp in additionalFields)
            {
                formFields.Add(FormField.CreateText(kvp.Key, kvp.Value));
            }
        }
        
        return SendFormData(url, formFields, "POST", lockable, callback);
    }
    
    /// <summary>
    /// 发送图片上传请求
    /// </summary>
    public string UploadImage(string url, string fieldName, byte[] imageData,
                             string fileName = "image.jpg", 
                             Dictionary<string, string> additionalFields = null,
                             Transform lockable = null, ResponseEvent callback = null)
    {
        var formFields = new List<FormField>();
        
        // 添加图片文件
        formFields.Add(FormField.CreateFile(fieldName, imageData, fileName, "image/jpeg"));
        
        // 添加额外字段
        if (additionalFields != null)
        {
            foreach (var kvp in additionalFields)
            {
                formFields.Add(FormField.CreateText(kvp.Key, kvp.Value));
            }
        }
        
        return SendFormData(url, formFields, "POST", lockable, callback);
    }
    
    /// <summary>
    /// 发送PUT请求
    /// </summary>
    public string Put(string endpoint, object requestData, Transform lockable = null, ResponseEvent callback = null)
    {
        var requestParams = new RequestParams
        {
            url = BuildUrl(endpoint),
            method = "PUT",
            requestData = requestData
        };

        return SendRequest(requestParams, lockable, callback);
    }

    /// <summary>
    /// 发送DELETE请求
    /// </summary>
    public string Delete(string endpoint, Transform lockable = null, ResponseEvent callback = null)
    {
        var requestParams = new RequestParams
        {
            url = BuildUrl(endpoint),
            method = "DELETE"
        };

        return SendRequest(requestParams, lockable, callback);
    }
    
    /// <summary>
    /// 清除所有请求
    /// </summary>
    public void ClearAllRequests()
    {
        StopAllCoroutines();
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

        var queryBuilder = new System.Text.StringBuilder(baseUrl);
        queryBuilder.Append("?");

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
    private IEnumerator ProcessRequest(RequestParams requestParams, Transform lockable = null, ResponseEvent callback = null)
    {
        yield return ExecuteRequestWithRetry(requestParams, callback);
    }

    /// <summary>
    /// 网络请求处理器（包含重试逻辑）
    /// </summary>
    private IEnumerator ExecuteRequestWithRetry(RequestParams requestParams, ResponseEvent callback = null)
    {
        _activeRequests++;
        NotifyActiveRequestsChanged();
        
        UIManager.SetLoadingStatus(true);

        long totalStartTime = DateTime.Now.Ticks;
        NetworkResponse finalResponse = null;
        
        try
        {
            // 触发开始事件
            OnRequestStarted?.Invoke(new NetworkResponse
            {
                localData = requestParams.localData, 
            });
            
            int attempt = 0;
            bool shouldRetry = true;
            
            while (shouldRetry && attempt <= (requestParams.retryOnFailure ? config.maxRetries : 0))
            {
                attempt++;
                
                if (attempt > 1)
                {
                    Debug.LogWarning($"[NetworkService] 请求重试 ({attempt - 1}/{config.maxRetries}): {requestParams.url}");
                    yield return new WaitForSeconds(config.retryDelay);
                }
                
                // 执行单次请求
                var attempt1 = attempt;
                yield return ExecuteSingleRequest(requestParams, response =>
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
                                      requestParams.retryOnFailure &&
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
            
            UIManager.SetLoadingStatus(false);
            
            // 计算总时间
            if (finalResponse != null)
            {
                finalResponse.responseTime = (DateTime.Now.Ticks - totalStartTime) / TimeSpan.TicksPerMillisecond;
            }
            
            // 执行最终回调
            if (finalResponse != null)
            {
                InvokeCallback(requestParams.networkConstant, finalResponse, callback);
            }
            else
            {
                // 创建错误响应
                var errorResponse = new NetworkResponse
                {
                    success = false,
                    error = "请求执行异常",
                    localData = requestParams.localData,
                    responseTime = (DateTime.Now.Ticks - totalStartTime) / TimeSpan.TicksPerMillisecond,
                };
                InvokeCallback(requestParams.networkConstant, errorResponse, callback);
            }
        }
    }

    /// <summary>
    /// 执行单次请求（不包含重试）
    /// </summary>
    private IEnumerator ExecuteSingleRequest(RequestParams requestParams, Action<NetworkResponse> onComplete)
    {
        long startTime = DateTime.Now.Ticks;
        var response = new NetworkResponse
        {
            localData = requestParams.localData,
        };
        string fullUrl = BuildFullUrl(requestParams.url, requestParams.queryParams);
        UnityWebRequest request = CreateUnityWebRequest(fullUrl, requestParams);
        AddHeadersToRequest(request, requestParams.headers);

        if (config.enableLogging)
        {
            LogRequest(request, requestParams);
        }

        // 同步发送请求（简化版本）
        request.timeout = requestParams.timeout;
        var asyncOperation = request.SendWebRequest();

        // 等待请求完成
        while (!asyncOperation.isDone)
        {
            yield return null; // 每帧检查一次，不阻塞主线程

            // 可以在这里添加超时检查
            if ((DateTime.Now.Ticks - startTime) / TimeSpan.TicksPerMillisecond > requestParams.timeout * 1000)
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
        
    private UnityWebRequest CreateUnityWebRequest(string url, RequestParams requestParams)
    {
        switch (requestParams.method.ToUpper())
        {
            case "POST":
            case "PUT":
                if (requestParams.FormDataFields != null && requestParams.FormDataFields.Count > 0)
                {
                    // 新的 FormData 请求（支持多种数据类型）
                    return CreateFormDataRequest(url, requestParams.method, requestParams.FormDataFields);
                }
                else
                {
                    // JSON请求
                    var request = new UnityWebRequest(url, requestParams.method);
                    if (requestParams.requestData != null)
                    {
                        string json = JsonConvert.SerializeObject(requestParams.requestData);
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
    private UnityWebRequest CreateFormDataRequest(string url, string method, List<FormField> formFields)
    {
        // 创建 WWWForm
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
        UnityWebRequest request = UnityWebRequest.Post(url, form);
    
        // 如果不是 POST，修改方法（PUT 等也支持 FormData）
        if (method.ToUpper() != "POST")
        {
            request.method = method.ToUpper();
        }
    
        return request;
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

    private void LogRequest(UnityWebRequest request, RequestParams requestParams)
    {
        string log = $"[NetworkService] 发送请求:\n" +
                    $"URL: {request.url}\n" +
                    $"Method: {request.method}\n" +
                    $"Headers: {(request.GetRequestHeader("Authorization") != null ? "有认证" : "无认证")}";

        if (requestParams.requestData != null)
        {
            log += $"\nBody: {JsonUtility.ToJson(requestParams.requestData)}";
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
    #endregion

    #region 属性
    public int ActiveRequests => _activeRequests;
    public bool IsRequesting => _activeRequests > 0;
    public string BaseUrl => config.baseUrl;
    #endregion
}