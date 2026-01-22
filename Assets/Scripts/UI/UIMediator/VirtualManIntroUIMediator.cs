using System;
using UniGLTF;
using UnityEngine;
using UnityEngine.UI;

public class VirtualManIntroUIMediator : BaseUIMediator
{
    [BindChild("p_btn_voice_active")]
    private VoiceActiveButton voiceActiveButton;
    
    private VoiceController voiceController;
    
    public override void OnOpen(UIParams uiParams = null)
    {
        voiceController = ControllerRefer.VoiceController;
        
        voiceActiveButton.ResetBtn();
        
        voiceActiveButton.onPointerDown.AddListener(StartVoiceRecognize);
        voiceActiveButton.onPointerUp.AddListener(StopVoiceRecognize);
        this.AddEventListener(EventConstant.VOICE_RECOGNITION_END, OnVoiceRecognitionEnd);
    }

    public override void OnClose()
    {
        voiceActiveButton.onPointerDown.RemoveListener(StartVoiceRecognize);
        voiceActiveButton.onPointerUp.RemoveListener(StopVoiceRecognize);
        this.RemoveAllEventListener();
    }
    
    public void StartVoiceRecognize()
    {
        voiceController.StartVoiceRecognize();
    }
    public void StopVoiceRecognize()
    {
        voiceController.StopVoiceRecognize();
    }
    
    private void OnVoiceRecognitionEnd(EventData eventData)
    {
        voiceActiveButton.ResetBtn();
    }

}