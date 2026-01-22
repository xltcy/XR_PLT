using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StartHudUIMediator : BaseUIMediator
{
    [BindChild("p_btn_relocate"), ButtonCallback(nameof(OnBtnRelocateClick))]
    private Button btnRelocate;
    
    [BindChild("p_btn_summon_model"), ButtonCallback(nameof(OnBtnSummonModelClick))]
    private Button btnSummonModel;
    
    [BindChild("p_btn_start"), ButtonCallback(nameof(OnBtnStartClick))]
    private Button btnStart;
    
    [BindChild("p_btn_show_model"), ButtonCallback(nameof(OnBtnShowModelClick))]
    private Button btnShowModel;
    
    [BindChild("p_btn_hide_model"), ButtonCallback(nameof(OnBtnHideModelClick))]
    private Button btnHideModel;
    
    [BindChild("p_dropdown_scene_select")]
    private Dropdown dropdownSceneSelect;
    
    [BindChild("p_text_dataset")]
    private TMP_InputField textDataset;
    
    private List<SummaryItemData> summaryItemDataList;
    
    private MeshController meshController;

    public override void OnOpen(UIParams uiParams = null)
    {
        //todo 1.添加Relocate按钮，显示、lockable
        //todo 2.添加Summon按钮显示
        dropdownSceneSelect.onValueChanged.AddListener(OnSceneSelectChanged);
        this.AddEventListener(EventConstant.COMPLETE_INIT_SUMMARY, OnCompleteInitSummary);
        this.AddEventListener(EventConstant.COMPLETE_GET_SCENE_DATA, OnCompleteGetSceneData);
        this.AddEventListener(EventConstant.COMPLETE_RELOCATE_SCENE, OnCompleteRelocateScene);

        meshController = ControllerRefer.MeshController;

        SetUI(0);
    }


    public override void OnClose()
    {
        dropdownSceneSelect.onValueChanged.RemoveListener(OnSceneSelectChanged);
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
    }
    
    private void OnBtnRelocateClick()
    {
        var relocateType = MeshController.RelocateType.Scene;
        //relocateType == 0，重定位场景；relocateType == 1，重定位声呐。

        //DebugUIMediator节点上添加开关
        if (DebugSwitch.Instance.DEBUG_FAKE_RELOCATE && Application.isEditor)
        {
            meshController.tempGetPose();
            return;
        }

        // Record Camera Pose
        meshController.camPoseT0 = meshController.GetARCameraPose();
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
        
        ControllerRefer.RelocateController.RelocateSceneRequest(rawData);
    }

    private void LoadSceneData()
    {
        ControllerRefer.SceneController.RequestSceneDataByKey(meshController.GetCurrentSummaryItemData());
    }

    private void OnBtnSummonModelClick()
    {
        meshController.ClickToSummonAtCamera(meshController.relocatedPose);
        SetUI(1);
    }

    private void OnBtnStartClick()
    {
        
    }

    private void OnToggleSceneSelect()
    {
        
    }

    private void OnBtnShowModelClick()
    {
        
    }

    private void OnBtnHideModelClick()
    {
        
    }
    
    private void OnSceneSelectChanged(int index)
    {
        meshController.SetCurrentSummaryItemData(summaryItemDataList[index]);
        SetDataSetLoc();
        LoadSceneData();
    }
    
    //设置模型选择
    private void SetDataSetLoc()
    {
        var curSummaryItem = meshController.GetCurrentSummaryItemData();
        textDataset.SetText(curSummaryItem?.sceneDataSet);
    }
    
    private void OnCompleteInitSummary(EventData eventData)
    {
        var options = new List<string>();
        summaryItemDataList = meshController.Summary;
        summaryItemDataList.ForEach(item => options.Add(item.sceneName));
        dropdownSceneSelect.ClearOptions();
        dropdownSceneSelect.AddOptions(options);
        
        //手动初始化一次场景选择DropDown
        OnSceneSelectChanged(dropdownSceneSelect.value);
    }

    private void OnCompleteGetSceneData(EventData eventData)
    {
        SetUI(1);
        ControllerRefer.VoiceController.InitLLMMessageList();
    }

    private void OnCompleteRelocateScene(EventData eventData)
    {
        SetUI(2);
    }
    
}