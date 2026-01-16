using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// UI Prefab导出器编辑器
/// </summary>
[CustomEditor(typeof(UIPrefabExporter))]
public class UIPrefabExporterEditor : Editor
{
    private UIPrefabExporter exporter;
    
    private void OnEnable()
    {
        exporter = (UIPrefabExporter)target;
    }
    
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        EditorGUILayout.Space(10);
        
        // 检测组件并显示提示
        BaseUIMediator mediator = exporter.GetComponent<BaseUIMediator>();
        CanvasGroup canvasGroup = exporter.GetComponent<CanvasGroup>();
        
        if (mediator == null)
        {
            EditorGUILayout.HelpBox("节点上没有挂载BaseUIMediator或其继承类！导出将失败。", MessageType.Warning);
        }
        
        if (canvasGroup == null)
        {
            EditorGUILayout.HelpBox("节点上没有CanvasGroup组件！导出时会自动添加。", MessageType.Info);
        }
        
        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("导出操作", EditorStyles.boldLabel);
        
        // 显示导出信息
        EditorGUILayout.HelpBox($"导出路径: {exporter.GetFullExportPath()}", MessageType.Info);
        EditorGUILayout.HelpBox($"Resources路径: {exporter.GetResourcesPath()}", MessageType.Info);
        
        // 验证是否可以导出
        string errorMessage;
        bool canExport = exporter.CanExport(out errorMessage);
        
        if (!canExport)
        {
            EditorGUILayout.HelpBox(errorMessage, MessageType.Error);
        }
        
        EditorGUILayout.Space(5);
        
        // 导出按钮
        GUI.enabled = canExport;
        if (GUILayout.Button("导出为Prefab并注册", GUILayout.Height(30)))
        {
            ExportPrefab(exporter);
        }
        GUI.enabled = true;
        
        EditorGUILayout.Space(5);
        
