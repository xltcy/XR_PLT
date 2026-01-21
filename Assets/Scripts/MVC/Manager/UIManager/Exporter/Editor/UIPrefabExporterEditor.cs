using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
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
        
        if (mediator == null)
        {
            EditorGUILayout.HelpBox("节点上没有挂载BaseUIMediator或其继承类！导出将失败。", MessageType.Warning);
        }
        
        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("导出操作", EditorStyles.boldLabel);
        
        // 显示导出信息
        EditorGUILayout.HelpBox($"导出路径: {exporter.GetFullExportPath()}", MessageType.Info);
        // EditorGUILayout.HelpBox($"Resources路径: {exporter.GetResourcesPath()}", MessageType.Info);
        
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
            mediator.sceneName = sceneName;
            EditorUtility.SetDirty(mediator);
        }

        string mediatorName = mediator.GetMediatorName();
        
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
        
        // 创建临时克隆对象用于保存，避免影响场景中的原始对象
        GameObject tempClone = Object.Instantiate(prefabObj);
        tempClone.name = prefabObj.name; // 保持名称一致
        
        try
        {
            // 从克隆对象中移除UIPrefabExporter组件
            UIPrefabExporter exporterInClone = tempClone.GetComponent<UIPrefabExporter>();
            if (exporterInClone != null)
            {
                Object.DestroyImmediate(exporterInClone);
            }
            
            // 保存克隆对象为Prefab（不建立连接）
            PrefabUtility.SaveAsPrefabAsset(tempClone, fullPath);
            
            isSuccess = true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"导出Prefab失败: {e.Message}");
            EditorUtility.DisplayDialog("导出失败", $"保存Prefab时出错: {e.Message}", "确定");
            return;
        }
        finally
        {
            // 删除临时克隆对象
            Object.DestroyImmediate(tempClone);
        }
        
        if (isSuccess)
        {
            // 自动注册
            RegisterToUINameConstant(mediatorName);
            
            RegisterToUIRegister(mediatorName, resourcesPath);
            
            AssetDatabase.Refresh();
            
            // 只保存UI所在的场景
            UnityEngine.SceneManagement.Scene uiScene = exporter.gameObject.scene;
            bool sceneSaved = false;
            string sceneStatus = "";
            
            if (uiScene.IsValid())
            {
                sceneSaved = EditorSceneManager.SaveScene(uiScene);
                sceneStatus = sceneSaved ? $"场景 '{uiScene.name}' 已保存" : $"场景 '{uiScene.name}' 保存失败";
            }
            else
            {
                sceneStatus = "无法找到UI所在场景";
            }
            
            EditorUtility.DisplayDialog("导出成功", 
                $"Prefab已导出到: {fullPath}\n" +
                $"UI名称: {prefabName}\n" +
                $"Resources路径: {resourcesPath}\n" +
                $"UIMediator名称: {mediatorName}\n" +
                $"{sceneStatus}", 
                "确定");
            
            Debug.Log($"UI Prefab导出成功: {prefabName} -> {fullPath}\n{sceneStatus}");
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
        
        // 提取所有现有的常量声明
        Dictionary<string, string> constants = new Dictionary<string, string>();
        System.Text.RegularExpressions.Regex regex = new System.Text.RegularExpressions.Regex(
            @"public\s+const\s+string\s+(\w+)\s*=\s*""([^""]*)""\s*;");
        System.Text.RegularExpressions.MatchCollection matches = regex.Matches(content);
        
        foreach (System.Text.RegularExpressions.Match match in matches)
        {
            string key = match.Groups[1].Value;
            string value = match.Groups[2].Value;
            constants[key] = value;
        }
        
        // 添加或更新新的常量
        bool isNewEntry = !constants.ContainsKey(uiName);
        constants[uiName] = uiName;
        
        // 提取类的头部和尾部
        int classBodyStart = content.IndexOf('{');
        int classBodyEnd = content.LastIndexOf('}');
        
        if (classBodyStart < 0 || classBodyEnd < 0)
        {
            Debug.LogError("无法解析UINameConstant.cs的类结构");
            return;
        }
        
        string header = content.Substring(0, classBodyStart + 1);
        
        // 按字母排序并生成新的常量声明
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.Append(header);
        sb.AppendLine();
        
        var sortedConstants = constants.OrderBy(kv => kv.Key);
        foreach (var kv in sortedConstants)
        {
            sb.AppendLine($"    public const string {kv.Key} = \"{kv.Value}\";");
        }
        
        sb.AppendLine("}");
        
        // 写回文件
        File.WriteAllText(constantPath, sb.ToString());
        
        if (isNewEntry)
        {
            Debug.Log($"已将 '{uiName}' 注册到UINameConstant");
        }
        else
        {
            Debug.Log($"已更新 '{uiName}' 在UINameConstant中的注册");
        }
    }
    
    /// <summary>
    /// 注册到ui_register.json
    /// </summary>
    private static void RegisterToUIRegister(string uiName, string resourcesPath)
    {
        string jsonPath = "Assets/Resources/UIRegister/ui_register.json";
        
        Dictionary<string, string> registerData = UIManager.LoadUIRegisterData();
        
        // 添加或更新数据
        bool isNewEntry = !registerData.ContainsKey(uiName);
        registerData[uiName] = resourcesPath;
        
        // 按字母排序并保存JSON
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
            
            if (isNewEntry)
            {
                Debug.Log($"已将 '{uiName}' 注册到ui_register.json");
            }
            else
            {
                Debug.Log($"已更新 '{uiName}' 在ui_register.json中的注册");
            }
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
