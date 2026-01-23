using System;
using TickSystem;
using UniGLTF;
using UnityEngine;
using UnityEngine.UI;

public class DebugUIMediator : BaseUIMediator, ITickerUpdate
{
    [BindChild("p_DebugSwitch"), ButtonCallback(nameof(OnDebugSwitchButtonClick))]
    private Button debugSwitchButton;
    [BindChild("p_RelocateSonar"), ButtonCallback(nameof(OnRelocateSonarClick))]
    private Button RelocateSonar;
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
    
    MeshController meshController;
    RelocateController relocateController;

    private Pose sonarPose;

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

        meshController = ControllerRefer.MeshController;
        relocateController = ControllerRefer.RelocateController;
        
        
        RelocateSonar.SetVisible(true);
        SummonSonar.SetVisible(false);
        
        debugSwitchButton.SetVisible(Application.isEditor);
        
        Debug.Log("DebugUIMediator OnOpen");

        this.AddEventListener(EventConstant.COMPLETE_RELOCATE_SONAR, OnRelocateSonarComplete);
        
        TickController.RegisterTick(this);
    }

    public void Tick()
    {
        // 检测 F1 键
        if (Input.GetKeyDown(KeyCode.F1))
        {
            OnDebugSwitchButtonClick();
        }
        
        // 检测四指触摸
        if (Input.touchCount >= 4)
        {
            // 检查是否有任何手指刚刚开始触摸
            for (int i = 0; i < Input.touchCount; i++)
            {
                if (Input.GetTouch(i).phase == TouchPhase.Began)
                {
                    OnDebugSwitchButtonClick();
                    break;
                }
            }
        }
    }


    public override void OnClose()
    {
        TickController.UnRegisterTick(this);
        
        // 清理事件监听
        Toggle_Mesh?.RemoveAllValueChangeListeners();
        Toggle_屏幕?.RemoveAllValueChangeListeners();
        Toggle_相机?.RemoveAllValueChangeListeners();
        Toggle_json?.RemoveAllValueChangeListeners();
        toggeleVoiceText?.RemoveAllValueChangeListeners();
        
        this.RemoveAllEventListener();
    }

    private void OnDebugSwitchButtonClick()
    {
        UIUtils.ToggleVisible(debugView);
    }
    
    private void OnRelocateSonarClick()
    {
        var relocateType = RelocateController.RelocateType.Sonar;

        var fake = DebugSwitch.Instance.DEBUG_FAKE_RELOCATE && Application.isEditor; 
        
        byte[] rawData;
        if (Application.platform == RuntimePlatform.Android)
        {
            // load from camera android aar
            rawData = meshController.GetCameraImgRawData();
        }
        else
        {
            //尝试读取DebugSwitch中的路径
            string debugImagePath = DebugSwitch.Instance.GetRelocateDebugImgPath(relocateType);
            rawData = meshController.GetLocalImgRawData(debugImagePath);
        }
        
        relocateController.RelocateSonarRequest(rawData, fake);
    }
    
    private void OnSummonSonarClick()
    {
        ControllerRefer.MeshController.ClickToSummonSonarAtCamera(relocateController.GetPoseByEnumType(RelocateController.RelocateType.Sonar));
        
        RelocateSonar.SetVisible(true);
        SummonSonar.SetVisible(false);
    }
    private void OnHideSonarClick()
    {
        ControllerRefer.MeshController.HideSonarRender();
    }
    private void OnShowSonarClick()
    {
        ControllerRefer.MeshController.ShowSonarRender();
    }

    private void OnRelocateSonarComplete(EventData eventData)
    {
        RelocateSonar.SetVisible(false);
        SummonSonar.SetVisible(true);
    }
}