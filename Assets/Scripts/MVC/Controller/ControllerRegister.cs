using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UniGLTF;
using UnityEngine;
using UnityEngine.Rendering;
/// <summary>
/// Controller注册表，用于管理所有Controller
/// Controller不同于Manager，Controller依赖于MonoBehaviour，必须挂载在GameObject上
/// </summary>
public class ControllerRegister : Singleton<ControllerRegister>
{
    [Header("Controller配置")]
    private bool autoInitializeOnStart = true;
    [SerializeField] private bool logInitialization = true;
    [SerializeField] private bool warnInitialization = true;
    [SerializeField] private bool errorInitialization = true;
    
    // 所有已注册的Controller
    [SerializeField]
    private SerializedDictionary<Type, BaseController> controllers = new SerializedDictionary<Type, BaseController>();
    
    // Controller初始化队列
    private List<BaseController> initializationQueue = new List<BaseController>();
    
    // 是否正在初始化
    private bool isInitializing = false;

    protected override void Awake()
    {
        // 添加 PersistentSingleton 组件
        // gameObject.GetOrAddComponent<PersistentSingleton>();

        
        base.Awake();
        
        // 自动查找并注册场景中已存在的Controller
        AutoRegisterExistingControllers();
    }
    
    protected void Start()
    {
        if (autoInitializeOnStart)
        {
            InitializeAllControllers();
        }
    }

    protected override void OnDestroy()
    {
        CleanupAllControllers();
        
        base.OnDestroy();
    }


    /// <summary>
    /// 自动注册场景中已存在的Controller
    /// </summary>
    private void AutoRegisterExistingControllers()
    {
        BaseController[] existingControllers = FindObjectsOfType<BaseController>(true);
        
        foreach (BaseController controller in existingControllers)
        {
            Type controllerType = controller.GetType();
            
            if (!controllers.ContainsKey(controllerType))
            {
                controllers[controllerType] = controller;
                
                if (logInitialization)
                {
                    Utils.LogMessage(LogType.Log, logInitialization, $"[ControllerRegister] 自动注册Controller: {controllerType.Name}");
                }
            }
            else
            {
                Utils.LogMessage(LogType.Warning, warnInitialization, $"[ControllerRegister] Controller类型 {controllerType.Name} 已存在，跳过重复注册");
            }
        }
    }
    
    /// <summary>
    /// 获取指定类型的Controller，如果不存在则自动创建
    /// </summary>
    public T GetController<T>() where T : BaseController
    {
        Type controllerType = typeof(T);
        
        if (controllers.TryGetValue(controllerType, out BaseController controller))
        {
            return (T)controller;
        }
        
        // 如果Controller不存在，自动创建
        return CreateController<T>();
    }
    
    /// <summary>
    /// 创建指定类型的Controller
    /// </summary>
    private T CreateController<T>() where T : BaseController
    {
        Type controllerType = typeof(T);
        
        if (controllers.TryGetValue(controllerType, out var value))
        {
            Utils.LogMessage(LogType.Warning, warnInitialization, $"[ControllerRegister] Controller类型 {controllerType.Name} 已存在");
            return (T)value;
        }
        
        // 创建Controller GameObject
        GameObject controllerGo = new GameObject($"{controllerType.Name}");
        controllerGo.transform.SetParent(transform);
        
        // 添加Controller组件
        T controller = controllerGo.AddComponent<T>();
        
        // 注册Controller
        controllers[controllerType] = controller;
        
        if (logInitialization)
        {
            Utils.LogMessage(LogType.Log, logInitialization, $"[ControllerRegister] 创建Controller: {controllerType.Name}");
        }
        
        // 如果已经初始化完成，立即初始化新创建的Controller
        if (!isInitializing && controller is BaseController baseController)
        {
            if (!baseController.IsInitialized)
            {
                baseController.OnRegister();
            }
        }
        
        return controller;
    }
    
