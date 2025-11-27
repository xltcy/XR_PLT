using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public class ComponentBinder : MonoBehaviour
{
    [SerializeField, HideInInspector]
    private bool autoBindOnAwake = true;
    [SerializeField, HideInInspector]
    private bool logBindResults = true;
    [SerializeField, HideInInspector]
    private bool searchInactiveNodes = true;
    
    // 缓存查找结果以提高性能
    private Dictionary<string, Transform> transformCache = new Dictionary<string, Transform>();

    [SerializeField, HideInInspector]
    private List<UnityEngine.Object> componentListCache = new List<UnityEngine.Object>();
    public Dictionary<Button, MethodInfo> BtnActionCache = new Dictionary<Button, MethodInfo>();
    
    protected virtual void Awake()
    {
        if (autoBindOnAwake)
        {
            BindComponents();
        }
    }

    protected virtual void OnDestroy()
    {
        DestroyAllButtonCallback();
        ClearCache();
    }
    
    public void BindComponents()
    {
        DestroyAllButtonCallback();
        ClearCache();
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
            
            ButtonCallbackAttribute btnAttribute = field.GetCustomAttribute<ButtonCallbackAttribute>();
            if (btnAttribute != null)
            {
                BindSingleButtonCallback(field, attribute, btnAttribute);
            }
            
            
        }
    }
    
    private void BindSingleChildComponent(FieldInfo field, BindChildAttribute attribute)
    {
        Type componentType = field.FieldType;
        
        // 查找子节点
        Transform childTransform = FindChildByName(attribute.ChildName);
        if (childTransform == null)
        {
            HandleBindError(field, attribute.Required, $"未找到名称为 '{attribute.ChildName}' 的子节点");
            return;
        }
        
        // 获取组件
        UnityEngine.Object component = (componentType == typeof(GameObject)) ? (UnityEngine.Object)childTransform.gameObject : childTransform.GetComponent(componentType);
        if (component == null)
        {
            HandleBindError(field, attribute.Required, 
                $"在节点 {childTransform.name} 上未找到组件: {componentType.Name}");
            return;
        }
        componentListCache.Add(component);
        
        // 设置字段值
        field.SetValue(this, component);
    }

    private void BindSingleButtonCallback(FieldInfo fieldInfo, BindChildAttribute attribute, ButtonCallbackAttribute btnAttribute)
    {
        Type fieldType = fieldInfo.FieldType;
        if (fieldType != typeof(Button))
        {
            HandleBindError(fieldInfo, btnAttribute.Required, $"[ComponentBinder] 字段 {fieldInfo.Name} 的类型必须是 Button");
            return;
        }
        
        // 查找按钮节点
        Transform buttonTransform = FindChildByName(attribute.ChildName);
        Button button = buttonTransform.GetComponent<Button>();
        if (button == null)
        {
            HandleBindError(fieldInfo, btnAttribute.Required, $"[ComponentBinder] 在节点 {buttonTransform.name} 上未找到 Button 组件 (字段: {fieldInfo.Name})");
            return;
        }
        
        // 通过反射获取回调方法
        MethodInfo method = GetType().GetMethod(btnAttribute.MethodName, 
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
    
        if (method == null)
        {
            HandleBindError(fieldInfo, btnAttribute.Required, 
                $"[ComponentBinder] 未找到方法: {btnAttribute.MethodName}");
            return;
        }
    
        // 创建委托并绑定
        UnityAction callback = () => method.Invoke(this, null);
        button.onClick.AddListener(callback);
        BtnActionCache.Add(button, method);
        
        if (logBindResults)
        {
            Debug.Log($"[ComponentBinder] 绑定按钮回调成功: {fieldInfo.Name} -> {buttonTransform.name}", this);
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
        if (transformCache.TryGetValue(cacheKey, out Transform value))
            return value;
        
        Transform result = transform.FindRecursive(childName, searchInactiveNodes);
        transformCache.Add(cacheKey, result);
        return result;
    }
    
    private void DestroyAllButtonCallback()
    {
        foreach (var pair in BtnActionCache)
        {
            var btn = pair.Key;
            if (btn == null || btn.onClick == null)
            {
                continue;
            }
            btn.onClick.RemoveAllListeners();
        }
    }

    private void ClearCache()
    {
        componentListCache.Clear();
        transformCache.Clear();
        BtnActionCache.Clear();
    }
    
    private void HandleBindError(FieldInfo field, bool required, string errorMessage)
    {
        if (!logBindResults) return;
        if (required)
        {
            Debug.LogError($"[ComponentBinder] {errorMessage} (字段: {field.Name})", this);
        }
        else
        {
            Debug.LogWarning($"[ComponentBinder] {errorMessage} (字段: {field.Name})", this);
        }
    }
}