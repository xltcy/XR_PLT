using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[CustomEditor(typeof(ComponentBinder), true)]
[CanEditMultipleObjects]
public class ComponentBinderEditor : Editor
{
    private bool showBindingSettings = true;
    private bool showComponentCache = false;
    private bool showButtonActionCache = false;
    private Vector2 componentScrollPos;
    private Vector2 buttonActionScrollPos;

    private SerializedProperty autoBindOnAwakeProp;
    private SerializedProperty logBindResultsProp;
    private SerializedProperty searchInactiveNodesProp;
    private SerializedProperty componentListCacheProp;

    private void OnEnable()
    {
        autoBindOnAwakeProp = serializedObject.FindProperty("autoBindOnAwake");
        logBindResultsProp = serializedObject.FindProperty("logBindResults");
        searchInactiveNodesProp = serializedObject.FindProperty("searchInactiveNodes");
        componentListCacheProp = serializedObject.FindProperty("componentListCache");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        
        DrawDefaultInspector();
        
        showBindingSettings = EditorGUILayout.Foldout(showBindingSettings, "绑定设置", true, EditorStyles.foldoutHeader);
        
        if (showBindingSettings)
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUI.indentLevel++;
            
            GUI.enabled = false;
            EditorGUILayout.PropertyField(autoBindOnAwakeProp, new GUIContent("自动绑定"));
            EditorGUILayout.PropertyField(logBindResultsProp, new GUIContent("日志输出"));
            EditorGUILayout.PropertyField(searchInactiveNodesProp, new GUIContent("搜索非活跃节点"));
            EditorGUILayout.PropertyField(componentListCacheProp, new GUIContent("组件缓存"), true);
            GUI.enabled = true;
            
            ComponentBinder binder = (ComponentBinder)target;

            EditorGUILayout.Space();
            
            // 显示Button Action缓存
            showButtonActionCache = EditorGUILayout.Foldout(showButtonActionCache, "按钮信息", true, EditorStyles.foldoutHeader);
            if (showButtonActionCache)
            {
                ShowButtonActionCacheSection("Button Action缓存", binder.BtnActionCache, ref buttonActionScrollPos);
            }
            
            EditorGUILayout.Space();
            if (GUILayout.Button("刷新绑定"))
            {
                binder.BindComponents();
                EditorUtility.SetDirty(binder);
            }
            EditorGUI.indentLevel--;
            EditorGUILayout.EndVertical();
        }

