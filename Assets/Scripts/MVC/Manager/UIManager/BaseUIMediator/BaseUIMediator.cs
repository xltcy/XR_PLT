using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI类型枚举
/// </summary>
public enum UIType
{
    /// <summary>HUD UI</summary>
    Hud,
    /// <summary>全屏UI，会隐藏其下所有UI</summary>
    FullScreen,
    /// <summary>弹窗UI，会Cover其下UI</summary>
    Popup,
    /// <summary>提示UI，可以同时存在多个，不影响其他UI</summary>
    Tip,
    /// <summary>最上层UI，始终在最顶部</summary>
    AboveAll,
}

/// <summary>
/// UI状态枚举
/// </summary>
public enum UIState
{
    None,
    Opening,
    Opened,
    Closing,
    Closed,
    Hidden,
}

/// <summary>
/// UI打开参数基类
/// </summary>
public class UIParams
{

}

public class BaseUIMediator : ComponentBinder
{
    /// <summary>UI类型，决定UI的显示逻辑</summary>
    [Header("UI类型")]
    public UIType uiType = UIType.Popup;
    
    /// <summary>所属场景名称</summary>
    [Header("UI对应场景名(不需要手动输入)")]
    public string sceneName;
    
    /// <summary>当前UI状态（由UIManager控制）</summary>
    [HideInInspector]
    public UIState currentState = UIState.None;
    
    /// <summary>CanvasGroup组件，用于控制UI交互</summary>
    [HideInInspector]
    public CanvasGroup canvasGroup;
    
    /// <summary>最后一次打开的参数</summary>
    public UIParams OpenParams { private set; get; }
    
    protected override void Awake()
    {
        base.Awake();
        
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
    }

    /// <summary>
    /// UI打开时调用，用于初始化UI
    /// 由子类重写实现具体初始化逻辑
    /// </summary>
    /// <param name="uiParams">打开参数</param>
    public virtual void OnOpen(UIParams uiParams = null)
    {
    }

    /// <summary>
    /// UI关闭时调用，用于清理UI
    /// 由子类重写实现具体的清理逻辑
    /// </summary>
    public virtual void OnClose()
    {
    }

    /// <summary>
    /// UI显示时调用（从隐藏状态恢复）
    /// 由子类重写实现具体逻辑
    /// </summary>
    public virtual void OnShow()
    {
    }

    /// <summary>
    /// UI隐藏时调用（不销毁，只是隐藏）
    /// 由子类重写实现具体逻辑
    /// </summary>
    public virtual void OnHide()
    {
    }

    /// <summary>
    /// 当UI被其他UI覆盖时调用
    /// 由子类重写实现具体逻辑（如暂停动画、音效等）
    /// </summary>
    public virtual void OnCover()
    {
    }

    /// <summary>
    /// 当覆盖UI关闭，本UI重新显示时调用
    /// 由子类重写实现具体逻辑（如恢复动画、音效等）
    /// </summary>
    public virtual void OnUnCover()
    {
    }

    /// <summary>
    /// 保存传入参数
    /// </summary>
    /// <param name="uiParams"></param>
    public void SetParam(UIParams uiParams)
    {
        this.OpenParams = uiParams;
    }
    
    /// <summary>
    /// 获取当前打开参数
    /// </summary>
    protected T GetOpenParams<T>() where T : UIParams
    {
        return OpenParams as T;
    }

    /// <summary>
    /// 获取Mediator继承类的类名
    /// </summary>
    public string GetMediatorName()
    {
        return this.GetType().Name;
    }
}