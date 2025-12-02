using System;
using UnityEngine;
using UnityEngine.UI;

public class DebugJsonComponent : BaseStateComponent
{
    [BindChild("p_btn_scene_json"), ButtonCallback("OnRequestSceneJsonButtonClick")]
    private Button requestSceneJsonButton;

    [BindChild("p_btn_scene_upload"), ButtonCallback("OnRequestSceneUploadButtonClick")]
    private Button requestSceneUploadButton;

    private void OnRequestSceneJsonButtonClick()
    { 
        ControllerRefer.SceneController.RequireSummaryData();
    }

    private void OnRequestSceneUploadButtonClick()
    {
        StartCoroutine(NetworkUtil.Instance.UploadSummaryData());
    }
}