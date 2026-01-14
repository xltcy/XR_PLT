using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UniGLTF;
using UnityEngine;
using UnityEngine.Rendering;
/// <summary>
/// Manager注册表，用于管理所有Manager
/// Manager不同于Controller，Manager不依赖于MonoBehaviour，可以是纯C#类
/// </summary>
public class ManagerRegister : Singleton<ManagerRegister>
{
    [Header("Manager配置")]
    private bool autoInitializeOnStart = true;
    [SerializeField] private bool logInitialization = true;
    [SerializeField] private bool warnInitialization = true;
    [SerializeField] private bool errorInitialization = true;
    
    // 所有已注册的Manager
    [SerializeField]
    private SerializedDictionary<Type, BaseManager> managers = new SerializedDictionary<Type, BaseManager>();
    
    // Manager初始化队列
    private List<BaseManager> initializationQueue = new List<BaseManager>();
    
    // 是否正在初始化
    private bool isInitializing = false;
    
    public enum InitTiming
    {
        OnAwake,        // 程序启动时初始化
        OnFirstUsed,    // 第一次使用时初始化
    }

    protected override void Awake()
    {
        // 添加 PersistentSingleton 组件
        // gameObject.GetOrAddComponent<PersistentSingleton>();
        
        base.Awake();
        
        // 自动注册所有 InitTiming = OnAwake 的 Manager
        AutoRegisterManagerTypes();
    }
    
    protected void Start()
    {
        if (autoInitializeOnStart)
        {
            InitializeAllManagers();
        }
    }

    protected override void OnDestroy()
    {
        CleanupAllManagers();
        
        base.OnDestroy();
    }


    /// <summary>
    /// 自动注册所有 InitTiming = OnAwake 的 Manager
    /// 注：Manager是纯C#类，不依赖MonoBehaviour，因此不使用FindObjectsOfType
    /// </summary>
    private void AutoRegisterManagerTypes()
    {
        // 通过反射扫描程序集中所有继承自BaseManager的类型
        Assembly assembly = Assembly.GetExecutingAssembly();
        Type[] managerTypes = assembly.GetTypes()
            .Where(t => typeof(BaseManager).IsAssignableFrom(t) && !t.IsAbstract && t != typeof(BaseManager))
            .ToArray();
        
        foreach (Type managerType in managerTypes)
        {
            try
            {
                // 创建临时实例来检查 InitTiming
                BaseManager tempInstance = Activator.CreateInstance(managerType) as BaseManager;
                
                if (tempInstance is { InitTiming: InitTiming.OnAwake })
                {
                    // 如果已经注册过，跳过
                    if (managers.ContainsKey(managerType))
                    {
                        if (logInitialization)
                        {
                            Utils.LogMessage(LogType.Log, logInitialization, 
                                $"[ManagerRegister] Manager {managerType.Name} 已存在，跳过自动注册");
                        }
                        continue;
                    }
                    
                    // 注册这个Manager
                    managers[managerType] = tempInstance;
                    
                    if (logInitialization)
                    {
                        Utils.LogMessage(LogType.Log, logInitialization, 
                            $"[ManagerRegister] 自动注册Manager: {managerType.Name} (InitTiming = OnAwake)");
                    }
                }
            }
            catch (Exception e)
            {
                Utils.LogMessage(LogType.Warning, warnInitialization, 
                    $"[ManagerRegister] 无法自动注册Manager: {managerType.Name}, 错误: {e.Message}\n" +
                    $"请确保该类有无参构造函数");
            }
        }
    }
    
    /// <summary>
    /// 获取指定类型的Manager，如果不存在则自动创建
    /// </summary>
    public T GetManager<T>() where T : BaseManager
    {
        Type managerType = typeof(T);
        
        if (managers.TryGetValue(managerType, out BaseManager manager))
        {
            return (T)manager;
        }
        
        // 如果Manager不存在，自动创建
        return CreateManager<T>();
    }
    
    /// <summary>
    /// 创建指定类型的Manager（纯C#类实例）
    /// </summary>
    private T CreateManager<T>() where T : BaseManager
    {
        Type managerType = typeof(T);
        
        if (managers.TryGetValue(managerType, out var value))
        {
            Utils.LogMessage(LogType.Warning, warnInitialization, $"[ManagerRegister] Manager类型 {managerType.Name} 已存在");
            return (T)value;
        }
        
        // 使用反射创建Manager实例（纯C#类，不是MonoBehaviour）
        T manager = null;
        try
        {
            manager = Activator.CreateInstance<T>();
        }
        catch (Exception e)
        {
            Utils.LogMessage(LogType.Error, errorInitialization, 
                $"[ManagerRegister] 创建Manager失败: {managerType.Name}, 错误: {e.Message}\n" +
                $"请确保 {managerType.Name} 有无参构造函数");
            return null;
        }
        
        // 注册Manager
        managers[managerType] = manager;
        
        if (logInitialization)
        {
            Utils.LogMessage(LogType.Log, logInitialization, $"[ManagerRegister] 创建Manager: {managerType.Name}");
        }
        
        // 如果已经初始化完成，立即初始化新创建的Manager
        if (!isInitializing && manager != null)
        {
            if (!manager.IsInitialized)
            {
                manager.OnRegister();
            }
        }
        
        return manager;
    }
    
