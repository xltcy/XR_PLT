using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 事件优先级
/// </summary>
public enum EventPriority
{
    Low = 0,
    Normal = 1,
    High = 2,
    Critical = 3,
    System = 4
}

/// <summary>
/// 事件执行选项
/// </summary>
[Flags]
public enum EventOptions
{
    Default = 0,
    Once = 1,           // 只执行一次
    Queue = 2,          // 加入队列异步执行
    Immediate = 4,      // 立即同步执行
    NoDuplicates = 8    // 不允许重复监听
}

/// <summary>
/// 事件回调接口
/// </summary>
public interface IEventHandler
{
    void HandleEvent(string eventName, object eventData);
}

/// <summary>
/// 泛型事件处理器
/// </summary>
public interface IEventHandler<T> : IEventHandler
{
    void HandleEvent(T eventData);
}

/// <summary>
/// 事件数据基类
/// </summary>
public class EventData
{
    public string EventName { get; private set; }
    public object Sender { get; private set; }
    public DateTime Timestamp { get; private set; }
    public object Data { get; set; }
    public bool IsConsumed { get; private set; }
    public Dictionary<string, object> Metadata { get; private set; }

    public EventData(string eventName, object sender = null, object data = null)
    {
        EventName = eventName;
        Sender = sender;
        Timestamp = DateTime.Now;
        Data = data;
        IsConsumed = false;
        Metadata = new Dictionary<string, object>();
    }

    public void Consume()
    {
        IsConsumed = true;
    }

    public T GetData<T>()
    {
        if (Data is T typedData)
            return typedData;
        
        try
        {
            return (T)Convert.ChangeType(Data, typeof(T));
        }
        catch
        {
            return default;
        }
    }

    public void SetMetadata(string key, object value)
    {
        Metadata[key] = value;
    }

    public T GetMetadata<T>(string key, T defaultValue = default)
    {
        if (Metadata.TryGetValue(key, out object value) && value is T typedValue)
            return typedValue;
        return defaultValue;
    }
}

/// <summary>
/// 泛型事件数据
/// </summary>
public class EventData<T> : EventData
{
    public new T Data { get; private set; }

    public EventData(string eventName, T data, object sender = null) 
        : base(eventName, sender, data)
    {
        Data = data;
    }

    public new T GetData<T2>() where T2 : T
    {
        return (T2)(object)Data;
    }
}

/// <summary>
/// 事件监听器
/// </summary>
public class EventListener
{
    public string Id { get; private set; }
    public string EventName { get; private set; }
    public Action<EventData> Callback { get; private set; }
    public EventPriority Priority { get; private set; }
    public EventOptions Options { get; private set; }
    public object Owner { get; private set; }

    public EventListener(string eventName, Action<EventData> callback, 
                       EventPriority priority = EventPriority.Normal, 
                       EventOptions options = EventOptions.Default,
                       object owner = null)
    {
        Id = Guid.NewGuid().ToString();
        EventName = eventName;
        Callback = callback;
        Priority = priority;
        Options = options;
        Owner = owner;
    }
}

/// <summary>
/// 泛型事件监听器
/// </summary>
public class EventListener<T> : EventListener
{
    public new Action<EventData<T>> Callback { get; private set; }

    public EventListener(string eventName, Action<EventData<T>> callback,
                       EventPriority priority = EventPriority.Normal,
                       EventOptions options = EventOptions.Default,
                       object owner = null)
        : base(eventName, null, priority, options, owner)
    {
        Callback = callback;
    }
}

