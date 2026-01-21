using UnityEngine;

/// <summary>
/// UI Prefab导出器
/// 挂载在UI节点上，用于将节点导出为Prefab并自动注册
/// </summary>
public class UIPrefabExporter : MonoBehaviour
{
    [Header("导出配置(需要填)")]
    [Tooltip("导出的Prefab名称（留空则使用GameObject名称）")]
    public string exportName;
    
    [Tooltip("导出路径（相对于Assets/Resources/）")]
    private const string exportPath = "UIPrefabs";
    
    [Header("只读信息(不需要填)")]
    [SerializeField, Tooltip("场景名称")]
    private string sceneName;
    
    [SerializeField, Tooltip("检测到的BaseUIMediator组件")]
    private BaseUIMediator detectedMediator;
    
    private void OnValidate()
    {
        // 检测必要组件
        detectedMediator = GetComponent<BaseUIMediator>();

        sceneName = GetSceneName();
    }
    
    /// <summary>
    /// 获取导出名称
    /// </summary>
    public string GetExportName()
    {
        return exportName;
        // return string.IsNullOrEmpty(exportName) ? gameObject.name : exportName;
    }
    
    /// <summary>
    /// 获取完整的导出路径
    /// </summary>
    public string GetFullExportPath()
    {
        return $"Assets/Resources/{exportPath}/{GetExportName()}.prefab";
    }
    
    /// <summary>
    /// 获取Resources相对路径（用于Resources.Load）
    /// </summary>
    public string GetResourcesPath()
    {
        return $"{exportPath}/{GetExportName()}";
    }
    
    /// <summary>
    /// 获取所在场景名称
    /// </summary>
    /// <returns></returns>
    public string GetSceneName()
    {
        sceneName = gameObject.scene.name;
        return sceneName;
    }
    
    /// <summary>
    /// 验证是否可以导出
    /// </summary>
    public bool CanExport(out string errorMessage)
    {
        errorMessage = string.Empty;
        
        // 检查是否有BaseUIMediator组件
        BaseUIMediator mediator = GetComponent<BaseUIMediator>();
        if (mediator == null)
        {
            errorMessage = "节点上没有挂载BaseUIMediator或其继承类！";
            return false;
        }
        
        // 检查导出名称
        if (string.IsNullOrEmpty(GetExportName()))
        {
            errorMessage = "导出名称不能为空！";
            return false;
        }
        
        return true;
    }
    
    /// <summary>
    /// 确保必要组件存在
    /// </summary>
    public void EnsureComponents()
    {
        // // 确保有CanvasGroup组件
        // if (GetComponent<CanvasGroup>() == null)
        // {
        //     gameObject.AddComponent<CanvasGroup>();
        //     Debug.Log($"已为 '{gameObject.name}' 添加CanvasGroup组件");
        // }
    }
}
