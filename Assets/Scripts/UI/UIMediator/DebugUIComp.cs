using System;
using UniGLTF;
using UnityEngine;
using UnityEngine.UI;

public class DebugUIComp : ComponentBinder
{
    [BindChild("p_DebugSwitch"), ButtonCallback(nameof(OnDebugSwitchButtonClick))]
    private Button debugSwitchButton;
    [BindChild("p_RelocateSonar"), ButtonCallback(nameof(OnRelocateSonarClick))]
    private Button RelocateSonar;
    [BindChild("p_SummonSonar"), ButtonCallback(nameof(OnSummonSonarClick))]
    private Button SummonSonar;
    [BindChild("p_Relocate"), ButtonCallback(nameof(OnRelocateClick))]
    private Button Relocate;
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

    [BindChild("p_Mesh控制台")]
    private Toggle Toggle_Mesh;
    [BindChild("p_屏幕控制台")]
    private Toggle Toggle_屏幕;
    [BindChild("p_相机控制台")]
    private Toggle Toggle_相机;
    [BindChild("p_json_toggle")]
    private Toggle Toggle_json;
    
    public void Start()
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
    }


    protected override void OnDestroy()
    {
        // 清理事件监听
        Toggle_Mesh?.RemoveAllValueChangeListeners();
        Toggle_屏幕?.RemoveAllValueChangeListeners();
        Toggle_相机?.RemoveAllValueChangeListeners();
        Toggle_json?.RemoveAllValueChangeListeners();
        base.OnDestroy();
    }

    private void OnDebugSwitchButtonClick()
    {
        UIUtils.ToggleVisible(debugView);
    }
    private void OnRelocateSonarClick()
    {
        ControllerRefer.MeshController.ClickToGetPoseByCapture(MeshController.RelocateType.Sonar);
    }
    private void OnSummonSonarClick()
    {
        ControllerRefer.MeshController.ClickToSummonSonarAtCamera(ControllerRefer.MeshController.relocatedSonarPose);
    }
    private void OnRelocateClick()
    {
        ControllerRefer.MeshController.ClickToGetPoseByCapture(MeshController.RelocateType.Scene);
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