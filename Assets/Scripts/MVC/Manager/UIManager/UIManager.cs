using UnityEngine;
using System.Collections.Generic;
using System;

public class UIManager : BaseManager
{
    public override int InitPriority => 999;    // 低优先级初始化

    // 初始化时机：场景加载完成后
    public override ManagerRegister.InitTiming InitTiming => ManagerRegister.InitTiming.OnSceneLoaded;

    private GameObject rootTrans;
    
    // UI层级父节点
    private Transform fullScreenLayer;
    private Transform popupLayer;
    private Transform tipLayer;
    private Transform aboveAllLayer;
    
    // UI管理数据结构
    private Dictionary<string, BaseUIMediator> openedUIs = new Dictionary<string, BaseUIMediator>();
    private Dictionary<string, BaseUIMediator> cachedUIs = new Dictionary<string, BaseUIMediator>();
    private Stack<BaseUIMediator> uiStack = new Stack<BaseUIMediator>();
    
    // UI配置数据
    private Dictionary<string, string> uiRegisterData = new Dictionary<string, string>();
    
    #region 生命周期
    public override void OnRegister()
    {
        base.OnRegister();

        rootTrans = GameObject.Find("UIRoot");
        if (rootTrans == null)
        {
            Debug.LogError("UIRoot not found! Please create a UIRoot GameObject in the scene.");
            return;
        }
        
        // 初始化不同的UI层级节点
        InitUILayers();
        
        // 加载UI注册配置
        LoadUIRegisterData();
    }
    
    public override void OnUnregister()
    {
        base.OnUnregister();
        
        // 清理所有UI
        CloseAll();
        
        // 清理缓存
        foreach (var ui in cachedUIs.Values)
        {
            if (ui != null)
            {
                GameObject.Destroy(ui.gameObject);
            }
        }
        cachedUIs.Clear();
    }
    #endregion 生命周期

    #region 初始化
    /// <summary>
    /// 初始化UI层级
    /// </summary>
    private void InitUILayers()
    {
        fullScreenLayer = CreateLayer("FullScreenLayer", 0);
        popupLayer = CreateLayer("PopupLayer", 100);
        tipLayer = CreateLayer("TipLayer", 200);
        aboveAllLayer = CreateLayer("AboveAllLayer", 300);
    }
    
    /// <summary>
    /// 创建UI层级节点
    /// </summary>
    private Transform CreateLayer(string layerName, int sortingOrder)
    {
        Transform layer = rootTrans.transform.Find(layerName);
        if (layer == null)
        {
            GameObject layerObj = new GameObject(layerName);
            layerObj.transform.SetParent(rootTrans.transform);
            layerObj.transform.localPosition = Vector3.zero;
            layerObj.transform.localRotation = Quaternion.identity;
            layerObj.transform.localScale = Vector3.one;
            
            Canvas canvas = layerObj.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = sortingOrder;
            
            layer = layerObj.transform;
        }
        return layer;
    }
    