        if (GUILayout.Button("打开UI导出工具窗口", GUILayout.Height(25)))
        {
            UIExportWindow.ShowWindow();
        }
    }
    
    /// <summary>
    /// 导出单个Prefab
    /// </summary>
    public static void ExportPrefab(UIPrefabExporter exporter)
    {
        string errorMessage;
        if (!exporter.CanExport(out errorMessage))
        {
            EditorUtility.DisplayDialog("导出失败", errorMessage, "确定");
            return;
        }
        
        string prefabName = exporter.GetExportName();
        string fullPath = exporter.GetFullExportPath();
        string resourcesPath = exporter.GetResourcesPath();
        string sceneName = exporter.GetSceneName();
        
        // 确保必要组件存在
        exporter.EnsureComponents();
        
        // 同步UI类型到BaseUIMediator
        BaseUIMediator mediator = exporter.GetComponent<BaseUIMediator>();
        if (mediator != null)
        {
            mediator.uiPrefabName = prefabName;
            mediator.sceneName = sceneName;
            EditorUtility.SetDirty(mediator);
        }

        string mediatorName = mediator.name;
        
        // 确保目录存在
        string directory = Path.GetDirectoryName(fullPath);
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
        
        // 检查是否已注册
        CheckExistingRegistration(mediatorName, resourcesPath, out string constantStatus, out string jsonStatus);
        
        // 保存Prefab
        GameObject prefabObj = exporter.gameObject;
        bool isSuccess = false;
        
        try
        {
            // 检查是否已存在
            GameObject existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(fullPath);
            
            if (existingPrefab != null)
            {
                // 更新现有Prefab
                PrefabUtility.SaveAsPrefabAssetAndConnect(prefabObj, fullPath, InteractionMode.UserAction);
            }
            else
            {
                // 创建新Prefab
                PrefabUtility.SaveAsPrefabAsset(prefabObj, fullPath);
            }
            
            isSuccess = true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"导出Prefab失败: {e.Message}");
            EditorUtility.DisplayDialog("导出失败", $"保存Prefab时出错: {e.Message}", "确定");
            return;
        }
        
        if (isSuccess)
        {
            // 自动注册
            RegisterToUINameConstant(mediatorName);
            
            RegisterToUIRegister(mediatorName, resourcesPath);
            
            AssetDatabase.Refresh();
            
            EditorUtility.DisplayDialog("导出成功", 
                $"Prefab已导出到: {fullPath}\n" +
                $"UI名称: {prefabName}\n" +
                $"Resources路径: {resourcesPath}" +
                $"UIMediator名称: {mediatorName}", 
                "确定");
            
            Debug.Log($"UI Prefab导出成功: {prefabName} -> {fullPath}");
        }
    }
    
    /// <summary>
    /// 注册到UINameConstant
    /// </summary>
    private static void RegisterToUINameConstant(string uiName)
    {
        string constantPath = "Assets/Scripts/MVC/Manager/UIManager/UINameConstant.cs";
        
        if (!File.Exists(constantPath))
        {
            Debug.LogError($"UINameConstant.cs not found at {constantPath}");
            return;
        }
        
        string content = File.ReadAllText(constantPath);
        
        // 检查是否已经存在
        string fieldDeclaration = $"public const string {uiName} = \"{uiName}\";";
        
        if (content.Contains($"public const string {uiName}"))
        {
            Debug.Log($"UI名称 '{uiName}' 已存在于UINameConstant中，跳过注册。");
            return;
        }
        
        // 找到类的最后一个字段位置
        int lastBraceIndex = content.LastIndexOf('}');
        if (lastBraceIndex > 0)
        {
            // 在最后一个}之前插入新字段
            string newContent = content.Insert(lastBraceIndex, $"    {fieldDeclaration}\n");
            File.WriteAllText(constantPath, newContent);
            
            Debug.Log($"已将 '{uiName}' 注册到UINameConstant");
        }
    }
    
    /// <summary>
    /// 注册到ui_register.json
    /// </summary>
    private static void RegisterToUIRegister(string uiName, string resourcesPath)
    {
        string jsonPath = "Assets/Resources/UIRegister/ui_register.json";
        
        Dictionary<string, string> registerData = new Dictionary<string, string>();
        
        // 读取现有数据
        if (File.Exists(jsonPath))
        {
            try
            {
                string jsonContent = File.ReadAllText(jsonPath);
                var existingData = JsonUtility.FromJson<Dictionary<string, string>>(jsonContent);
                if (existingData != null)
                {
                    registerData = existingData;
                }
            }
            catch
            {
                // 如果解析失败，尝试手动解析
                try
                {
                    string jsonContent = File.ReadAllText(jsonPath);
                    registerData = ParseSimpleJson(jsonContent);
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"解析ui_register.json失败: {e.Message}");
                }
            }
        }
        
        // 添加或更新数据
        registerData[uiName] = resourcesPath;
        registerData[uiName] = resourcesPath;
        
        // 保存JSON（手动格式化）
        try
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine("{");
            
            var sortedData = registerData.OrderBy(kv => kv.Key);
            int count = 0;
            int total = registerData.Count;
            
            foreach (var kv in sortedData)
            {
                count++;
                sb.Append($"  \"{kv.Key}\": \"{kv.Value}\"");
                if (count < total)
                {
                    sb.AppendLine(",");
                }
                else
                {
                    sb.AppendLine();
                }
            }
            
            sb.AppendLine("}");
            
            File.WriteAllText(jsonPath, sb.ToString());
            Debug.Log($"已将 '{uiName}' 注册到ui_register.json");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"保存ui_register.json失败: {e.Message}");
        }
    }
    
    /// <summary>
    /// 简单JSON解析（用于字典格式）
    /// </summary>
    private static Dictionary<string, string> ParseSimpleJson(string json)
    {
        Dictionary<string, string> result = new Dictionary<string, string>();
        
        json = json.Trim().Trim('{', '}');
        string[] lines = json.Split(new[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries);
        
        foreach (string line in lines)
        {
            string[] parts = line.Split(':');
            if (parts.Length == 2)
            {
                string key = parts[0].Trim().Trim('"');
                string value = parts[1].Trim().Trim('"');
                result[key] = value;
            }
        }
        
        return result;
    }
    
    /// <summary>
    /// 检查现有注册信息
    /// </summary>
    private static void CheckExistingRegistration(string uiName, string resourcesPath, out string constantStatus, out string jsonStatus)
    {
        constantStatus = "未注册";
        jsonStatus = "未注册";
        
        // 检查UINameConstant
        string constantPath = "Assets/Scripts/MVC/Manager/UIManager/UINameConstant.cs";
        if (File.Exists(constantPath))
        {
            string content = File.ReadAllText(constantPath);
            if (content.Contains($"public const string {uiName}"))
            {
                constantStatus = "已注册";
            }
        }
        
        // 检查ui_register.json
        string jsonPath = "Assets/Resources/UIRegister/ui_register.json";
        if (File.Exists(jsonPath))
        {
            try
            {
                string jsonContent = File.ReadAllText(jsonPath);
                var registerData = ParseSimpleJson(jsonContent);
                
                if (registerData.ContainsKey(uiName))
                {
                    if (registerData[uiName] == resourcesPath)
                    {
                        jsonStatus = "已注册（路径正确）";
                    }
                    else
                    {
                        jsonStatus = $"已注册（路径不同: {registerData[uiName]}）";
                    }
                }
            }
            catch
            {
                jsonStatus = "解析失败";
            }
        }
        
        Debug.Log($"注册信息检查 - UI名称: {uiName}\nUINameConstant: {constantStatus}\nui_register.json: {jsonStatus}");
    }
}
