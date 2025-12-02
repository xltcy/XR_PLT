using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;

public class ControllerReferUserGenerator : EditorWindow
{
    private string outputPath = "Assets/Scripts/MVC/ControllerRefer.cs";
    
    // 有效的后缀
    private List<string> controllerSuffixes = new List<string> 
    { 
        "Controller", "Manager"
    };
    
    // 要包含的目录（用户脚本）
    private List<string> includePaths = new List<string>
    {
        "Assets/Scripts",
    };
    
    // 要排除的文件名关键词（第三方库）
    private List<string> excludeKeywords = new List<string>
    {
        "Google",
        "Facebook",
    };
    
    private Vector2 scrollPosition;
    private List<ControllerClassInfo> detectedControllers = new List<ControllerClassInfo>();
    
    [System.Serializable]
    public class ControllerClassInfo
    {
        public string FullTypeName;
        public string TypeName;
        public string VariableName;
        public string ScriptPath; // 脚本完整路径
        public bool Include = true;
        
        public ControllerClassInfo(string typeName, string scriptPath)
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
    
    [MenuItem("Tools/Generate/ControllerRefer生成")]
    public static void ShowWindow()
    {
        GetWindow<ControllerReferUserGenerator>("Controller Refer Generator - 用户脚本");
    }
    
    private void OnEnable()
    {
        // 自动扫描用户脚本
        ScanUserScriptsForControllers();
    }
    