    /// <summary>
    /// 加载UI注册配置
    /// </summary>
    private void LoadUIRegisterData()
    {
        // 从ui_register.json加载配置
        TextAsset jsonFile = Resources.Load<TextAsset>("UIRegister/ui_register");
        if (jsonFile != null)
        {
            try
            {
                var data = JsonUtility.FromJson<UIRegisterDataList>(jsonFile.text);
                if (data != null && data.uiList != null)
                {
                    foreach (var item in data.uiList)
                    {
                        uiRegisterData[item.name] = item.prefabPath;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load ui_register.json: {e.Message}");
            }
        }
    }
    #endregion

    #region UI打开/关闭
    /// <summary>
    /// 打开UI（通过BaseUIMediator实例）
    /// </summary>
    public void Open(BaseUIMediator uiMediator, UIParams @params = null)
    {
        if (uiMediator == null)
        {
            Debug.LogError("UIMediator is null!");
            return;
        }
        
        OpenUIInternal(uiMediator, @params);
    }

    /// <summary>
    /// 打开UI（通过UI名称）
    /// </summary>
    /// <param name="uiPrefabName">UI名称</param>
    /// <param name="params">打开参数</param>
    public void Open(string uiPrefabName, UIParams @params = null)
    {
        if (string.IsNullOrEmpty(uiPrefabName))
        {
            Debug.LogError("UI name is null or empty!");
            return;
        }
        
        // 检查UI是否已经打开
        if (openedUIs.ContainsKey(uiPrefabName))
        {
            Debug.LogWarning($"UI {uiPrefabName} is already opened!");
            return;
        }
        
        // 检查缓存
        if (cachedUIs.ContainsKey(uiPrefabName))
        {
            BaseUIMediator ui = cachedUIs[uiPrefabName];
            cachedUIs.Remove(uiPrefabName);
            OpenUIInternal(ui, @params);
            return;
        }
        
        // 加载UI Prefab
        LoadUIMediatorPrefab(uiPrefabName, (ui) =>
        {
            if (ui != null)
            {
                OpenUIInternal(ui, @params);
            }
        });
    }
    
    /// <summary>
    /// 打开UI（通过UI名称，带泛型参数）
    /// </summary>
    public void Open<T>(string uiPrefabName, T openParams) where T : UIParams
    {
        Open(uiPrefabName, openParams as UIParams);
    }
    
    /// <summary>
    /// 内部打开UI逻辑
    /// </summary>
    private void OpenUIInternal(BaseUIMediator uiMediator, UIParams @params = null)
    {
        if (uiMediator == null) return;
        
        string uiPrefabName = uiMediator.uiPrefabName;
        
        // 根据UI类型处理其他UI
        HandleUITypeLogic(uiMediator);
        
        // 设置父节点
        SetUIParent(uiMediator);
        
        // 添加到打开列表
        openedUIs[uiPrefabName] = uiMediator;
        
        // 添加到UI栈
        if (uiMediator.uiType != UIType.Tip && uiMediator.uiType != UIType.AboveAll)
        {
            uiStack.Push(uiMediator);
        }
        
        // 设置UI状态和显示（由UIManager统一控制）
        uiMediator.currentState = UIState.Opening;
        uiMediator.gameObject.SetActive(true);
        uiMediator.currentState = UIState.Opened;
        
        // 调用OnOpen（业务逻辑由子类实现）
        uiMediator.OnOpen(@params);
    }
    
    /// <summary>
    /// 根据UI类型处理其他UI
    /// </summary>
    private void HandleUITypeLogic(BaseUIMediator newUI)
    {
        switch (newUI.uiType)
        {
            case UIType.FullScreen:
                // 隐藏所有其他UI（除了AboveAll）
                foreach (var ui in openedUIs.Values)
                {
                    if (ui != newUI && ui.uiType != UIType.AboveAll)
                    {
                        HideUI(ui);
                    }
                }
                break;
                
            case UIType.Popup:
                // 遮罩下层UI（除了Tip和AboveAll）
                foreach (var ui in openedUIs.Values)
                {
                    if (ui != newUI && ui.uiType != UIType.Tip && ui.uiType != UIType.AboveAll)
                    {
                        CoverUI(ui);
                    }
                }
                break;
                
            case UIType.Tip:
            case UIType.AboveAll:
                // 不影响其他UI，Tip可以同时存在多个
                break;
        }
    }
    
    /// <summary>
    /// 隐藏UI（由UIManager控制显示状态）
    /// </summary>
    private void HideUI(BaseUIMediator ui)
    {
        ui.gameObject.SetActive(false);
        ui.currentState = UIState.Hidden;
        
        // 调用业务逻辑回调
        ui.OnHide();
    }
    
    /// <summary>
    /// 显示UI（由UIManager控制显示状态）
    /// </summary>
    private void ShowUI(BaseUIMediator ui)
    {
        ui.gameObject.SetActive(true);
        ui.currentState = UIState.Opened;
        
        // 调用业务逻辑回调
        ui.OnShow();
    }
    
    /// <summary>
    /// 遮罩UI（由UIManager控制交互状态）
    /// </summary>
    private void CoverUI(BaseUIMediator ui)
    {
        // 调用业务逻辑回调
        ui.OnCover();
    }
    
    /// <summary>
    /// 取消遮罩UI（由UIManager控制交互状态）
    /// </summary>
    private void UnCoverUI(BaseUIMediator ui)
    {
        // 调用业务逻辑回调
        ui.OnUnCover();
    }
    
    /// <summary>
    /// 设置UI父节点
    /// </summary>
    private void SetUIParent(BaseUIMediator uiMediator)
    {
        Transform parent = null;
        
        switch (uiMediator.uiType)
        {
            case UIType.FullScreen:
                parent = fullScreenLayer;
                break;
            case UIType.Popup:
                parent = popupLayer;
                break;
            case UIType.Tip:
                parent = tipLayer;
                break;
            case UIType.AboveAll:
                parent = aboveAllLayer;
                break;
        }
        
        if (parent != null)
        {
            uiMediator.transform.SetParent(parent, false);
        }
    }
    
    /// <summary>
    /// 关闭UI（通过UI名称）
    /// </summary>
    public void Close(string uiPrefabName, bool destroy = false)
    {
        if (!openedUIs.ContainsKey(uiPrefabName))
        {
            Debug.LogWarning($"UI {uiPrefabName} is not opened!");
            return;
        }
        
        BaseUIMediator ui = openedUIs[uiPrefabName];
        CloseUIInternal(ui, destroy);
    }

    /// <summary>
    /// 关闭UI（通过BaseUIMediator实例）
    /// </summary>
    public void Close(BaseUIMediator uiMediator, bool destroy = false)
    {
        if (uiMediator == null)
        {
            Debug.LogError("UIMediator is null!");
            return;
        }
        
        CloseUIInternal(uiMediator, destroy);
    }
    
    /// <summary>
    /// 内部关闭UI逻辑
    /// </summary>
    private void CloseUIInternal(BaseUIMediator uiMediator, bool destroy)
    {
        string uiPrefabName = uiMediator.uiPrefabName;
        
        // 从打开列表移除
        if (openedUIs.ContainsKey(uiPrefabName))
        {
            openedUIs.Remove(uiPrefabName);
        }
        
        // 从UI栈移除（无论是否在栈顶都要移除）
        if (uiStack.Contains(uiMediator))
        {
            var list = new List<BaseUIMediator>(uiStack);
            list.Remove(uiMediator);
            uiStack.Clear();
            for (int i = list.Count - 1; i >= 0; i--)
            {
                uiStack.Push(list[i]);
            }
        }
        
        // 设置UI状态（由UIManager统一控制）
        uiMediator.currentState = UIState.Closing;
        
        // 调用OnClose（业务逻辑由子类实现）
        uiMediator.OnClose();
        
        // 隐藏UI（由UIManager统一控制）
        uiMediator.currentState = UIState.Closed;
        uiMediator.gameObject.SetActive(false);
        
        // 恢复被覆盖的UI
        RestoreCoveredUI(uiMediator);
        
        // 销毁或缓存
        if (destroy)
        {
            GameObject.Destroy(uiMediator.gameObject);
        }
        else
        {
            cachedUIs[uiPrefabName] = uiMediator;
        }
    }
    
    /// <summary>
    /// 恢复被覆盖的UI
    /// </summary>
    private void RestoreCoveredUI(BaseUIMediator closedUI)
    {
        if (closedUI.uiType == UIType.FullScreen)
        {
            // 显示被隐藏的UI
            foreach (var ui in openedUIs.Values)
            {
                if (ui.currentState == UIState.Hidden)
                {
                    ShowUI(ui);
                }
            }
        }
        else if (closedUI.uiType == UIType.Popup)
        {
            // 恢复被遮罩的UI
            foreach (var ui in openedUIs.Values)
            {
                if (ui.uiType != UIType.Tip && ui.uiType != UIType.AboveAll)
                {
                    UnCoverUI(ui);
                }
            }
        }
    }
    
    /// <summary>
    /// 关闭所有UI
    /// </summary>
    public void CloseAll(bool destroy = false)
    {
        List<BaseUIMediator> uisToClose = new List<BaseUIMediator>(openedUIs.Values);
        foreach (var ui in uisToClose)
        {
            CloseUIInternal(ui, destroy);
        }
        
        uiStack.Clear();
    }
    
    /// <summary>
    /// 返回上一个UI
    /// </summary>
    public void BackToLastUI()
    {
        if (uiStack.Count > 0)
        {
            BaseUIMediator currentUI = uiStack.Peek();
            CloseUIInternal(currentUI, false);
        }
    }
    #endregion
    
    #region UI加载
    /// <summary>
    /// 加载UI Prefab
    /// </summary>
    private void LoadUIMediatorPrefab(string uiPrefabName, Action<BaseUIMediator> onLoaded)
    {
        if (!uiRegisterData.ContainsKey(uiPrefabName))
        {
            Debug.LogError($"UI {uiPrefabName} is not registered in ui_register.json!");
            onLoaded?.Invoke(null);
            return;
        }
        
        string prefabPath = uiRegisterData[uiPrefabName];
        
        // 使用Addressable或Resources加载
        // 这里使用Resources作为示例
        GameObject prefab = Resources.Load<GameObject>(prefabPath);
        if (prefab != null)
        {
            GameObject uiObj = GameObject.Instantiate(prefab);
            BaseUIMediator mediator = uiObj.GetComponent<BaseUIMediator>();
            
            if (mediator == null)
            {
                Debug.LogError($"UI {uiPrefabName} does not have BaseUIMediator component!");
                GameObject.Destroy(uiObj);
                onLoaded?.Invoke(null);
                return;
            }
            
            onLoaded?.Invoke(mediator);
        }
        else
        {
            Debug.LogError($"Failed to load UI prefab: {prefabPath}");
            onLoaded?.Invoke(null);
        }
    }
    #endregion
    
    #region 辅助方法
    /// <summary>
    /// 获取已打开的UI
    /// </summary>
    public BaseUIMediator GetOpenedUI(string uiPrefabName)
    {
        return openedUIs.ContainsKey(uiPrefabName) ? openedUIs[uiPrefabName] : null;
    }
    
    /// <summary>
    /// 检查UI是否已打开
    /// </summary>
    public bool IsUIOpened(string uiPrefabName)
    {
        return openedUIs.ContainsKey(uiPrefabName);
    }
    #endregion
}

/// <summary>
/// UI注册数据列表（用于JSON反序列化）
/// </summary>
[Serializable]
public class UIRegisterDataList
{
    public List<UIRegisterItem> uiList;
}

[Serializable]
public class UIRegisterItem
{
    public string name;
    public string prefabPath;
}
