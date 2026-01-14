using UnityEngine;

/// <summary>
/// Controller和Manager的基类
/// </summary>
public class BaseManager
{
    /// <summary>
    /// Controller的初始化优先级，数值越小优先级越高
    /// </summary>
    public virtual int InitPriority => 999;

    /// <summary>
    /// Controller的初始化时机
    /// </summary>
    public virtual ManagerRegister.InitTiming InitTiming => ManagerRegister.InitTiming.OnFirstUsed;
    
    /// <summary>
    /// Controller是否已经初始化完成
    /// </summary>
    public bool IsInitialized { get; protected set; }
    
    /// <summary>
    /// 初始化Controller
    /// </summary>
    public virtual void OnRegister()
    {
        IsInitialized = true;
    }
    
    /// <summary>
    /// 清理Controller
    /// </summary>
    public virtual void OnUnregister()
    {
        IsInitialized = false;
    }
}