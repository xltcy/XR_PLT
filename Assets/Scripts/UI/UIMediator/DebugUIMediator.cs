using System;
using UniGLTF;
using UnityEngine;
using UnityEngine.UI;

public class DebugUIMediator : BaseUIMediator
{
    [BindChild("p_DebugSwitch"), ButtonCallback(nameof(OnDebugSwitchButtonClick))]
    private Button debugSwitchButton;
    [BindChild("p_SummonSonar"), ButtonCallback(nameof(OnSummonSonarClick))]
    private Button SummonSonar;
    [BindChild("p_HideSonar"), ButtonCallback(nameof(OnHideSonarClick))]
    private Button HideSonar;
    [BindChild("p_ShowSonar"), ButtonCallback(nameof(OnShowSonarClick))]
    private Button ShowSonar;

    [BindChild("p_DebugView")]
    private Transform debugView;
    
    [BindChild("p_Mesh")]
    private GameObject UI_调试_Mesh;
    [BindChild("p_屏幕")]
    private GameObject UI_调试_屏幕;
    [BindChild("p_相机")]
    private GameObject UI_调试_相机;
    [BindChild("p_json")]
    private Transform jsonComp;
    [BindChild("p_voicetext")]
    private Transform voiceTextComp;

    [BindChild("p_Mesh控制台")]
    private Toggle Toggle_Mesh;
    [BindChild("p_屏幕控制台")]
    private Toggle Toggle_屏幕;
    [BindChild("p_相机控制台")]
    private Toggle Toggle_相机;
    [BindChild("p_json_toggle")]
    private Toggle Toggle_json;
    [BindChild("p_toggle_voicetext")]
    private Toggle toggeleVoiceText;

    public override void OnOpen(UIParams uiParams = null)
    {
        // 添加事件监听
        Toggle_Mesh.AddValueChangeListener(value => {
            UI_调试_Mesh.SetVisible(value);
        });
        Toggle_屏幕.AddValueChangeListener(value => {
            UI_调试_屏幕.SetVisible(value);
        });
        Toggle_相机.AddValueChangeListener(value => {
            UI_调试_相机.SetVisible(value);
        });
        Toggle_json.AddValueChangeListener(value => {
            jsonComp.SetVisible(value);
        });
        toggeleVoiceText.AddValueChangeListener(value => {
            voiceTextComp.SetVisible(value);
        });
        
        Debug.Log("DebugUIMediator OnOpen");
    }


    public override void OnClose()
    {
        // 清理事件监听
        Toggle_Mesh?.RemoveAllValueChangeListeners();
        Toggle_屏幕?.RemoveAllValueChangeListeners();
        Toggle_相机?.RemoveAllValueChangeListeners();
        Toggle_json?.RemoveAllValueChangeListeners();
        toggeleVoiceText?.RemoveAllValueChangeListeners();
    }

    private void OnDebugSwitchButtonClick()
    {
        UIUtils.ToggleVisible(debugView);
    }
    private void OnSummonSonarClick()
    {
        ControllerRefer.MeshController.ClickToSummonSonarAtCamera();
    }
    private void OnHideSonarClick()
    {
        ControllerRefer.MeshController.HideSonarRender();
    }
    private void OnShowSonarClick()
    {
        ControllerRefer.MeshController.ShowSonarRender();
    }
}