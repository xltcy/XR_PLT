using System;
using UniGLTF;
using UnityEngine;
using UnityEngine.UI;

public class LoadingUIMediator : BaseUIMediator
{
    [BindChild("p_img_loading")]
    private Image imgLoading;
    
    public override void OnOpen(UIParams uiParams = null)
    {
        imgLoading.SetVisible(true);
    }

    public override void OnClose()
    {
        
    }
}