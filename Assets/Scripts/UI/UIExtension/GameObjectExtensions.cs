using UnityEngine;

public static class GameObjectExtensions
{
    /// <summary>
    /// 获取或添加组件
    /// </summary>
    public static T GetOrAddComponent<T>(this GameObject gameObject) where T : Component
    {
        if (gameObject == null) return null;
        T component = gameObject.GetComponent<T>();
        if (component == null)
            component = gameObject.AddComponent<T>();
        return component;
    }
}