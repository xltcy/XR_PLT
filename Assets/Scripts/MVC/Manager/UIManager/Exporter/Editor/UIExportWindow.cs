using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// UI导出工具窗口
/// </summary>
public class UIExportWindow : EditorWindow
{
    private Vector2 scrollPosition;
    private List<UIPrefabExporter> exporters = new List<UIPrefabExporter>();
    private Dictionary<UIPrefabExporter, bool> selectedExporters = new Dictionary<UIPrefabExporter, bool>();
    
    private bool showOnlyValid = false;
    private string searchFilter = "";
    
    [MenuItem("Tools/UI管理/UI导出工具")]
    public static void ShowWindow()
    {
        UIExportWindow window = GetWindow<UIExportWindow>("UI导出工具");
        window.minSize = new Vector2(600, 400);
        window.Show();
    }
    
    private void OnEnable()
    {
        RefreshExporterList();
    }
    
    private void OnGUI()
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("UI Prefab导出工具", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("在场景中找到所有挂载了UIPrefabExporter组件的GameObject，可以批量导出为Prefab并自动注册。", MessageType.Info);
        
        EditorGUILayout.Space(10);
        
        // 工具栏
        EditorGUILayout.BeginHorizontal();
        {
            if (GUILayout.Button("刷新列表", GUILayout.Width(100)))
            {
                RefreshExporterList();
            }
            
            GUILayout.FlexibleSpace();
            
            EditorGUILayout.LabelField("搜索:", GUILayout.Width(50));
            searchFilter = EditorGUILayout.TextField(searchFilter, GUILayout.Width(200));
            
            showOnlyValid = EditorGUILayout.Toggle("只显示有效", showOnlyValid, GUILayout.Width(100));
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(5);
        
        // 统计信息
        int totalCount = exporters.Count;
        int validCount = exporters.Count(e => e.CanExport(out _));
        int selectedCount = selectedExporters.Count(kv => kv.Value);
        
        EditorGUILayout.BeginHorizontal();
        {
            EditorGUILayout.LabelField($"总数: {totalCount}  |  有效: {validCount}  |  已选择: {selectedCount}", EditorStyles.miniLabel);
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(5);
        
        // 批量操作按钮
        EditorGUILayout.BeginHorizontal();
        {
            if (GUILayout.Button("全选", GUILayout.Width(100)))
            {
                SelectAll(true);
            }
            
            if (GUILayout.Button("全不选", GUILayout.Width(100)))
            {
                SelectAll(false);
            }
            
            if (GUILayout.Button("反选", GUILayout.Width(100)))
            {
                InvertSelection();
            }
            
            GUILayout.FlexibleSpace();
            
            GUI.backgroundColor = Color.green;
            GUI.enabled = selectedCount > 0;
            if (GUILayout.Button($"导出选中的({selectedCount})", GUILayout.Width(150), GUILayout.Height(25)))
            {
                ExportSelected();
            }
            GUI.enabled = true;
            GUI.backgroundColor = Color.white;
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(10);
        
        // 导出器列表
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        {
            DrawExporterList();
        }
        EditorGUILayout.EndScrollView();
    }
    
    /// <summary>
    /// 刷新导出器列表
    /// </summary>
    private void RefreshExporterList()
    {
        exporters.Clear();
        selectedExporters.Clear();
        
        // 查找场景中所有的UIPrefabExporter
        UIPrefabExporter[] foundExporters = FindObjectsOfType<UIPrefabExporter>(true);
        
        foreach (var exporter in foundExporters)
        {
            exporters.Add(exporter);
            selectedExporters[exporter] = false;
        }
        
        Debug.Log($"找到 {exporters.Count} 个UI导出器");
    }
    
    /// <summary>
    /// 绘制导出器列表
    /// </summary>
    private void DrawExporterList()
    {
        if (exporters.Count == 0)
        {
            EditorGUILayout.HelpBox("场景中没有找到UIPrefabExporter组件。\n请在需要导出的UI节点上添加UIPrefabExporter组件。", MessageType.Warning);
            return;
        }
        
        foreach (var exporter in exporters)
        {
            if (exporter == null) continue;
            
            // 过滤
            string errorMessage;
            bool isValid = exporter.CanExport(out errorMessage);
            
            if (showOnlyValid && !isValid)
                continue;
            
            if (!string.IsNullOrEmpty(searchFilter))
            {
                string exportName = exporter.GetExportName().ToLower();
                string goName = exporter.gameObject.name.ToLower();
                string filter = searchFilter.ToLower();
                
                if (!exportName.Contains(filter) && !goName.Contains(filter))
                    continue;
            }
            
            // 绘制导出器条目
            DrawExporterItem(exporter, isValid, errorMessage);
        }
    }
    
    /// <summary>
    /// 绘制单个导出器条目
    /// </summary>
    private void DrawExporterItem(UIPrefabExporter exporter, bool isValid, string errorMessage)
    {
        EditorGUILayout.BeginVertical("box");
        {
            EditorGUILayout.BeginHorizontal();
            {
                // 复选框
                bool isSelected = selectedExporters.ContainsKey(exporter) && selectedExporters[exporter];
                bool newSelected = EditorGUILayout.Toggle(isSelected, GUILayout.Width(20));
                if (newSelected != isSelected)
                {
                    selectedExporters[exporter] = newSelected;
                }
                
                // 状态图标
                if (isValid)
                {
                    EditorGUILayout.LabelField(new GUIContent("✓", "可以导出"), GUILayout.Width(20));
                }
                else
                {
                    GUI.color = Color.red;
                    EditorGUILayout.LabelField(new GUIContent("✗", errorMessage), GUILayout.Width(20));
                    GUI.color = Color.white;
                }
                
                // GameObject名称
                EditorGUILayout.LabelField(exporter.gameObject.name, EditorStyles.boldLabel, GUILayout.Width(150));
                
                // 导出名称
                EditorGUILayout.LabelField($"→ {exporter.GetExportName()}", GUILayout.Width(150));
                
                GUILayout.FlexibleSpace();
                
                // 选择按钮
                if (GUILayout.Button("选择", GUILayout.Width(50)))
                {
                    Selection.activeGameObject = exporter.gameObject;
                    EditorGUIUtility.PingObject(exporter.gameObject);
                }
                
                // 单独导出按钮
                GUI.enabled = isValid;
                if (GUILayout.Button("导出", GUILayout.Width(50)))
                {
                    UIPrefabExporterEditor.ExportPrefab(exporter);
                }
                GUI.enabled = true;
            }
            EditorGUILayout.EndHorizontal();
            
            // 详细信息
            EditorGUILayout.BeginHorizontal();
            {
                GUILayout.Space(40);
                EditorGUILayout.LabelField($"路径: {exporter.GetResourcesPath()}", EditorStyles.miniLabel);
            }
            EditorGUILayout.EndHorizontal();
            
            // 错误信息
            if (!isValid)
            {
                EditorGUILayout.BeginHorizontal();
                {
                    GUILayout.Space(40);
                    EditorGUILayout.HelpBox(errorMessage, MessageType.Error);
                }
                EditorGUILayout.EndHorizontal();
            }
        }
        EditorGUILayout.EndVertical();
        
        EditorGUILayout.Space(2);
    }
    
    /// <summary>
    /// 全选/全不选
    /// </summary>
    private void SelectAll(bool select)
    {
        foreach (var exporter in exporters)
        {
            if (exporter != null)
            {
                selectedExporters[exporter] = select;
            }
        }
    }
    
    /// <summary>
    /// 反选
    /// </summary>
    private void InvertSelection()
    {
        foreach (var exporter in exporters)
        {
            if (exporter != null)
            {
                selectedExporters[exporter] = !selectedExporters[exporter];
            }
        }
    }
    
    /// <summary>
    /// 导出选中的
    /// </summary>
    private void ExportSelected()
    {
        List<UIPrefabExporter> toExport = selectedExporters
            .Where(kv => kv.Value && kv.Key != null)
            .Select(kv => kv.Key)
            .ToList();
        
        if (toExport.Count == 0)
        {
            EditorUtility.DisplayDialog("提示", "没有选中任何导出器", "确定");
            return;
        }
        
        int successCount = 0;
        int failCount = 0;
        
        EditorUtility.DisplayProgressBar("导出UI Prefab", "正在导出...", 0);
        
        try
        {
            for (int i = 0; i < toExport.Count; i++)
            {
                UIPrefabExporter exporter = toExport[i];
                
                EditorUtility.DisplayProgressBar("导出UI Prefab", 
                    $"正在导出: {exporter.GetExportName()} ({i + 1}/{toExport.Count})", 
                    (float)(i + 1) / toExport.Count);
                
                string errorMessage;
                if (exporter.CanExport(out errorMessage))
                {
                    try
                    {
                        UIPrefabExporterEditor.ExportPrefab(exporter);
                        successCount++;
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"导出 {exporter.GetExportName()} 失败: {e.Message}");
                        failCount++;
                    }
                }
                else
                {
                    Debug.LogWarning($"跳过无效的导出器: {exporter.gameObject.name} - {errorMessage}");
                    failCount++;
                }
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
        
        AssetDatabase.Refresh();
        
        string message = $"导出完成！\n成功: {successCount}\n失败: {failCount}";
        EditorUtility.DisplayDialog("导出结果", message, "确定");
        
        Debug.Log($"批量导出完成 - 成功: {successCount}, 失败: {failCount}");
    }
}
