using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

public class ManagerReferUserGenerator : EditorWindow
{
    private string outputPath = "Assets/Scripts/MVC/Manager/ManagerRefer.cs";
    
    // 父类名称
    private string baseManagerName = "BaseManager";
    
    // 有效的后缀
    private List<string> managerSuffixes = new List<string> 
    { 
        "Manager"
    };
    
    // 要包含的目录（用户脚本）
    private List<string> includePaths = new List<string>
    {
        "Assets/Scripts",
    };
    
    // 要排除的文件名关键词（如第三方库）
    private List<string> excludeKeywords = new List<string>
    {
        "Google",
        "Facebook",
        "ManagerRegister",
    };
    
    private Vector2 scrollPosition;
    private List<ManagerClassInfo> detectedManagers = new List<ManagerClassInfo>();
    
    [System.Serializable]
    public class ManagerClassInfo
    {
        public string FullTypeName;
        public string TypeName;
        public string VariableName;
        public string ScriptPath; // 脚本完整路径
        public bool Include = true;
        
        public ManagerClassInfo(string typeName, string scriptPath)
        {
            TypeName = typeName;
            ScriptPath = scriptPath;
            
            // 生成变量名
            VariableName = GenerateVariableName(typeName);
        }
        
        private string GenerateVariableName(string typeName)
        {
            return typeName;
        }
    }
    
    [MenuItem("Tools/Generate/ManagerRefer生成")]
    public static void ShowWindow()
    {
        GetWindow<ManagerReferUserGenerator>("Manager Refer Generator - 继承BaseManager");
    }
    
    private void OnEnable()
    {
        // 自动扫描用户脚本
        ScanUserScriptsForManagers();
    }
    