    void OnGUI()
    {
        GUILayout.Label("Controller Refer 代码生成器 - 仅用户脚本", EditorStyles.boldLabel);
        EditorGUILayout.Space(10);
        
        // 配置选项
        outputPath = EditorGUILayout.TextField("输出路径:", outputPath);
        
        EditorGUILayout.Space(10);
        GUILayout.Label($"检测到的用户 Controller 类: {detectedControllers.Count}", EditorStyles.boldLabel);
        
        // 操作按钮
        EditorGUILayout.BeginHorizontal();
        {
            if (GUILayout.Button("重新扫描", GUILayout.Width(100)))
            {
                ScanUserScriptsForControllers();
            }
            
            if (GUILayout.Button("全选", GUILayout.Width(60)))
            {
                foreach (var c in detectedControllers) c.Include = true;
            }
            
            if (GUILayout.Button("全不选", GUILayout.Width(60)))
            {
                foreach (var c in detectedControllers) c.Include = false;
            }
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(5);
        
        // 显示检测到的 Controller 列表
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            {
                foreach (var controller in detectedControllers)
                {
                    EditorGUILayout.BeginHorizontal();
                    {
                        controller.Include = EditorGUILayout.Toggle(controller.Include, GUILayout.Width(20));
                        
                        // 类型名
                        EditorGUILayout.LabelField(controller.TypeName, GUILayout.Width(180));
                        
                        // 变量名
                        EditorGUILayout.LabelField(controller.VariableName, GUILayout.Width(120));
                        
                        // 路径（简化的）
                        string displayPath = controller.ScriptPath;
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
                ExportControllerList();
            }
        }
        EditorGUILayout.EndHorizontal();
    }
    
    private void ScanUserScriptsForControllers()
    {
        detectedControllers.Clear();
        
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
            
            // 检查是否是 Controller 类
            if (IsControllerClass(fileName, scriptPath))
            {
                detectedControllers.Add(new ControllerClassInfo(fileName, scriptPath));
            }
        }
        
        // 按类型名排序
        detectedControllers = detectedControllers
            .OrderBy(c => c.TypeName)
            .ToList();
        
        Debug.Log($"检测到 {detectedControllers.Count} 个用户 Controller 类");
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
    
    private bool IsControllerClass(string fileName, string scriptPath)
    {
        // 检查文件名后缀
        foreach (string suffix in controllerSuffixes)
        {
            if (fileName.EndsWith(suffix) && fileName.Length > suffix.Length)
            {
                // 进一步检查文件内容，确认是 MonoBehaviour
                try
                {
                    string[] lines = File.ReadAllLines(scriptPath, Encoding.UTF8);
                    
                    // 检查文件内容
                    bool hasClassDefinition = false;
                    
                    foreach (string line in lines)
                    {
                        string trimmedLine = line.Trim();
                        
                        // 检查类定义
                        if (trimmedLine.StartsWith("public class") || trimmedLine.StartsWith("class"))
                        {
                            if (trimmedLine.Contains(fileName))
                            {
                                hasClassDefinition = true;
                            }
                        }
                        
                        // 如果已经找到所需信息，提前退出
                        if (hasClassDefinition)
                        {
                            break;
                        }
                    }
                    
                    return hasClassDefinition;
                }
                catch
                {
                    // 读取文件失败，跳过
                    return false;
                }
            }
        }
        
        return false;
    }
    
    private void GenerateCode()
    {
        var selectedControllers = detectedControllers.Where(c => c.Include).ToList();
        
        if (selectedControllers.Count == 0)
        {
            EditorUtility.DisplayDialog("警告", "请至少选择一个 Controller", "确定");
            return;
        }
        
        StringBuilder sb = new StringBuilder();
        
        // 添加文件头
        sb.AppendLine("// =====================================================");
        sb.AppendLine("// 自动生成的 Controller 引用类");
        sb.AppendLine("// 生成时间: " + System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        sb.AppendLine("// 包含 " + selectedControllers.Count + " 个用户 Controller");
        sb.AppendLine("// =====================================================");
        sb.AppendLine();
        sb.AppendLine("using UnityEngine;");
        sb.AppendLine();
        
        // 类注释
        sb.AppendLine("/// <summary>");
        sb.AppendLine("/// Controller 快速引用器 - 仅用户脚本");
        sb.AppendLine("/// 自动生成，请勿手动修改");
        sb.AppendLine("/// </summary>");
        sb.AppendLine("public static class ControllerRefer");
        sb.AppendLine("{");
        
        // 生成字段和属性
        foreach (var controller in selectedControllers)
        {
            // 字段
            string fieldName = "_" + char.ToLower(controller.VariableName[0]) + controller.VariableName.Substring(1);
            sb.AppendLine($"    private static {controller.TypeName} {fieldName};");
            
            // 属性
            sb.AppendLine($"    public static {controller.TypeName} {controller.VariableName}");
            sb.AppendLine( "    {");
            sb.AppendLine( "        get");
            sb.AppendLine( "        {");
            sb.AppendLine($"            return {fieldName} ??= ControllerRegister.Instance?.GetController<{controller.TypeName}>();");
            sb.AppendLine( "        }");
            sb.AppendLine( "    }");
            sb.AppendLine();
        }
        
        // 添加通用获取方法
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// 通用获取 Controller 方法");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    public static T Get<T>() where T : BaseController");
        sb.AppendLine("    {");
        sb.AppendLine("        return ControllerRegister.Instance?.GetController<T>();");
        sb.AppendLine("    }");
        sb.AppendLine();
        
        // 添加重置方法
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// 重置所有 Controller 引用（场景切换时调用）");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    public static void ResetAll()");
        sb.AppendLine("    {");
        
        foreach (var controller in selectedControllers)
        {
            string fieldName = "_" + char.ToLower(controller.VariableName[0]) + controller.VariableName.Substring(1);
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
        
        // 备份旧文件（如果存在）
        // if (File.Exists(outputPath))
        // {
        //     string backupPath = outputPath + ".backup_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
        //     File.Copy(outputPath, backupPath, true);
        // }
        
        // 写入文件
        File.WriteAllText(outputPath, sb.ToString(), Encoding.UTF8);
        
        Debug.Log($"✅ ControllerRefer.cs 已生成到: {outputPath}");
        Debug.Log($"✅ 包含 {selectedControllers.Count} 个用户 Controller");
        
        string result = $"已生成 ControllerRefer.cs\n" +
                       $"路径: {outputPath}\n" +
                       $"包含 {selectedControllers.Count} 个用户 Controller:\n" +
                       string.Join(", ", selectedControllers.Select(c => c.TypeName));
        
        EditorUtility.DisplayDialog("生成成功", result, "确定");
    }
    
    private void ExportControllerList()
    {
        var selected = detectedControllers.Where(c => c.Include).ToList();
        
        string exportPath = EditorUtility.SaveFilePanel(
            "导出 Controller 列表",
            Application.dataPath,
            "ControllerList_" + System.DateTime.Now.ToString("yyyyMMdd"),
            "txt");
        
        if (string.IsNullOrEmpty(exportPath))
            return;
        
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("用户 Controller 列表");
        sb.AppendLine("生成时间: " + System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        sb.AppendLine("总数: " + selected.Count);
        sb.AppendLine();
        sb.AppendLine("序号 | 类型名 | 变量名 | 文件路径");
        sb.AppendLine("----|--------|--------|----------");
        
        for (int i = 0; i < selected.Count; i++)
        {
            var controller = selected[i];
            string relativePath = controller.ScriptPath;
            if (relativePath.StartsWith(Application.dataPath))
            {
                relativePath = "Assets" + relativePath.Substring(Application.dataPath.Length);
            }
            
            sb.AppendLine($"{i + 1:D3} | {controller.TypeName} | {controller.VariableName} | {relativePath}");
        }
        
        File.WriteAllText(exportPath, sb.ToString(), Encoding.UTF8);
        Debug.Log($"Controller 列表已导出到: {exportPath}");
        
        EditorUtility.RevealInFinder(exportPath);
    }
}