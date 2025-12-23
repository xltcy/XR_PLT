using System;
using UnityEngine;
using UnityEngine.UI;

public class ScreenImgComp : BaseStateComponent
{
    [BindChild("p_screen_img")]
    private RawImage screenImg;

    public void Start()
    {
        this.AddEventListener(EventConstant.DEBUG_TOGGLE_SCREEN_IMG, OnToggleScreenImg);
        
        UIUtils.SetVisible(screenImg, false);
    }

    private void OnDestroy()
    {
        this.RemoveAllEventListener();
    }
    
    // 显示或隐藏屏幕图片
    private void OnToggleScreenImg(EventData eventData)
    {
        var data = eventData?.GetData<Texture2D>();
        UIUtils.ToggleVisible(screenImg);
        if (!data) return;
        if (!screenImg.texture || screenImg.texture.name != data.name)
        {
            screenImg.texture = data;
        }
    }
}
