using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GxlIntroduceStartHudUIMediator : BaseUIMediator
{
    [BindChild("p_btn_relocate"), ButtonCallback(nameof(OnBtnRelocateClick))]
    private Button btnRelocate;
    
    [BindChild("p_btn_summon_model"), ButtonCallback(nameof(OnBtnSummonModelClick))]
    private Button btnSummonModel;
    
    [BindChild("p_btn_start"), ButtonCallback(nameof(OnBtnStartClick))]
    private Button btnStart;
    
    [BindChild("p_btn_refresh_link"), ButtonCallback(nameof(OnBtnRefreshLinkClick))]
    private Button btnRefreshLink;
    
    [BindChild("p_inputfield_refresh_link_state")]
    private TMP_InputField inputFieldRefreshLinkState;
    
    [BindChild("p_btn_show_model"), ButtonCallback(nameof(OnBtnShowModelClick))]
    private Button btnShowModel;
    
    [BindChild("p_btn_hide_model"), ButtonCallback(nameof(OnBtnHideModelClick))]
    private Button btnHideModel;
    
    [BindChild("p_dropdown_scene_select")]
    private Dropdown dropdownSceneSelect;
    
    [BindChild("p_text_dataset")]
    private TMP_InputField textDataset;
    
    [BindChild("p_dropdown_avatar_select")]
    private Dropdown dropdownAvatarSelect;
    
    private List<SummaryItemData> summaryItemDataList;
    
    private MeshController meshController;
    private RelocateController relocateController;
    private SceneController sceneController;
    private bool initSMPL = false;

    // 虚拟人的prefab名字
    private List<string> avatarNames = new List<string>
    {
        "prefab_yz",
        "prefab_yl"
    };
    

    public override void OnOpen(UIParams uiParams = null)
    {
        textDataset.text = "开始";
        dropdownSceneSelect.SetVisible(false);
        dropdownSceneSelect.onValueChanged.AddListener(OnSceneSelectChanged);
        dropdownAvatarSelect.onValueChanged.AddListener(OnAvatarSelectChanged);
        this.AddEventListener(EventConstant.COMPLETE_INIT_SUMMARY, OnCompleteInitSummary);
        this.AddEventListener(EventConstant.COMPLETE_GET_SCENE_DATA, OnCompleteGetSceneData);
        this.AddEventListener(EventConstant.COMPLETE_RELOCATE_SCENE, OnCompleteRelocateScene);

        meshController = ControllerRefer.MeshController;
        relocateController = ControllerRefer.RelocateController;
        sceneController = ControllerRefer.SceneController;

        initSMPL = false;
        
        btnStart.SetVisible(false);
        SetUI(0);
    }


    public override void OnClose()
    {
        dropdownSceneSelect.onValueChanged.RemoveListener(OnSceneSelectChanged);
        dropdownAvatarSelect.onValueChanged.RemoveListener(OnAvatarSelectChanged);
        this.RemoveAllEventListener();
    }

    /// <summary>
    /// 设置UI状态，0-未获取场景数据；1-可重定位；2-可生成模型
    /// </summary>
    /// <param name="index"></param>
    private void SetUI(int index)
    {
        btnRelocate.SetVisible(index == 1);
        btnSummonModel.SetVisible(index == 2);
        btnRelocate.SetVisible(index == 1);

        SetRefreshLinkState();
    }

    private void SetModelBtnGroup(bool modelVisible)
    {
        btnShowModel.SetVisible(!modelVisible);
        btnHideModel.SetVisible(modelVisible);
    }
    
    private void LoadSceneData()
    {
        ControllerRefer.SceneController.RequestSceneDataByKey(sceneController.GetCurrentSummaryItemData());
    }
    
    private string GetAvatarName(int index)
    {
        if (index >= 0 && index < avatarNames.Count)
        {
            return avatarNames[index];
        }
        return avatarNames[0];
    }
    
    private void SetRefreshLinkState()
    {
        string text = "未连接 PPT 控制端，请先在电脑上启动脚本，再点击刷新按钮";
        if (ControllerRefer.PptRemoteController.IsConnected())
        {
            text = ControllerRefer.PptRemoteController.GetConnectionDescription();
        }

        if (inputFieldRefreshLinkState)
        {
            inputFieldRefreshLinkState.text = text;
        }
    }
    
    #region callback
    private void OnBtnRelocateClick()
    {
        var relocateType = RelocateController.RelocateType.Scene;

        //DebugUIMediator节点上添加开关
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
        
        relocateController.RelocateSceneRequest(rawData, null, fake);
    }
    
    private void OnBtnSummonModelClick()
    {
        meshController.ClickToSummonAtCamera(relocateController.GetPoseByEnumType(RelocateController.RelocateType.Scene));
        SetUI(1);
        SetModelBtnGroup(true);
    }

    private void OnBtnStartClick()
    {
        OnBtnHideModelClick();
        
        var sceneData = ControllerRefer.SceneController.SceneData;
        if (sceneData == null)
        {
            // Error
            Debug.Log("No available explanation point!!!");
            return;
        }
        
        if (!initSMPL)
        {
            ControllerRefer.SMPLController.SetVisible(true);
            ControllerRefer.SMPLController.InitializeSmplPosition();
            ControllerRefer.SMPLController.SetControllerTickActive(true);
            initSMPL = true;
        }
        
        if (sceneData.explanationPoints.Count == 1)
        {
            // skip selectDestination.
            ControllerRefer.SceneController.SetSelectedExplainationPoint(sceneData.explanationPoints[0].id);
            ManagerRefer.UIManager.Open(UINameConstant.VirtualManIntroUIMediator);
        }
        else
        {
            // 打开讲解点选择
            // todo 迁移逻辑到UI SwitchRunState(RunState.SelectDestination);
            ManagerRefer.UIManager.Open(UINameConstant.SelectDesUIMediator);
        }

        CloseSelf();
    }

    private void OnBtnRefreshLinkClick()
    {
        ControllerRefer.PptRemoteController.RefreshConnection();
        SetRefreshLinkState();
    }

    private void OnBtnShowModelClick()
    {
        meshController.ShowMeshRender();
        SetModelBtnGroup(true);
    }

    private void OnBtnHideModelClick()
    {
        meshController.HideMeshRender();
        SetModelBtnGroup(false);
    }
    
    private void OnSceneSelectChanged(int index)
    {
        sceneController.SetCurrentSummaryItemData(summaryItemDataList[index]);
        SetDataSetLoc();
        LoadSceneData();
    }
    
    //设置模型选择
    private void SetDataSetLoc()
    {
        var curSummaryItem = sceneController.GetCurrentSummaryItemData();
        textDataset.SetText(curSummaryItem?.sceneDataSet);
    }
    
    private void OnCompleteInitSummary(EventData eventData)
    {
        var options = new List<string>();
        summaryItemDataList = sceneController.Summary;
        summaryItemDataList.ForEach(item => options.Add(item.sceneName));
        dropdownSceneSelect.ClearOptions();
        dropdownSceneSelect.AddOptions(options);
        
        //手动初始化一次场景选择DropDown
        OnSceneSelectChanged(dropdownSceneSelect.value);
        
        dropdownAvatarSelect.ClearOptions();
        dropdownAvatarSelect.AddOptions(avatarNames);
        OnAvatarSelectChanged(dropdownAvatarSelect.value);
    }

    private void OnCompleteGetSceneData(EventData eventData)
    {
        SetUI(1);
        ControllerRefer.VoiceController.InitLLMMessageList();
        btnStart.SetVisible(false);
        btnShowModel.SetVisible(false);
        btnHideModel.SetVisible(false);
    }

    private void OnCompleteRelocateScene(EventData eventData)
    {
        SetUI(2);
        btnStart.SetVisible(true);

        OnBtnSummonModelClick();
    }

    private void OnAvatarSelectChanged(int index)
    {
        ControllerRefer.SMPLController.SelectedAvatarName = GetAvatarName(index);
    }
    #endregion callback
    
}