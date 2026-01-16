using System;
using UnityEngine;
using UnityEngine.UI;

public class ScreenImgComp : BaseStateComponent
{
    [BindChild("p_screen_img")]
    private RawImage screenImg;
    
    [SerializeField]
    public ScreenImgLayer screenImgLayer = ScreenImgLayer.UI;
    
    public enum ScreenImgLayer
    {
        UI,
        Space,
    }

    public void Start()
    {
        this.AddEventListener(EventConstant.DEBUG_SET_SCREEN_IMG, OnSetScreenImg);
        
        UIUtils.SetVisible(screenImg, false);
    }

    private void OnDestroy()
    {
        this.RemoveAllEventListener();
    }

    public class SetScreenImgEventData
    {
        public Texture2D curImg;
        public bool enabled;
        public ScreenImgLayer imgLayer;
    }
    
    // 显示或隐藏屏幕图片
    private void OnSetScreenImg(EventData eventData)
    {
        var data = eventData?.GetData<SetScreenImgEventData>();
        if (data == null) return;
        UIUtils.SetVisible(screenImg, data.enabled && data.imgLayer == this.screenImgLayer);
        if (!screenImg.texture || screenImg.texture.name != data.curImg.name)
        {
            screenImg.texture = data.curImg;
        }
    }
}
