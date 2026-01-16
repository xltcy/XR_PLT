using UnityEngine;
using UnityEditor;

/// <summary>
/// UI创建工具菜单
/// </summary>
public class UICreationMenu
{
    [MenuItem("GameObject/UI/Create UI with Exporter", false, 0)]
    public static void CreateUIWithExporter()
    {
        // 创建UI GameObject
        GameObject uiObject = new GameObject("NewUI");
        
        // 添加RectTransform
        RectTransform rectTransform = uiObject.AddComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(1920, 1080);
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        
        // 添加CanvasGroup
        uiObject.AddComponent<CanvasGroup>();
        
        // 添加BaseUIMediator
        BaseUIMediator mediator = uiObject.AddComponent<BaseUIMediator>();
        mediator.uiType = UIType.Popup;
        
        // 添加UIPrefabExporter
        UIPrefabExporter exporter = uiObject.AddComponent<UIPrefabExporter>();
        exporter.exportName = "ui_new_uimediator";
        
        // 选中新创建的对象
        Selection.activeGameObject = uiObject;
        
        // 在Hierarchy中展开
        EditorGUIUtility.PingObject(uiObject);
        
        Debug.Log("已创建UI对象，请修改UI名称和配置，然后添加自定义UIMediator脚本");
    }
    
    [MenuItem("Tools/UI管理/创建UI模板")]
    public static void CreateUITemplate()
    {
        CreateUIWithExporter();
    }
    
    [MenuItem("Tools/UI管理/打开UI文件夹")]
    public static void OpenUIFolder()
    {
        string path = Application.dataPath + "/Resources/UIPrefabs";
        
        if (!System.IO.Directory.Exists(path))
        {
            System.IO.Directory.CreateDirectory(path);
        }
        
        EditorUtility.RevealInFinder(path);
    }
    
    [MenuItem("Tools/UI管理/打开UIMediator文件夹")]
    public static void OpenUIMediatorFolder()
    {
        string path = Application.dataPath + "/Scripts/UI/UIMediator";

        if (!System.IO.Directory.Exists(path))
        {
            Debug.LogError("UIMediator文件夹不存在: " + path);
            return;
        }
        
        EditorUtility.RevealInFinder(path);
    }
    
    [MenuItem("Tools/UI管理/验证UI系统配置")]
    public static void ValidateUISystem()
    {
        bool isValid = true;
        System.Text.StringBuilder report = new System.Text.StringBuilder();
        report.AppendLine("=== UI系统配置验证报告 ===\n");
        
        // 检查UIRoot
        GameObject uiRoot = GameObject.Find("UIRoot");
        if (uiRoot == null)
        {
            report.AppendLine("❌ 未找到UIRoot对象！请在场景中创建UIRoot。");
            isValid = false;
        }
        else
        {
            report.AppendLine("✓ UIRoot对象存在");
        }
        
        // 检查必要文件
        string[] requiredFiles = new string[]
        {
            "Assets/Scripts/MVC/Manager/UIManager/UIManager.cs",
            "Assets/Scripts/MVC/Manager/UIManager/BaseUIMediator/BaseUIMediator.cs",
            "Assets/Scripts/MVC/Manager/UIManager/UINameConstant.cs",
            "Assets/Resources/UIRegister/ui_register.json",
        };
        
        foreach (string file in requiredFiles)
        {
            if (System.IO.File.Exists(file))
            {
                report.AppendLine($"✓ {file}");
            }
            else
            {
                report.AppendLine($"❌ 缺少文件: {file}");
                isValid = false;
            }
        }
        
        // 检查Resources文件夹
        string resourcesPath = "Assets/Resources";
        if (!System.IO.Directory.Exists(resourcesPath))
        {
            report.AppendLine($"\n⚠ Resources文件夹不存在，将无法使用Resources.Load加载UI");
        }
        else
        {
            report.AppendLine($"\n✓ Resources文件夹存在");
        }
        
        // 检查UIPrefabs文件夹
        string uiPrefabsPath = "Assets/Resources/UIPrefabs";
        if (!System.IO.Directory.Exists(uiPrefabsPath))
        {
            report.AppendLine($"⚠ UIPrefabs文件夹不存在，建议创建用于存放UI Prefab");
        }
        else
        {
            report.AppendLine($"✓ UIPrefabs文件夹存在");
        }
        
        report.AppendLine("\n" + (isValid ? "✓ 配置验证通过！" : "❌ 配置存在问题，请修复后使用。"));
        
        Debug.Log(report.ToString());
        EditorUtility.DisplayDialog("UI系统配置验证", report.ToString(), "确定");
    }
}
