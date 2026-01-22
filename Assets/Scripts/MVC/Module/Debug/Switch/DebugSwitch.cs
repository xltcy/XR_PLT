using System.IO;
using UnityEditor;
using UnityEngine;

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

    [SerializeField, Header("使用UIManager")]
    public bool DEBUG_USING_UIMANAGER = false;
    #endregion
    
    /*=========================================================================*/
    
    #region 重定位调试图片 
    [SerializeField, Header("重定位调试用图片")]
    public Texture2D DebugSonarImg;
    
    [SerializeField]
    public Texture2D DebugSceneImg;

    private Texture2D curImg;
    private bool isImgDisplay = false;
    private ScreenImgComp.ScreenImgLayer imgLayer = ScreenImgComp.ScreenImgLayer.UI;

    public string GetRelocateDebugImgPath(MeshController.RelocateType type)
    {
#if UNITY_EDITOR
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
#else
        return string.Empty;
#endif
    }

    public void ToggleImgDisplay()
    {
        isImgDisplay = !isImgDisplay;
        this.TriggerEvent(EventConstant.DEBUG_SET_SCREEN_IMG,
            new ScreenImgComp.SetScreenImgEventData
            {
                curImg = curImg,
                enabled = isImgDisplay,
                imgLayer = imgLayer
            }
        );
    }

    public void ToggleImgLayer()
    {
        imgLayer = imgLayer == ScreenImgComp.ScreenImgLayer.UI ? ScreenImgComp.ScreenImgLayer.Space : ScreenImgComp.ScreenImgLayer.UI;
        this.TriggerEvent(EventConstant.DEBUG_SET_SCREEN_IMG,
            new ScreenImgComp.SetScreenImgEventData
            {
                curImg = curImg,
                enabled = isImgDisplay,
                imgLayer = imgLayer
            }
        );
    }
    
    #endregion
    
    /*=========================================================================*/
}