    /// <summary>
    /// 初始化所有Controller
    /// </summary>
    public void InitializeAllControllers()
    {
        if (isInitializing)
        {
            Utils.LogMessage(LogType.Warning, warnInitialization, "[ControllerRegister] Controller初始化正在进行中");
            return;
        }
        
        isInitializing = true;
        
        // 收集需要初始化的Controller
        initializationQueue.Clear();
        initializationQueue.AddRange(controllers.Values.Where(m => !m.IsInitialized));
        
        // 按优先级排序
        initializationQueue.Sort((a, b) => a.InitPriority.CompareTo(b.InitPriority));
        
        if (logInitialization)
        {
            Utils.LogMessage(LogType.Log, logInitialization, $"[ControllerRegister] 开始初始化 {initializationQueue.Count} 个Controller");
        }
        
        // 初始化所有Controller
        foreach (BaseController controller in initializationQueue)
        {
            try
            {
                if (!controller.IsInitialized)
                {
                    controller.OnRegister();
                    
                    if (logInitialization)
                    {
                        Utils.LogMessage(LogType.Log, logInitialization, $"[ControllerRegister] 初始化完成: {controller.GetType().Name}");
                    }
                }
            }
            catch (Exception e)
            {
                Utils.LogMessage(LogType.Error, errorInitialization, $"[ControllerRegister] 初始化Controller失败: {controller.GetType().Name}, 错误: {e.Message}");
            }
        }
        
        initializationQueue.Clear();
        isInitializing = false;
        
        if (logInitialization)
        {
            Utils.LogMessage(LogType.Log, logInitialization, "[ControllerRegister] 所有Controller初始化完成");
        }
    }
    
    /// <summary>
    /// 清理所有Controller
    /// </summary>
    public void CleanupAllControllers()
    {
        foreach (BaseController controller in controllers.Values)
        {
            try
            {
                if (controller.IsInitialized)
                {
                    controller.OnUnregister();
                }
            }
            catch (Exception e)
            {
                Utils.LogMessage(LogType.Error, errorInitialization, $"[ControllerRegister] 清理Controller失败: {controller.GetType().Name}\n\n错误: {e.Message}\n\n调用栈：{e.StackTrace}");
            }
        }
        
        if (logInitialization)
        {
            Utils.LogMessage(LogType.Log, logInitialization, "[ControllerRegister] 所有Controller清理完成");
        }
    }
    
    /// <summary>
    /// 获取所有已注册的Controller类型
    /// </summary>
    public List<Type> GetRegisteredControllerTypes()
    {
        return new List<Type>(controllers.Keys);
    }
    
    /// <summary>
    /// 获取所有已注册的Controller
    /// </summary>
    public List<BaseController> GetRegisteredControllers()
    {
        return new List<BaseController>(controllers.Values);
    }
    
    /// <summary>
    /// 检查指定类型的Controller是否存在
    /// </summary>
    public bool HasController<T>() where T : BaseController
    {
        return controllers.ContainsKey(typeof(T));
    }
    
    /// <summary>
    /// 手动注册Controller
    /// </summary>
    public void RegisterController<T>(T controller) where T : BaseController
    {
        Type controllerType = typeof(T);
        
        if (controllers.ContainsKey(controllerType))
        {
            Utils.LogMessage(LogType.Warning, warnInitialization, $"[ControllerRegister] Controller类型 {controllerType.Name} 已存在，将被替换");
        }
        
        controllers[controllerType] = controller;
        controller.OnRegister();
        
        if (logInitialization)
        {
            Utils.LogMessage(LogType.Log, logInitialization, $"[ControllerRegister] 手动注册Controller: {controllerType.Name}");
        }
    }
    
    /// <summary>
    /// 注销Controller
    /// </summary>
    public void UnregisterController<T>() where T : BaseController
    {
        Type controllerType = typeof(T);
        
        if (controllers.TryGetValue(controllerType, out BaseController controller))
        {
            if (controller.IsInitialized)
            {
                controller.OnUnregister();
            }
            
            controllers.Remove(controllerType);
            
            if (logInitialization)
            {
                Utils.LogMessage(LogType.Log, logInitialization, $"[ControllerRegister] 注销Controller: {controllerType.Name}");
            }
        }
    }

    
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitializeOnLoad()
    {
        // 在场景建立前调用
    }

}
