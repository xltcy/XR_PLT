using System;
using System.Collections.Generic;
using System.Net;
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

    [BindChild("p_dropdown_voice_model")]
    private Dropdown dropdownVoiceModel;
    
    private List<SummaryItemData> summaryItemDataList;
    
    private MeshController meshController;
    private RelocateController relocateController;
    private SceneController sceneController;
    private bool initSMPL = false;

    // 虚拟人的prefab名字
    private class AvatarConfig
    {
        public string DisplayName;
        public string PrefabName;
        public string VoiceName;

        public AvatarConfig(string displayName, string prefabName, string voiceName)
        {
            DisplayName = displayName;
            PrefabName = prefabName;
            VoiceName = voiceName;
        }
    }

    private List<AvatarConfig> avatarNames = new List<AvatarConfig>
    {
        new AvatarConfig("yuze", "prefab_yz", AzureAuth.MaleVoiceName),
        new AvatarConfig("yangluo", "prefab_yl", AzureAuth.FemaleVoiceName),
    };

    // 语音模型下拉框只暴露当前讲解流程需要的模型。
    // MimoTTS 仍保留在 SpeechSynthesizerController 中用于调试或后续扩展，但不显示在这个正式讲解入口里。
    private class VoiceModelOption
    {
        public string DisplayName;
        public SpeechManager.SpeechSynthesisMode Mode;

        public VoiceModelOption(string displayName, SpeechManager.SpeechSynthesisMode mode)
        {
            DisplayName = displayName;
            Mode = mode;
        }
    }

    private readonly List<VoiceModelOption> voiceModelOptions = new List<VoiceModelOption>
    {
        new VoiceModelOption("cosyvoice", SpeechManager.SpeechSynthesisMode.CosyVoice),
        new VoiceModelOption("azure", SpeechManager.SpeechSynthesisMode.Azure),
    };
    

    public override void OnOpen(UIParams uiParams = null)
    {
        textDataset.text = "开始";
        dropdownSceneSelect.onValueChanged.AddListener(OnSceneSelectChanged);
        dropdownAvatarSelect.onValueChanged.AddListener(OnAvatarSelectChanged);
        dropdownVoiceModel.onValueChanged.AddListener(OnVoiceModelSelectChanged);
        this.AddEventListener(EventConstant.COMPLETE_INIT_SUMMARY, OnCompleteInitSummary);
        this.AddEventListener(EventConstant.COMPLETE_GET_SCENE_DATA, OnCompleteGetSceneData);
        this.AddEventListener(EventConstant.COMPLETE_RELOCATE_SCENE, OnCompleteRelocateScene);
        this.AddEventListener(EventConstant.PPT_REMOTE_CONNECTION_CHANGED, OnPptRemoteConnectionChanged);

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
        dropdownVoiceModel.onValueChanged.RemoveListener(OnVoiceModelSelectChanged);
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
        return GetAvatarConfig(index).PrefabName;
    }

    private AvatarConfig GetAvatarConfig(int index)
    {
        if (index >= 0 && index < avatarNames.Count)
        {
            return avatarNames[index];
        }

        return avatarNames[0];
    }

    /// <summary>
    /// 根据下拉框索引获取语音模型配置。
    /// 下拉框索引来自 UI，因此这里做边界兜底，避免 prefab 选项数量和代码列表暂时不同步时抛异常。
    /// </summary>
    private VoiceModelOption GetVoiceModelOption(int index)
    {
        if (index >= 0 && index < voiceModelOptions.Count)
        {
            return voiceModelOptions[index];
        }

        return voiceModelOptions[0];
    }

    /// <summary>
    /// 将当前 SpeechManager 的语音模式映射回下拉框索引。
    /// 这个 UI 只展示 Azure 和 CosyVoice，如果当前模式是隐藏的 MimoTTS，会返回 0 并在初始化时回退到 Azure。
    /// </summary>
    private int GetVoiceModelOptionIndex(SpeechManager.SpeechSynthesisMode mode)
    {
        for (int i = 0; i < voiceModelOptions.Count; i++)
        {
            if (voiceModelOptions[i].Mode == mode)
            {
                return i;
            }
        }

        return 0;
    }

    /// <summary>
    /// 初始化语音模型下拉框。
    /// 选项由代码生成，不依赖 prefab 内预填内容，确保界面始终只出现 azure 和 cosyvoice。
    /// </summary>
    private void InitializeVoiceModelDropdown()
    {
        dropdownVoiceModel.ClearOptions();
        dropdownVoiceModel.AddOptions(voiceModelOptions.ConvertAll(item => item.DisplayName));

        // 下拉框只提供 Azure 和 CosyVoice。若当前模式来自调试菜单且不在列表内，回退到 Azure，
        // 保证正式讲解入口不会停留在隐藏模型上。
        int selectedIndex = GetVoiceModelOptionIndex(SpeechManager.GetSynthesisMode());
        dropdownVoiceModel.SetValueWithoutNotify(selectedIndex);
        OnVoiceModelSelectChanged(selectedIndex);
    }
    
    private void SetRefreshLinkState()
    {
        PptRemoteConnectionState state = ControllerRefer.PptRemoteController.GetConnectionState();
        string stateText;
        switch (state)
        {
            case PptRemoteConnectionState.Connected:
                stateText = ControllerRefer.PptRemoteController.GetConnectionDescription();
                break;
            case PptRemoteConnectionState.Searching:
                stateText = "正在搜索 PPT 控制端...";
                break;
            default:
                stateText = "未连接 PPT 控制端，请先在电脑上启动脚本，再点击刷新按钮";
                break;
        }

        if (inputFieldRefreshLinkState)
        {
            inputFieldRefreshLinkState.text = stateText;
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
        
        btnRefreshLink.SetVisible(false);
        
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
        string host = GetInputIPv4Address();
        if (string.IsNullOrEmpty(host))
        {
            ControllerRefer.PptRemoteController.RefreshConnection();
        }
        else
        {
            ControllerRefer.PptRemoteController.RefreshConnection(host);
        }

        SetRefreshLinkState();
    }

    private void OnPptRemoteConnectionChanged(EventData eventData)
    {
        SetRefreshLinkState();
    }

    private string GetInputIPv4Address()
    {
        if (!inputFieldRefreshLinkState) return null;

        string text = inputFieldRefreshLinkState.text;
        if (string.IsNullOrWhiteSpace(text)) return null;

        string[] parts = text.Split(new[] { ' ', '\t', '\r', '\n', '(', ')', ':', ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (string part in parts)
        {
            if (IPAddress.TryParse(part, out IPAddress address) && address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            {
                return address.ToString();
            }
        }

        return null;
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

        int needShowCnt = 2;
        for (int i = 0; i < needShowCnt; i++)
        {
            options.Add(summaryItemDataList[i].sceneName);
        }
        
        //summaryItemDataList.ForEach(item => options.Add(item.sceneName));
        dropdownSceneSelect.ClearOptions();
        dropdownSceneSelect.AddOptions(options);
        
        //手动初始化一次场景选择DropDown
        OnSceneSelectChanged(dropdownSceneSelect.value);
        
        dropdownAvatarSelect.ClearOptions();
        dropdownAvatarSelect.AddOptions(avatarNames.ConvertAll(item => item.PrefabName));
        OnAvatarSelectChanged(dropdownAvatarSelect.value);

        InitializeVoiceModelDropdown();
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
        AvatarConfig avatarConfig = GetAvatarConfig(index);
        ControllerRefer.SMPLController.SelectedAvatarName = avatarConfig.PrefabName;
        ControllerRefer.SpeechManager?.SetSynthesisVoice(avatarConfig.VoiceName);
    }

    /// <summary>
    /// 响应语音模型下拉框变化。
    /// 这里只切换合成 provider，不改变虚拟人形象和男女声；男女声仍由上面的 avatar 下拉框决定。
    /// </summary>
    private void OnVoiceModelSelectChanged(int index)
    {
        VoiceModelOption option = GetVoiceModelOption(index);
        SpeechManager.SetSynthesisMode(option.Mode);
    }
    #endregion callback
    
}
