using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class ComponentBinder : MonoBehaviour
{
    [Header("绑定设置")]
    [SerializeField]
    private bool autoBindOnAwake = true;

    [SerializeField]
    private bool logBindResults = true;

    [SerializeField]
    private bool searchInactiveNodes = true;
    
    // 缓存查找结果以提高性能
    private Dictionary<string, Transform> _transformCache = new Dictionary<string, Transform>();
    
    protected virtual void Awake()
    {
        if (autoBindOnAwake)
        {
            BindComponents();
        }
    }
    
    [ContextMenu("测试绑定组件")]
    public void BindComponents()
    {
        _transformCache.Clear();
        BindChildComponents();
        
        if (logBindResults)
        {
            Debug.Log($"[ComponentBinder] {gameObject.name} 组件绑定完成", this);
        }
    }
    
    private void BindChildComponents()
    {
        FieldInfo[] fields = GetType().GetFields(
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        
        foreach (FieldInfo field in fields)
        {
            BindChildAttribute attribute = field.GetCustomAttribute<BindChildAttribute>();
            if (attribute == null) continue;
            
            BindSingleChildComponent(field, attribute);
        }
    }
    
    private void BindSingleChildComponent(FieldInfo field, BindChildAttribute attribute)
    {
        Type componentType = field.FieldType;
        
        // 查找子节点
        Transform childTransform = FindChildByName(attribute.ChildName);
        if (childTransform == null)
        {
            HandleBindError(field, attribute, $"未找到名称为 '{attribute.ChildName}' 的子节点");
            return;
        }
        
        // 获取组件
        Component component = childTransform.GetComponent(componentType);
        if (component == null)
        {
            HandleBindError(field, attribute, 
                $"在节点 {childTransform.name} 上未找到组件: {componentType.Name}");
            return;
        }
        
        // 设置字段值
        field.SetValue(this, component);
        
        if (logBindResults)
        {
            Debug.Log($"[ComponentBinder] 绑定成功: {field.Name} -> {childTransform.name}/{componentType.Name}");
        }
    }
    
    /// <summary>
    /// 按名称查找子节点
    /// </summary>
    private Transform FindChildByName(string childName)
    {
        if (string.IsNullOrEmpty(childName))
            return transform;
            
        string cacheKey = $"{childName}";
        if (_transformCache.ContainsKey(cacheKey))
            return _transformCache[cacheKey];
        
        Transform result = null;
        
        result = FindInChildren(childName, searchInactiveNodes);
        
        _transformCache[cacheKey] = result;
        return result;
    }
    
    /// <summary>
    /// 在所有后代节点中查找
    /// </summary>
    private Transform FindInChildren(string childName, bool includeInactive)
    {
        return transform.FindRecursive(childName, includeInactive);
    }
    
    /// <summary>
    /// 递归查找单个节点
    /// </summary>
    private Transform FindRecursive(Transform current, string targetName)
    {
        if (current.name == targetName)
            return current;
            
        foreach (Transform child in current)
        {
            if (!searchInactiveNodes && !child.gameObject.activeInHierarchy)
                continue;
                
            Transform result = FindRecursive(child, targetName);
            if (result != null)
                return result;
        }
        
        return null;
    }
    
    private void HandleBindError(FieldInfo field, BindChildAttribute attribute, string errorMessage)
    {
        if (attribute.Required)
        {
            Debug.LogError($"[ComponentBinder] {errorMessage} (字段: {field.Name})", this);
        }
        else
        {
            Debug.LogWarning($"[ComponentBinder] {errorMessage} (字段: {field.Name})", this);
        }
    }
}