    void OnGUI()
    {
        GUILayout.Label("Manager Refer 代码生成器 - 继承BaseManager", EditorStyles.boldLabel);
        EditorGUILayout.Space(10);
        
        // 配置选项
        outputPath = EditorGUILayout.TextField("输出路径:", outputPath);
        
        EditorGUILayout.Space(10);
        GUILayout.Label($"检测到的继承 BaseManager 的类: {detectedManagers.Count}", EditorStyles.boldLabel);
        
        // 操作按钮
        EditorGUILayout.BeginHorizontal();
        {
            if (GUILayout.Button("重新扫描", GUILayout.Width(100)))
            {
                ScanUserScriptsForManagers();
            }
            
            if (GUILayout.Button("全选", GUILayout.Width(60)))
            {
                foreach (var c in detectedManagers) c.Include = true;
            }
            
            if (GUILayout.Button("全不选", GUILayout.Width(60)))
            {
                foreach (var c in detectedManagers) c.Include = false;
            }
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(5);
        
        // 显示检测到的 Manager 列表
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            {
                foreach (var manager in detectedManagers)
                {
                    EditorGUILayout.BeginHorizontal();
                    {
                        manager.Include = EditorGUILayout.Toggle(manager.Include, GUILayout.Width(20));
                        
                        // 类型名
                        EditorGUILayout.LabelField(manager.TypeName, GUILayout.Width(180));
                        
                        // 变量名
                        EditorGUILayout.LabelField(manager.VariableName, GUILayout.Width(120));
                        
                        // 路径（简化的）
                        string displayPath = manager.ScriptPath;
                        if (displayPath.StartsWith(Application.dataPath))
                        {
                            displayPath = "Assets" + displayPath.Substring(Application.dataPath.Length);
                        }
                        EditorGUILayout.LabelField(Path.GetDirectoryName(displayPath), EditorStyles.miniLabel);
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndScrollView();
        
        EditorGUILayout.Space(20);
        
        // 生成按钮
        EditorGUILayout.BeginHorizontal();
        {
            if (GUILayout.Button("生成代码", GUILayout.Height(40)))
            {
                GenerateCode();
                AssetDatabase.Refresh();
            }
            
            if (GUILayout.Button("导出列表", GUILayout.Width(100)))
            {
                ExportManagerList();
            }
        }
        EditorGUILayout.EndHorizontal();
    }
    
    private void ScanUserScriptsForManagers()
    {
        detectedManagers.Clear();
        
        // 获取项目中所有的 C# 脚本
        string[] allScripts = Directory.GetFiles(Application.dataPath, "*.cs", SearchOption.AllDirectories);
        
        foreach (string scriptPath in allScripts)
        {
            // 检查是否需要排除
            if (ShouldExcludeScript(scriptPath))
            {
                continue;
            }
            
            string fileName = Path.GetFileNameWithoutExtension(scriptPath);
            
            // 检查是否继承了 BaseManager
            if (IsInheritsBaseManager(fileName, scriptPath))
            {
                detectedManagers.Add(new ManagerClassInfo(fileName, scriptPath));
            }
        }
        
        // 按类型名排序
        detectedManagers = detectedManagers
            .OrderBy(c => c.TypeName)
            .ToList();
        
        Debug.Log($"检测到 {detectedManagers.Count} 个继承 BaseManager 的类");
    }
    
    private bool ShouldExcludeScript(string scriptPath)
    {
        // 转换为统一的小写路径用于比较
        string normalizedPath = scriptPath.Replace('\\', '/').ToLower();
        
        // 1. 排除不在指定目录的文件
        foreach (string excludeDir in includePaths)
        {
            string excludeDirLower = excludeDir.Replace('\\', '/').ToLower();
            if (!normalizedPath.Contains(excludeDirLower))
            {
                return true;
            }
        }
        
        // 2. 排除 Packages 目录（Unity Package Manager）
        if (normalizedPath.Contains("/packages/") || normalizedPath.Contains("\\packages\\"))
        {
            return true;
        }
        
        // 3. 排除文件名包含第三方关键词
        string fileName = Path.GetFileName(scriptPath);
        foreach (string keyword in excludeKeywords)
        {
            if (fileName.Contains(keyword))
            {
                return true;
            }
        }
        
        // 4. 排除 Unity 自动生成的脚本
        if (fileName.StartsWith("AssemblyInfo") || 
            fileName.StartsWith("Unity") ||
            fileName.Contains("Generated") ||
            fileName.EndsWith(".Designer"))
        {
            return true;
        }
        
        // 5. 检查是否在用户自定义的 Assets 目录下（基本路径判断）
        string relativePath = scriptPath.Substring(Application.dataPath.Length + 1);
        string[] pathParts = relativePath.Split(Path.DirectorySeparatorChar);
        
        // 6. 排除BaseManager本身
        if (fileName == baseManagerName)
        {
            return true;
        }
        
        // 如果脚本在 Assets 根目录下的特殊文件夹中，可能不是用户脚本
        if (pathParts.Length > 0)
        {
            string firstFolder = pathParts[0].ToLower();
            if (firstFolder == "editor" || firstFolder == "plugins" || firstFolder == "standard assets")
            {
                return true;
            }
        }
        
        return false;
    }
    
    private bool IsInheritsBaseManager(string fileName, string scriptPath)
    {
        try
        {
            string[] lines = File.ReadAllLines(scriptPath, Encoding.UTF8);
            
            bool hasClassDefinition = false;
            bool inheritsBaseManager = false;
            string currentClassName = "";
            
            // 正则表达式匹配类定义
            Regex classRegex = new Regex(@"^\s*(public\s+)?class\s+(\w+)");
            Regex inheritanceRegex = new Regex(@"^\s*(public\s+)?class\s+\w+\s*:\s*(.*)");
            
            foreach (string line in lines)
            {
                // 检查类定义
                Match classMatch = classRegex.Match(line);
                if (classMatch.Success)
                {
                    string className = classMatch.Groups[2].Value;
                    if (className == fileName)
                    {
                        hasClassDefinition = true;
                        currentClassName = className;
                        
                        // 检查继承关系
                        Match inheritanceMatch = inheritanceRegex.Match(line);
                        if (inheritanceMatch.Success)
                        {
                            string inheritanceChain = inheritanceMatch.Groups[2].Value;
                            // 检查是否直接或间接继承 BaseManager
                            if (inheritanceChain.Contains(baseManagerName))
                            {
                                inheritsBaseManager = true;
                                break;
                            }
                        }
                    }
                }
                
                // 如果已经找到类定义，检查后续行中的继承关系
                if (hasClassDefinition && currentClassName == fileName)
                {
                    // 检查是否在后续行中继承 BaseManager（处理跨行继承）
                    if (line.Contains(":"))
                    {
                        string[] parts = line.Split(':');
                        if (parts.Length > 1)
                        {
                            if (parts[1].Contains(baseManagerName))
                            {
                                inheritsBaseManager = true;
                                break;
                            }
                        }
                    }
                }
            }
            
            return hasClassDefinition && inheritsBaseManager;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"读取脚本 {scriptPath} 时出错: {e.Message}");
            return false;
        }
    }
    
    private void GenerateCode()
    {
        var selectedManagers = detectedManagers.Where(c => c.Include).ToList();
        
        if (selectedManagers.Count == 0)
        {
            EditorUtility.DisplayDialog("警告", "请至少选择一个 Manager", "确定");
            return;
        }
        
        StringBuilder sb = new StringBuilder();
        
        // 添加文件头
        sb.AppendLine("// =====================================================");
        sb.AppendLine("// 自动生成的 Manager 引用类");
        sb.AppendLine("// 生成时间: " + System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        sb.AppendLine("// 包含 " + selectedManagers.Count + " 个继承 BaseManager 的类");
        sb.AppendLine("// =====================================================");
        sb.AppendLine();
        sb.AppendLine("using UnityEngine;");
        sb.AppendLine();
        
        // 添加 BaseManager 的 using（如果需要）
        // bool needsBaseManagerUsing = false;
        // foreach (var manager in selectedManagers)
        // {
        //     if (manager.TypeName != "BaseManager")
        //     {
        //         needsBaseManagerUsing = true;
        //         break;
        //     }
        // }
        //
        // if (needsBaseManagerUsing)
        // {
        //     sb.AppendLine("// 请确保以下命名空间存在，或添加相应的 using 语句");
        //     sb.AppendLine("// using YourNamespace; // 根据实际情况修改");
        //     sb.AppendLine();
        // }
        
        // 类注释
        sb.AppendLine("/// <summary>");
        sb.AppendLine("/// Manager 快速引用器 - 仅继承 BaseManager 的类");
        sb.AppendLine("/// 自动生成，请勿手动修改");
        sb.AppendLine("/// </summary>");
        sb.AppendLine("public static class ManagerRefer");
        sb.AppendLine("{");
        
        // 生成字段和属性
        foreach (var manager in selectedManagers)
        {
            // 字段
            string fieldName = "_" + char.ToLower(manager.VariableName[0]) + manager.VariableName.Substring(1);
            sb.AppendLine($"    private static {manager.TypeName} {fieldName};");
            
            // 属性
            sb.AppendLine($"    public static {manager.TypeName} {manager.VariableName}");
            sb.AppendLine( "    {");
            sb.AppendLine( "        get");
            sb.AppendLine( "        {");
            sb.AppendLine($"            return {fieldName} ??= ManagerRegister.Instance?.GetManager<{manager.TypeName}>();");
            sb.AppendLine( "        }");
            sb.AppendLine( "    }");
            sb.AppendLine();
        }
        
        // 添加通用获取方法
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// 通用获取 Manager 方法");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    public static T Get<T>() where T : BaseManager");
        sb.AppendLine("    {");
        sb.AppendLine("        return ManagerRegister.Instance?.GetManager<T>();");
        sb.AppendLine("    }");
        sb.AppendLine();
        
            
        // 根据字符串名称获取 Manager
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// 根据类型名称字符串获取 Manager");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    /// <param name=\"managerName\">Manager 类型名称</param>");
        sb.AppendLine("    /// <returns>BaseManager 实例，如果未找到则返回 null</returns>");
        sb.AppendLine("    public static BaseManager GetByName(string managerName)");
        sb.AppendLine("    {");
        sb.AppendLine("        if (string.IsNullOrEmpty(managerName))");
        sb.AppendLine("        {");
        sb.AppendLine("            Debug.LogWarning(\"Manager 名称不能为空\");");
        sb.AppendLine("            return null;");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        // 使用 switch 语句根据名称返回对应的 Manager");
        sb.AppendLine("        switch (managerName)");
        sb.AppendLine("        {");
    
        // 为每个选中的 Manager 生成 case 语句
        foreach (var manager in selectedManagers)
        {
            sb.AppendLine($"            case \"{manager.TypeName}\":");
            sb.AppendLine($"                return {manager.VariableName};");
        }
    
        sb.AppendLine("            default:");
        sb.AppendLine("                Debug.LogWarning($\"未找到名为 {managerName} 的 Manager\");");
        sb.AppendLine("                return null;");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine();
        
        // 添加重置方法
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// 重置所有 Manager 引用（场景切换时调用）");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    public static void ResetAll()");
        sb.AppendLine("    {");
        
        foreach (var manager in selectedManagers)
        {
            string fieldName = "_" + char.ToLower(manager.VariableName[0]) + manager.VariableName.Substring(1);
            sb.AppendLine($"        {fieldName} = null;");
        }
        
        sb.AppendLine("    }");
        
        // 结束类
        sb.AppendLine("}");
        
        // 确保输出目录存在
        string directory = Path.GetDirectoryName(outputPath);
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
        
        // 写入文件
        File.WriteAllText(outputPath, sb.ToString(), Encoding.UTF8);
        
        Debug.Log($"✅ ManagerRefer.cs 已生成到: {outputPath}");
        Debug.Log($"✅ 包含 {selectedManagers.Count} 个继承 BaseManager 的类");
        
        string result = $"已生成 ManagerRefer.cs\n" +
                       $"路径: {outputPath}\n" +
                       $"包含 {selectedManagers.Count} 个继承 BaseManager 的类:\n" +
                       string.Join(", ", selectedManagers.Select(c => c.TypeName));
        
        EditorUtility.DisplayDialog("生成成功", result, "确定");
    }
    
    private void ExportManagerList()
    {
        var selected = detectedManagers.Where(c => c.Include).ToList();
        
        string exportPath = EditorUtility.SaveFilePanel(
            "导出 Manager 列表",
            Application.dataPath,
            "ManagerList_" + System.DateTime.Now.ToString("yyyyMMdd"),
            "txt");
        
        if (string.IsNullOrEmpty(exportPath))
            return;
        
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("继承 BaseManager 的类列表");
        sb.AppendLine("生成时间: " + System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        sb.AppendLine("总数: " + selected.Count);
        sb.AppendLine();
        sb.AppendLine("序号 | 类型名 | 变量名 | 文件路径");
        sb.AppendLine("----|--------|--------|----------");
        
        for (int i = 0; i < selected.Count; i++)
        {
            var manager = selected[i];
            string relativePath = manager.ScriptPath;
            if (relativePath.StartsWith(Application.dataPath))
            {
                relativePath = "Assets" + relativePath.Substring(Application.dataPath.Length);
            }
            
            sb.AppendLine($"{i + 1:D3} | {manager.TypeName} | {manager.VariableName} | {relativePath}");
        }
        
        File.WriteAllText(exportPath, sb.ToString(), Encoding.UTF8);
        Debug.Log($"Manager 列表已导出到: {exportPath}");
        
        EditorUtility.RevealInFinder(exportPath);
    }
}