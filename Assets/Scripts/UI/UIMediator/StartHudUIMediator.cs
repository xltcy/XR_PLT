using System;
using System.Collections.Generic;
using TMPro;
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

        meshController = ControllerRefer.MeshController;
    }


    public override void OnClose()
    {
        dropdownSceneSelect.onValueChanged.RemoveListener(OnSceneSelectChanged);
        this.RemoveAllEventListener();
    }

    private void OnBtnRelocateClick()
    {
        ControllerRefer.MeshController.ClickToGetPoseByCapture(MeshController.RelocateType.Scene);
    }

    private void OnBtnSummonModelClick()
    {
        
        
        /*SetStartState(StartState.Summoning);

        // init modelInstance
        var modelInstance = ControllerRefer.SceneController.AnalysisSceneData();
        ControllerRefer.MeshController.SetModelInstance(modelInstance);
        
        // set AstarPath ConsPos;
        Vector3 centerPos = AstarPath.active.data.recastGraph.forcedBoundsCenter;
        SMPLController.SetConsPos(centerPos);
        
        modelInstance.transform.position = relocatedPose.position * GetModelScale(modelInstance);
        modelInstance.transform.rotation = relocatedPose.rotation;

        SetStartState(StartState.Normal);*/
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
    
}