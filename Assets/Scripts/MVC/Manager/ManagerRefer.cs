// =====================================================
// 自动生成的 Manager 引用类
// 生成时间: 2026-01-22 20:06:40
// 包含 5 个继承 BaseManager 的类
// =====================================================

using UnityEngine;

/// <summary>
/// Manager 快速引用器 - 仅继承 BaseManager 的类
/// 自动生成，请勿手动修改
/// </summary>
public static class ManagerRefer
{
    private static CoroutineManager _coroutineManager;
    public static CoroutineManager CoroutineManager
    {
        get
        {
            return _coroutineManager ??= ManagerRegister.Instance?.GetManager<CoroutineManager>();
        }
    }

    private static EventManager _eventManager;
    public static EventManager EventManager
    {
        get
        {
            return _eventManager ??= ManagerRegister.Instance?.GetManager<EventManager>();
        }
    }

    private static NetworkServiceManager _networkServiceManager;
    public static NetworkServiceManager NetworkServiceManager
    {
        get
        {
            return _networkServiceManager ??= ManagerRegister.Instance?.GetManager<NetworkServiceManager>();
        }
    }

    private static ResourceManager _resourceManager;
    public static ResourceManager ResourceManager
    {
        get
        {
            return _resourceManager ??= ManagerRegister.Instance?.GetManager<ResourceManager>();
        }
    }

    private static UIManager _uIManager;
    public static UIManager UIManager
    {
        get
        {
            return _uIManager ??= ManagerRegister.Instance?.GetManager<UIManager>();
        }
    }

    /// <summary>
    /// 通用获取 Manager 方法
    /// </summary>
    public static T Get<T>() where T : BaseManager
    {
        return ManagerRegister.Instance?.GetManager<T>();
    }

    /// <summary>
    /// 根据类型名称字符串获取 Manager
    /// </summary>
    /// <param name="managerName">Manager 类型名称</param>
    /// <returns>BaseManager 实例，如果未找到则返回 null</returns>
    public static BaseManager GetByName(string managerName)
    {
        if (string.IsNullOrEmpty(managerName))
        {
            Debug.LogWarning("Manager 名称不能为空");
            return null;
        }

        // 使用 switch 语句根据名称返回对应的 Manager
        switch (managerName)
        {
            case "CoroutineManager":
                return CoroutineManager;
            case "EventManager":
                return EventManager;
            case "NetworkServiceManager":
                return NetworkServiceManager;
            case "ResourceManager":
                return ResourceManager;
            case "UIManager":
                return UIManager;
            default:
                Debug.LogWarning($"未找到名为 {managerName} 的 Manager");
                return null;
        }
    }

    /// <summary>
    /// 重置所有 Manager 引用（场景切换时调用）
    /// </summary>
    public static void ResetAll()
    {
        _coroutineManager = null;
        _eventManager = null;
        _networkServiceManager = null;
        _resourceManager = null;
        _uIManager = null;
    }
}
