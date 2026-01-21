using System;
using UniGLTF;
using UnityEngine;
using UnityEngine.UI;

public class VirtualManIntroUIMediator : BaseUIMediator
{
    [BindChild("p_btn_voice_active")]
    private VoiceActiveButton btn_voice_active;
    
    public override void OnOpen(UIParams uiParams = null)
    {

    }

    public override void OnClose()
    {
        
    }

}