/// <summary>
/// 事件管理器 - 单例模式
/// </summary>
public class EventManager : MonoBehaviour
{
    #region Singleton
    private static EventManager _instance;
    public static EventManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("EventManager");
                _instance = go.AddComponent<EventManager>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }
    #endregion

    [Header("配置")]
    [SerializeField] private bool enableLogging = true;
    [SerializeField] private int maxEventHistory = 1000;
    [SerializeField] private float asyncQueueInterval = 0.016f; // 约60Hz
    [SerializeField] private bool autoCleanup = true;
    [SerializeField] private float cleanupInterval = 60f; // 每60秒清理一次

    // 事件监听器存储
    private Dictionary<string, List<EventListener>> _listeners = 
        new Dictionary<string, List<EventListener>>();
    
    // 事件历史记录
    private List<EventData> _eventHistory = new List<EventData>();
    
    // 事件队列（用于异步执行）
    private Queue<EventData> _eventQueue = new Queue<EventData>();
    private bool _isProcessingQueue = false;
    
    // 事件统计
    private Dictionary<string, int> _eventStatistics = new Dictionary<string, int>();
    private DateTime _startTime;
    
    // 清理计时器
    private float _lastCleanupTime;
    
    // 事件
    public delegate void EventDelegate(string eventName, EventData eventData);
    public event EventDelegate OnEventDispatched;
    public event EventDelegate OnEventConsumed;

    #region 初始化
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        _instance = this;
        DontDestroyOnLoad(gameObject);
        _startTime = DateTime.Now;
        _lastCleanupTime = Time.time;
        
        Debug.Log("[EventManager] 初始化完成");
    }

    private void Start()
    {
        StartCoroutine(ProcessEventQueue());
        
        if (autoCleanup)
        {
            StartCoroutine(AutoCleanup());
        }
    }

    private void OnDestroy()
    {
        ClearAllListeners();
    }
    #endregion

    #region 事件监听
    /// <summary>
    /// 添加事件监听器
    /// </summary>
    public string AddListener(string eventName, Action<EventData> callback,
                             EventPriority priority = EventPriority.Normal,
                             EventOptions options = EventOptions.Default,
                             object owner = null)
    {
        if (string.IsNullOrEmpty(eventName) || callback == null)
        {
            Debug.LogWarning("[EventManager] 无效的事件监听器参数");
            return null;
        }

        // 检查重复监听
        if ((options & EventOptions.NoDuplicates) != 0)
        {
            if (_listeners.TryGetValue(eventName, out var listener))
            {
                bool hasDuplicate = listener.Any(l => l.Callback == callback);
                if (hasDuplicate)
                {
                    Debug.LogWarning($"[EventManager] 事件 '{eventName}' 已存在重复监听器");
                    return null;
                }
            }
        }

        // 创建监听器
        var newListener = new EventListener(eventName, callback, priority, options, owner);
        
        // 添加到列表
        if (!_listeners.ContainsKey(eventName))
        {
            _listeners[eventName] = new List<EventListener>();
        }
        
        _listeners[eventName].Add(newListener);
        
        // 按优先级排序
        _listeners[eventName] = _listeners[eventName]
            .OrderByDescending(l => l.Priority)
            .ToList();

        if (enableLogging)
        {
            Debug.Log($"[EventManager] 添加监听器: {eventName} (优先级: {priority})");
        }

        return newListener.Id;
    }

    /// <summary>
    /// 添加泛型事件监听器
    /// </summary>
    public string AddListener<T>(string eventName, Action<EventData<T>> callback,
                                EventPriority priority = EventPriority.Normal,
                                EventOptions options = EventOptions.Default,
                                object owner = null)
    {
        if (string.IsNullOrEmpty(eventName) || callback == null)
            return null;

        // 包装回调
        Action<EventData> wrappedCallback = evt =>
        {
            if (evt is EventData<T> typedEvent)
            {
                callback(typedEvent);
            }
            else
            {
                // 尝试转换
                var typedData = new EventData<T>(evt.EventName, evt.GetData<T>(), evt.Sender);
                callback(typedData);
            }
        };

        return AddListener(eventName, wrappedCallback, priority, options, owner);
    }

    /// <summary>
    /// 添加接口事件监听器
    /// </summary>
    public string AddListener(string eventName, IEventHandler handler,
                             EventPriority priority = EventPriority.Normal,
                             EventOptions options = EventOptions.Default)
    {
        return AddListener(eventName, 
            evt => handler.HandleEvent(evt.EventName, evt.Data),
            priority, options, handler);
    }

    /// <summary>
    /// 移除事件监听器
    /// </summary>
    public bool RemoveListener(string eventName, Action<EventData> callback)
    {
        if (!_listeners.ContainsKey(eventName))
            return false;

        var listeners = _listeners[eventName];
        int count = listeners.RemoveAll(l => l.Callback == callback);

        if (count > 0 && enableLogging)
        {
            Debug.Log($"[EventManager] 移除 {count} 个监听器: {eventName}");
        }

        return count > 0;
    }

    /// <summary>
    /// 通过ID移除监听器
    /// </summary>
    public bool RemoveListenerById(string listenerId)
    {
        foreach (var kvp in _listeners)
        {
            int count = kvp.Value.RemoveAll(l => l.Id == listenerId);
            if (count > 0)
            {
                if (enableLogging)
                {
                    Debug.Log($"[EventManager] 移除监听器ID: {listenerId}");
                }
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 移除所有者所有监听器
    /// </summary>
    public int RemoveListenersByOwner(object owner)
    {
        if (owner == null) return 0;

        int totalRemoved = 0;
        foreach (var kvp in _listeners)
        {
            int count = kvp.Value.RemoveAll(l => l.Owner == owner);
            totalRemoved += count;
        }

        if (totalRemoved > 0 && enableLogging)
        {
            Debug.Log($"[EventManager] 移除所有者 '{owner}' 的 {totalRemoved} 个监听器");
        }

        return totalRemoved;
    }

    /// <summary>
    /// 清除指定事件的所有监听器
    /// </summary>
    public void ClearListeners(string eventName)
    {
        if (_listeners.ContainsKey(eventName))
        {
            _listeners[eventName].Clear();
            Debug.Log($"[EventManager] 清除事件 '{eventName}' 的所有监听器");
        }
    }

    /// <summary>
    /// 清除所有事件监听器
    /// </summary>
    public void ClearAllListeners()
    {
        _listeners.Clear();
        Debug.Log("[EventManager] 清除所有事件监听器");
    }
    #endregion

    #region 事件分发
    /// <summary>
    /// 分发事件
    /// </summary>
    public EventData Dispatch(string eventName, object sender = null, object data = null)
    {
        return DispatchInternal(eventName, sender, data, false);
    }

    /// <summary>
    /// 分发泛型事件
    /// </summary>
    public EventData<T> Dispatch<T>(string eventName, T data, object sender = null)
    {
        var eventData = new EventData<T>(eventName, data, sender);
        return DispatchInternal(eventName, sender, eventData, false) as EventData<T>;
    }

    /// <summary>
    /// 异步分发事件（加入队列）
    /// </summary>
    public void DispatchAsync(string eventName, object sender = null, object data = null)
    {
        var eventData = new EventData(eventName, sender, data);
        lock (_eventQueue)
        {
            _eventQueue.Enqueue(eventData);
        }
    }

    /// <summary>
    /// 立即同步分发事件
    /// </summary>
    public EventData DispatchImmediate(string eventName, object sender = null, object data = null)
    {
        return DispatchInternal(eventName, sender, data, true);
    }

    /// <summary>
    /// 内部分发逻辑
    /// </summary>
    private EventData DispatchInternal(string eventName, object sender, object data, bool immediate)
    {
        if (string.IsNullOrEmpty(eventName))
        {
            Debug.LogWarning("[EventManager] 事件名不能为空");
            return null;
        }

        // 创建事件数据
        var eventData = data is EventData evtData ? evtData : new EventData(eventName, sender, data);
        
        // 记录历史
        AddToHistory(eventData);
        
        // 更新统计
        UpdateStatistics(eventName);
        
        if (enableLogging)
        {
            Debug.Log($"[EventManager] 分发事件: {eventName} (发送者: {sender})");
        }

        // 触发事件
        OnEventDispatched?.Invoke(eventName, eventData);

        // 执行监听器
        if (_listeners.ContainsKey(eventName))
        {
            var listeners = _listeners[eventName].ToList(); // 复制列表，避免在迭代中修改
            
            foreach (var listener in listeners)
            {
                if (eventData.IsConsumed)
                    break;

                try
                {
                    // 执行回调
                    listener.Callback?.Invoke(eventData);
                    
                    // 检查是否只执行一次
                    if ((listener.Options & EventOptions.Once) != 0)
                    {
                        RemoveListenerById(listener.Id);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"[EventManager] 事件处理异常: {eventName}, 错误: {e.Message}");
                }
            }
        }

        // 触发事件完成
        if (eventData.IsConsumed)
        {
            OnEventConsumed?.Invoke(eventName, eventData);
        }

        return eventData;
    }
    #endregion

    #region 事件队列处理
    /// <summary>
    /// 处理事件队列
    /// </summary>
    private System.Collections.IEnumerator ProcessEventQueue()
    {
        while (true)
        {
            if (_eventQueue.Count > 0 && !_isProcessingQueue)
            {
                _isProcessingQueue = true;
                
                // 批量处理事件
                int batchSize = 10;
                for (int i = 0; i < batchSize && _eventQueue.Count > 0; i++)
                {
                    EventData eventData;
                    lock (_eventQueue)
                    {
                        eventData = _eventQueue.Dequeue();
                    }
                    
                    DispatchInternal(eventData.EventName, eventData.Sender, eventData, false);
                    
                    // 如果队列中还有大量事件，等待一帧
                    if (_eventQueue.Count > 50)
                    {
                        yield return null;
                    }
                }
                
                _isProcessingQueue = false;
            }
            
            yield return new WaitForSeconds(asyncQueueInterval);
        }
    }

    /// <summary>
    /// 清空事件队列
    /// </summary>
    public void ClearEventQueue()
    {
        lock (_eventQueue)
        {
            _eventQueue.Clear();
            Debug.Log("[EventManager] 事件队列已清空");
        }
    }
    #endregion

    #region 事件历史
    /// <summary>
    /// 添加到历史记录
    /// </summary>
    private void AddToHistory(EventData eventData)
    {
        lock (_eventHistory)
        {
            _eventHistory.Add(eventData);
            
            // 限制历史记录大小
            if (_eventHistory.Count > maxEventHistory)
            {
                int toRemove = _eventHistory.Count - maxEventHistory;
                _eventHistory.RemoveRange(0, toRemove);
            }
        }
    }

    /// <summary>
    /// 获取事件历史
    /// </summary>
    public List<EventData> GetEventHistory(int maxCount = 100)
    {
        lock (_eventHistory)
        {
            int startIndex = Mathf.Max(0, _eventHistory.Count - maxCount);
            int count = Mathf.Min(maxCount, _eventHistory.Count);
            return _eventHistory.GetRange(startIndex, count);
        }
    }

    /// <summary>
    /// 根据事件名过滤历史
    /// </summary>
    public List<EventData> GetEventHistory(string eventName, int maxCount = 100)
    {
        lock (_eventHistory)
        {
            var filtered = _eventHistory.Where(e => e.EventName == eventName).ToList();
            int startIndex = Mathf.Max(0, filtered.Count - maxCount);
            int count = Mathf.Min(maxCount, filtered.Count);
            return filtered.GetRange(startIndex, count);
        }
    }

    /// <summary>
    /// 清空事件历史
    /// </summary>
    public void ClearEventHistory()
    {
        lock (_eventHistory)
        {
            _eventHistory.Clear();
            Debug.Log("[EventManager] 事件历史已清空");
        }
    }
    #endregion

    #region 事件统计
    /// <summary>
    /// 更新事件统计
    /// </summary>
    private void UpdateStatistics(string eventName)
    {
        if (!_eventStatistics.ContainsKey(eventName))
        {
            _eventStatistics[eventName] = 0;
        }
        _eventStatistics[eventName]++;
    }

    /// <summary>
    /// 获取事件统计信息
    /// </summary>
    public Dictionary<string, int> GetEventStatistics()
    {
        return new Dictionary<string, int>(_eventStatistics);
    }

    /// <summary>
    /// 获取事件频率（事件/秒）
    /// </summary>
    public float GetEventFrequency(string eventName)
    {
        if (!_eventStatistics.ContainsKey(eventName))
            return 0f;

        TimeSpan runtime = DateTime.Now - _startTime;
        if (runtime.TotalSeconds <= 0)
            return 0f;

        return _eventStatistics[eventName] / (float)runtime.TotalSeconds;
    }

    /// <summary>
    /// 重置事件统计
    /// </summary>
    public void ResetStatistics()
    {
        _eventStatistics.Clear();
        _startTime = DateTime.Now;
        Debug.Log("[EventManager] 事件统计已重置");
    }
    #endregion

    #region 自动清理
    /// <summary>
    /// 自动清理无效监听器
    /// </summary>
    private System.Collections.IEnumerator AutoCleanup()
    {
        while (true)
        {
            yield return new WaitForSeconds(cleanupInterval);
            
            if (Time.time - _lastCleanupTime >= cleanupInterval)
            {
                CleanupInvalidListeners();
                _lastCleanupTime = Time.time;
            }
        }
    }

    /// <summary>
    /// 清理无效监听器
    /// </summary>
    public void CleanupInvalidListeners()
    {
        int totalRemoved = 0;
        var eventsToRemove = new List<string>();

        foreach (var kvp in _listeners)
        {
            // 移除Owner为null的监听器
            int removed = kvp.Value.RemoveAll(l => l.Owner == null);
            totalRemoved += removed;

            // 检查是否为空
            if (kvp.Value.Count == 0)
            {
                eventsToRemove.Add(kvp.Key);
            }
        }

        // 移除空的事件
        foreach (var eventName in eventsToRemove)
        {
            _listeners.Remove(eventName);
        }

        if (totalRemoved > 0 && enableLogging)
        {
            Debug.Log($"[EventManager] 清理了 {totalRemoved} 个无效监听器");
        }
    }
    #endregion

    #region 工具方法
    /// <summary>
    /// 检查事件是否有监听器
    /// </summary>
    public bool HasListeners(string eventName)
    {
        return _listeners.ContainsKey(eventName) && _listeners[eventName].Count > 0;
    }

    /// <summary>
    /// 获取事件监听器数量
    /// </summary>
    public int GetListenerCount(string eventName)
    {
        return _listeners.ContainsKey(eventName) ? _listeners[eventName].Count : 0;
    }

    /// <summary>
    /// 获取总监听器数量
    /// </summary>
    public int GetTotalListenerCount()
    {
        return _listeners.Sum(kvp => kvp.Value.Count);
    }

    /// <summary>
    /// 获取活跃事件列表
    /// </summary>
    public List<string> GetActiveEvents()
    {
        return _listeners.Where(kvp => kvp.Value.Count > 0).Select(kvp => kvp.Key).ToList();
    }

    /// <summary>
    /// 等待事件触发（协程）
    /// </summary>
    public System.Collections.IEnumerator WaitForEvent(string eventName, 
                                                      Action<EventData> onEvent = null)
    {
        bool eventTriggered = false;
        EventData triggeredEvent = null;
        
        string listenerId = AddListener(eventName, evt =>
        {
            eventTriggered = true;
            triggeredEvent = evt;
            onEvent?.Invoke(evt);
        }, EventPriority.Normal, EventOptions.Once);
        
        // 等待事件触发
        while (!eventTriggered)
        {
            yield return null;
        }
        
        // 清理监听器（如果还没被自动移除）
        RemoveListenerById(listenerId);
    }
    #endregion

    #region 调试和监控
    /// <summary>
    /// 打印事件系统状态
    /// </summary>
    public void PrintStatus()
    {
        Debug.Log("=== 事件系统状态 ===");
        Debug.Log($"运行时间: {(DateTime.Now - _startTime):hh\\:mm\\:ss}");
        Debug.Log($"活跃事件数: {GetActiveEvents().Count}");
        Debug.Log($"总监听器数: {GetTotalListenerCount()}");
        Debug.Log($"事件历史记录: {_eventHistory.Count}");
        Debug.Log($"事件队列长度: {_eventQueue.Count}");
        
        // 显示最多的事件
        if (_eventStatistics.Count > 0)
        {
            var topEvents = _eventStatistics.OrderByDescending(kvp => kvp.Value).Take(5);
            Debug.Log("事件统计（前5）:");
            foreach (var kvp in topEvents)
            {
                Debug.Log($"  {kvp.Key}: {kvp.Value} 次 ({GetEventFrequency(kvp.Key):F2}/秒)");
            }
        }
    }

    /// <summary>
    /// 导出事件历史为JSON
    /// </summary>
    public string ExportHistoryToJson(int maxEvents = 100)
    {
        var history = GetEventHistory(maxEvents);
        var exportData = history.Select(e => new
        {
            e.EventName,
            Timestamp = e.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff"),
            Sender = e.Sender?.GetType().Name ?? "null",
            Data = e.Data?.ToString(),
            e.IsConsumed
        }).ToList();
        
        return JsonUtility.ToJson(exportData, true);
    }
    #endregion

    #region 预定义事件快捷方法
    /// <summary>
    /// 发送系统消息
    /// </summary>
    public void SendSystemMessage(string message, EventPriority priority = EventPriority.Normal)
    {
        Dispatch("System.Message", this, new { Message = message, Level = priority });
    }

    /// <summary>
    /// 发送错误消息
    /// </summary>
    public void SendErrorMessage(string error, Exception exception = null)
    {
        var errorData = new
        {
            Error = error,
            Exception = exception?.Message,
            StackTrace = exception?.StackTrace,
            Timestamp = DateTime.Now
        };
        Dispatch("System.Error", this, errorData);
    }

    /// <summary>
    /// 发送UI更新事件
    /// </summary>
    public void SendUIUpdate(string uiElement, object data)
    {
        Dispatch($"UI.Update.{uiElement}", this, data);
    }

    /// <summary>
    /// 发送场景事件
    /// </summary>
    public void SendSceneEvent(string sceneName, string action, object data = null)
    {
        Dispatch($"Scene.{action}", this, new { SceneName = sceneName, Data = data });
    }
    #endregion
}

/// <summary>
/// 事件管理器扩展方法
/// </summary>
public static class EventManagerExtensions
{
    /// <summary>
    /// 快速添加监听器（使用委托）
    /// </summary>
    public static string AddEventListener(this object owner, string eventName, Action<EventData> callback,
                           EventPriority priority = EventPriority.Normal)
    {
        return EventManager.Instance.AddListener(eventName, callback, priority, EventOptions.Default, owner);
    }

    /// <summary>
    /// 快速添加泛型监听器
    /// </summary>
    public static string AddEventListener<T>(this object owner, string eventName, Action<EventData<T>> callback,
                              EventPriority priority = EventPriority.Normal)
    {
        return EventManager.Instance.AddListener(eventName, callback, priority, EventOptions.Default, owner);
    }

    /// <summary>
    /// 快速分发事件
    /// </summary>
    public static EventData TriggerEvent(this object sender, string eventName, object data = null)
    {
        return EventManager.Instance.Dispatch(eventName, sender, data);
    }

    /// <summary>
    /// 快速分发泛型事件
    /// </summary>
    public static EventData<T> TriggerEvent<T>(this object sender, string eventName, T data)
    {
        return EventManager.Instance.Dispatch(eventName, data, sender);
    }

    /// <summary>
    /// 快速移除监听器
    /// </summary>
    public static void RemoveEventListener(this object owner, string eventName, Action<EventData> callback)
    {
        EventManager.Instance.RemoveListener(eventName, callback);
    }

    /// <summary>
    /// 移除所有者所有监听器
    /// </summary>
    public static void RemoveAllEventListener(this object owner)
    {
        EventManager.Instance.RemoveListenersByOwner(owner);
    }
}