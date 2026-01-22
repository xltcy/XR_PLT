using System;
using UnityEngine;
using UnityEngine.UI;

public class DebugVoiceTextComponent : BaseStateComponent
{
    [BindChild("p_btn_voicetext"), ButtonCallback(nameof(OnBtnVoiceTextClick))]
    private Button requestSceneJsonButton;

    [BindChild("p_input_field")]
    private InputField inputField;

    private void OnBtnVoiceTextClick()
    {
        ControllerRefer.VoiceController.ProcessVoiceRecognizeResult(inputField.text);
    }
}