    /// <summary>
    /// 初始化所有Manager
    /// </summary>
    public void InitializeAllManagers()
    {
        if (isInitializing)
        {
            Utils.LogMessage(LogType.Warning, warnInitialization, "[ManagerRegister] Manager初始化正在进行中");
            return;
        }
        
        isInitializing = true;
        
        // 收集需要初始化的Manager
        initializationQueue.Clear();
        initializationQueue.AddRange(managers.Values.Where(m => !m.IsInitialized));
        
        // 按优先级排序
        initializationQueue.Sort((a, b) => a.InitPriority.CompareTo(b.InitPriority));
        
        if (logInitialization)
        {
            Utils.LogMessage(LogType.Log, logInitialization, $"[ManagerRegister] 开始初始化 {initializationQueue.Count} 个Manager");
        }
        
        // 初始化所有Manager
        foreach (BaseManager manager in initializationQueue)
        {
            try
            {
                if (!manager.IsInitialized)
                {
                    manager.OnRegister();
                    
                    if (logInitialization)
                    {
                        Utils.LogMessage(LogType.Log, logInitialization, $"[ManagerRegister] 初始化完成: {manager.GetType().Name}");
                    }
                }
            }
            catch (Exception e)
            {
                Utils.LogMessage(LogType.Error, errorInitialization, $"[ManagerRegister] 初始化Manager失败: {manager.GetType().Name}, 错误: {e.Message}");
            }
        }
        
        initializationQueue.Clear();
        isInitializing = false;
        
        if (logInitialization)
        {
            Utils.LogMessage(LogType.Log, logInitialization, "[ManagerRegister] 所有Manager初始化完成");
        }
    }
    
    /// <summary>
    /// 清理所有Manager
    /// </summary>
    public void CleanupAllManagers()
    {
        foreach (BaseManager manager in managers.Values)
        {
            try
            {
                if (manager.IsInitialized)
                {
                    manager.OnUnregister();
                }
            }
            catch (Exception e)
            {
                Utils.LogMessage(LogType.Error, errorInitialization, $"[ManagerRegister] 清理Manager失败: {manager.GetType().Name}, 错误: {e.Message}");
            }
        }
        
        if (logInitialization)
        {
            Utils.LogMessage(LogType.Log, logInitialization, "[ManagerRegister] 所有Manager清理完成");
        }
    }
    
    /// <summary>
    /// 获取所有已注册的Manager类型
    /// </summary>
    public List<Type> GetRegisteredManagerTypes()
    {
        return new List<Type>(managers.Keys);
    }
    
    /// <summary>
    /// 获取所有已注册的Manager
    /// </summary>
    public List<BaseManager> GetRegisteredManagers()
    {
        return new List<BaseManager>(managers.Values);
    }
    
    /// <summary>
    /// 检查指定类型的Manager是否存在
    /// </summary>
    public bool HasManager<T>() where T : BaseManager
    {
        return managers.ContainsKey(typeof(T));
    }
    
    /// <summary>
    /// 手动注册Manager
    /// </summary>
    public void RegisterManager<T>(T manager) where T : BaseManager
    {
        Type managerType = typeof(T);
        
        if (managers.ContainsKey(managerType))
        {
            Utils.LogMessage(LogType.Warning, warnInitialization, $"[ManagerRegister] Manager类型 {managerType.Name} 已存在，将被替换");
        }
        
        managers[managerType] = manager;
        manager.OnRegister();
        
        if (logInitialization)
        {
            Utils.LogMessage(LogType.Log, logInitialization, $"[ManagerRegister] 手动注册Manager: {managerType.Name}");
        }
    }
    
    /// <summary>
    /// 注销Manager
    /// </summary>
    public void UnregisterManager<T>() where T : BaseManager
    {
        Type managerType = typeof(T);
        
        if (managers.TryGetValue(managerType, out BaseManager manager))
        {
            if (manager.IsInitialized)
            {
                manager.OnUnregister();
            }
            
            managers.Remove(managerType);
            
            if (logInitialization)
            {
                Utils.LogMessage(LogType.Log, logInitialization, $"[ManagerRegister] 注销Manager: {managerType.Name}");
            }
        }
    }
}
