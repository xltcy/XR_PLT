using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 调试用的开关，在DebugSwitch节点Editor界面上修改
/// </summary>
public class DebugSwitch : Singleton<DebugSwitch>
{
    /*=========================================================================*/
    
    #region 开关
    [SerializeField, Header("Debug用虚假重定位")]
    public bool DEBUG_FAKE_RELOCATE = false; 
    [SerializeField, Header("使用网络下载json")]
    public bool DEBUG_USING_NETWORK_JSON = false;
    
    #endregion
    
    /*=========================================================================*/
    
    #region 重定位调试图片 
    [SerializeField, Header("重定位调试用图片")]
    public Texture2D DebugSonarImg;
    
    [SerializeField]
    public Texture2D DebugSceneImg;

    private Texture2D curImg;
    public string GetRelocateDebugImgPath(MeshController.RelocateType type)
    {
        Texture2D img = null;
        switch (type)
        {
            case MeshController.RelocateType.Sonar:
                img = DebugSonarImg;
                break;
            case MeshController.RelocateType.Scene:
                img = DebugSceneImg;
                break;  
        }

        curImg = img;
        
        string assetPath = AssetDatabase.GetAssetPath(img);
        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        return Path.GetFullPath(Path.Combine(projectRoot, assetPath));
    }
    
    public void ToggleImgDisplay()
    {
        this.TriggerEvent(EventConstant.DEBUG_TOGGLE_SCREEN_IMG, curImg);
    }
    
    #endregion
    
    /*=========================================================================*/
}
