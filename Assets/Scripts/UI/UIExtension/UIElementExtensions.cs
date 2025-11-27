// csharp
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

public static class UIElementExtensions
{
    #region Transform Extensions
    /// <summary>
    /// 根据节点名查找子节点
    /// </summary>
    /// <param name="current"></param>
    /// <param name="name"></param>
    /// <param name="includeInactive"></param>
    /// <returns></returns>
    public static Transform FindDeep(this Transform current, string name, bool includeInactive = true)
    {
        if (current == null) return null;
        if (string.IsNullOrEmpty(name)) return null;
        return current.FindRecursive(name, includeInactive);
    }

    /// <summary>
    /// 根据路径查找子节点，支持相对路径和深度查找
    /// </summary>
    /// <param name="current"></param>
    /// <param name="path"></param>
    /// <param name="includeInactive"></param>
    /// <returns></returns>
    public static Transform FindByPath(this Transform current, string path, bool includeInactive = true)
    {
        if (current == null) return null;
        if (string.IsNullOrEmpty(path)) return null;

        // 如果包含 '/' 使用 Unity 的相对查找，否则用深度查找
        if (path.Contains("/"))
            return current.Find(path); // 注意：Find 不会搜索非激活子对象
        return current.FindDeep(path, includeInactive);
    }

    /// <summary>
    /// 获取完整路径
    /// </summary>
    /// <param name="current"></param>
    /// <returns></returns>
    public static string GetFullPath(this Transform current)
    {
        if (current == null) return string.Empty;
        var parts = new List<string>();
        var cur = current;
        while (cur != null)
        {
            parts.Add(cur.name);
            cur = cur.parent;
        }
        parts.Reverse();
        return string.Join("/", parts);
    }

    /// <summary>
    /// 设置自己及所有子节点的激活状态
    /// </summary>
    /// <param name="parent"></param>
    /// <param name="active"></param>
    public static void SetActiveRecursively(this Transform parent, bool active)
    {
        if (parent == null) return;
        parent.gameObject.SetActive(active);
        foreach (Transform child in parent)
            child.SetActiveRecursively(active);
    }

    /// <summary>
    /// 场景内查找指定名称的Transform
    /// </summary>
    /// <param name="current"></param>
    /// <param name="name"></param>
    /// <param name="includeInactive"></param>
    /// <returns></returns>
    public static Transform FindInScene(this Transform current, string name, bool includeInactive = true)
    {
        if (string.IsNullOrEmpty(name)) return null;
        var all = UnityEngine.Object.FindObjectsOfType<GameObject>(includeInactive);
        foreach (var go in all)
        {
            if (go.name == name)
                return go.transform;
        }
        return null;
    }

    /// <summary>
    /// 查找子节点（递归）
    /// </summary>
    /// <param name="current"></param>
    /// <param name="targetName"></param>
    /// <param name="includeInactive"></param>
    /// <returns></returns>
    public static Transform FindRecursive(this Transform current, string targetName, bool includeInactive = true)
    {
        if (current.name == targetName) return current;
        foreach (Transform child in current)
        {
            if (!includeInactive && !child.gameObject.activeInHierarchy) continue;
            if (child.name == targetName) return child;
            var found = child.FindRecursive(targetName, includeInactive);
            if (found != null) return found;
        }
        return null;
    }
    #endregion Transform Extensions
    
    #region GameObject Extensions
    
    /// <summary>
    /// 设置GameObject的显示/隐藏状态
    /// </summary>
    public static void SetVisible(this GameObject go, bool visible)
    {
        go?.SetActive(visible);
    }
    
    /// <summary>
    /// 切换显示状态
    /// </summary>
    public static void ToggleVisible(this GameObject go)
    {
        go?.SetActive(!go.activeSelf);
    }
    
    /// <summary>
    /// 检查是否可见
    /// </summary>
    public static bool IsVisible(this GameObject go)
    {
        return go != null && go.gameObject != null && go.gameObject.activeInHierarchy;
    }
    
    #endregion GameObject Extensions
    
    #region Component Extensions
    /// <summary>
    /// 设置GameObject的显示/隐藏状态
    /// </summary>
    public static void SetVisible(this Component go, bool visible)
    {
        go?.gameObject.SetActive(visible);
    }
    
    /// <summary>
    /// 切换显示状态
    /// </summary>
    /// <param name="component"></param>
    public static void ToggleVisible(this Component component)
    {
        component?.gameObject.ToggleVisible();
    }
    #endregion Component Extensions

    #region Canvas Extensions
    
    /// <summary>
    /// 设置CanvasGroup的透明度方式显示/隐藏
    /// </summary>
    public static void SetVisibleAlpha(this CanvasGroup canvasGroup, bool visible, float alpha = 1f)
    {
        if (canvasGroup)
        {
            canvasGroup.alpha = visible ? alpha : 0f;
            canvasGroup.blocksRaycasts = visible;
            canvasGroup.interactable = visible;
        }
    }
    
    #endregion Canvas Extensions
    
    #region 回调绑定
    // Slider 扩展
    public static void AddValueChangeListener(this UnityEngine.UI.Slider slider, UnityEngine.Events.UnityAction<float> action)
    {
        slider.onValueChanged.AddListener(action);
    }
    
    public static void RemoveAllValueChangeListeners(this UnityEngine.UI.Slider slider)
    {
        slider.onValueChanged?.RemoveAllListeners();
    }
    
    // Dropdown 扩展
    public static void AddValueChangeListener(this UnityEngine.UI.Dropdown dropdown, UnityEngine.Events.UnityAction<int> action)
    {
        dropdown.onValueChanged.AddListener(action);
    }
    
    public static void RemoveAllValueChangeListeners(this UnityEngine.UI.Dropdown dropdown)
    {
        dropdown.onValueChanged?.RemoveAllListeners();
    }
    
    // InputField 扩展
    public static void AddValueChangeListener(this UnityEngine.UI.InputField inputField, UnityEngine.Events.UnityAction<string> action)
    {
        inputField.onValueChanged.AddListener(action);
    }
    
    public static void RemoveAllValueChangeListeners(this UnityEngine.UI.InputField inputField)
    {
        inputField.onValueChanged?.RemoveAllListeners();
    }
    
    // Toggle 扩展
    public static void AddValueChangeListener(this UnityEngine.UI.Toggle toggle, UnityEngine.Events.UnityAction<bool> action)
    {
        toggle.onValueChanged.AddListener(action);
    }
    
    public static void RemoveAllValueChangeListeners(this UnityEngine.UI.Toggle toggle)
    {
        toggle.onValueChanged?.RemoveAllListeners();
    }
    
    // TMP_InputField 扩展
    public static void AddValueChangeListener(this TMPro.TMP_InputField inputField, UnityEngine.Events.UnityAction<string> action)
    {
        inputField.onValueChanged.AddListener(action);
    }
    
    public static void RemoveAllValueChangeListeners(this TMPro.TMP_InputField inputField)
    {
        inputField.onValueChanged?.RemoveAllListeners();
    }
    
    // TMP_Dropdown 扩展
    public static void AddValueChangeListener(this TMPro.TMP_Dropdown dropdown, UnityEngine.Events.UnityAction<int> action)
    {
        dropdown.onValueChanged.AddListener(action);
    }
    
    public static void RemoveAllValueChangeListeners(this TMPro.TMP_Dropdown dropdown)
    {
        dropdown.onValueChanged?.RemoveAllListeners();
    }
    #endregion 回调绑定
}