        serializedObject.ApplyModifiedProperties();
    }
    
    #region 绘制按钮回调缓存部分
    private void ShowButtonActionCacheSection(string title, Dictionary<Button, MethodInfo> cache, ref Vector2 scrollPos)
    {
        EditorGUILayout.LabelField($"{title} ({cache?.Count ?? 0} 项)");
        
        if (cache != null)
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.MaxHeight(200));
            
            int index = 0;
            foreach (var pair in cache)
            {
                DisplayPersistentCallback(serializedObject.targetObject, pair.Key, pair.Value, index);
                EditorGUILayout.Space();
                index++;
            }
            
            EditorGUILayout.EndScrollView();
        }
        
    }
    
    private void DisplayPersistentCallback(UnityEngine.Object targetObject, Component bindObject, MethodBase method, int index)
    {
        EditorGUILayout.BeginVertical(GUI.skin.box);
        {
            string methodName = method.Name;
            
            EditorGUILayout.LabelField($"回调 #{index + 1}", EditorStyles.boldLabel);
            
            // 目标对象信息
            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.LabelField("绑定对象:", GUILayout.Width(80));
                GUILayout.FlexibleSpace();
                EditorGUILayout.ObjectField(bindObject, typeof(UnityEngine.Object), true, GUILayout.ExpandWidth(true));
            }
            EditorGUILayout.EndHorizontal();
            
            // 方法名称（可点击跳转）
            EditorGUILayout.BeginHorizontal();
            {
                //获取脚本
                
                EditorGUILayout.LabelField("方法名称:", GUILayout.Width(80));
                GUILayout.FlexibleSpace();
                if (targetObject != null && !string.IsNullOrEmpty(methodName))
                {
                    // 创建可点击的方法名称按钮
                    GUIContent methodButtonContent = new GUIContent( $"{targetObject.name}:{methodName}()", "点击跳转到方法定义");
                    if (GUILayout.Button(methodButtonContent, EditorStyles.objectField))
                    {
                        JumpToMethod(targetObject, methodName);
                    }
                }
                else
                {
                    EditorGUILayout.TextField(methodName);
                }
            }
            EditorGUILayout.EndHorizontal();
            
            // 显示方法详细信息
            // if (targetObject != null && !string.IsNullOrEmpty(methodName))
            // {
            //     DisplayMethodDetails(targetObject, methodName);
            // }
        }
        EditorGUILayout.EndVertical();
        
        EditorGUILayout.Space();
    }
    
    private void DisplayMethodDetails(UnityEngine.Object targetObject, string methodName)
    {
        System.Type targetType = targetObject.GetType();
        MethodInfo method = targetType.GetMethod(methodName, 
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            
        if (method != null)
        {
            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.LabelField("脚本类型:", GUILayout.Width(80));
                
                // 脚本类型也可点击跳转
                GUIContent scriptTypeContent = new GUIContent(targetType.Name, "点击跳转到脚本");
                if (GUILayout.Button(scriptTypeContent, EditorStyles.linkLabel))
                {
                    JumpToScript(targetObject);
                }
            }
            EditorGUILayout.EndHorizontal();
            
            // 方法签名信息
            ParameterInfo[] parameters = method.GetParameters();
            string signature = $"{method.ReturnType.Name} {methodName}(";
            for (int i = 0; i < parameters.Length; i++)
            {
                signature += $"{parameters[i].ParameterType.Name} {parameters[i].Name}";
                if (i < parameters.Length - 1) signature += ", ";
            }
            signature += ")";
            
            EditorGUILayout.LabelField("方法签名:", signature);
        }
        else
        {
            EditorGUILayout.HelpBox($"找不到方法: {methodName}", MessageType.Warning);
        }
    }
    
    private void JumpToMethod(UnityEngine.Object targetObject, string methodName)
    {
        if (targetObject == null)
        {
            Debug.LogWarning("目标对象为空，无法跳转");
            return;
        }
        
        // 获取 MonoScript
        MonoScript monoScript = null;
        if (targetObject is MonoBehaviour behaviour)
        {
            monoScript = MonoScript.FromMonoBehaviour(behaviour);
        }
        else if (targetObject is ScriptableObject scriptableObject)
        {
            monoScript = MonoScript.FromScriptableObject(scriptableObject);
        }
        
        if (monoScript == null)
        {
            Debug.LogWarning($"无法获取 {targetObject.GetType().Name} 的 MonoScript");
            return;
        }
        
        // 在 Project 窗口中选中脚本
        // Selection.activeObject = monoScript;
        // EditorGUIUtility.PingObject(monoScript);
        
        // 尝试在代码编辑器中跳转到方法
        if (TryFindMethodInScript(monoScript, methodName))
        {
            Debug.Log($"已跳转到方法: {methodName}");
        }
        else
        {
            Debug.Log($"已选中脚本，但无法自动定位方法: {methodName}");
        }
    }
    
    private void JumpToScript(UnityEngine.Object targetObject)
    {
        if (targetObject == null) return;
        
        MonoScript monoScript = null;
        if (targetObject is MonoBehaviour behaviour)
        {
            monoScript = MonoScript.FromMonoBehaviour(behaviour);
        }
        else if (targetObject is ScriptableObject scriptableObject)
        {
            monoScript = MonoScript.FromScriptableObject(scriptableObject);
        }
        
        if (monoScript != null)
        {
            Selection.activeObject = monoScript;
            EditorGUIUtility.PingObject(monoScript);
            Debug.Log($"已跳转到脚本: {monoScript.name}");
        }
    }
    
    private bool TryFindMethodInScript(MonoScript monoScript, string methodName)
    {
        string scriptPath = AssetDatabase.GetAssetPath(monoScript);
        
        if (string.IsNullOrEmpty(scriptPath))
            return false;
        
        // 在 Visual Studio 或 Rider 中打开并搜索方法
        // 注意：这需要编辑器的外部工具支持
        try
        {
            // 方法1：使用 Unity 的 OpenAsset 功能
            if (AssetDatabase.OpenAsset(monoScript))
            {
                // 这里可以尝试使用反射调用编辑器的搜索功能
                // 但具体实现取决于你使用的代码编辑器
                return true;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"打开脚本时出错: {e.Message}");
        }
        
        return false;
    }
    #endregion 结束绘制按钮回调缓存部分
}
