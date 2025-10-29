using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class ComponentExtensions
{
    /// <summary>
    /// 设置GameObject的显示/隐藏状态
    /// </summary>
    public static void SetVisible(this Component component, bool visible)
    {
        if (component != null && component.gameObject != null)
        {
            component.gameObject.SetActive(visible);
        }
    }
    
    /// <summary>
    /// 设置CanvasGroup的透明度方式显示/隐藏
    /// </summary>
    public static void SetVisibleAlpha(this CanvasGroup canvasGroup, bool visible, float alpha = 1f)
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = visible ? alpha : 0f;
            canvasGroup.blocksRaycasts = visible;
            canvasGroup.interactable = visible;
        }
    }
    
    /// <summary>
    /// 切换显示状态
    /// </summary>
    public static void ToggleVisible(this Component component)
    {
        if (component != null && component.gameObject != null)
        {
            component.gameObject.SetActive(!component.gameObject.activeSelf);
        }
    }
    
    /// <summary>
    /// 检查是否可见
    /// </summary>
    public static bool IsVisible(this Component component)
    {
        return component != null && component.gameObject != null && component.gameObject.activeInHierarchy;
    }
}