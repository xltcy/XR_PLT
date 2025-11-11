using System;
using UniGLTF;
using UnityEngine;
using UnityEngine.UI;

public class DebugUIMediator : BaseUIMediator
{
    [BindChild("p_btn_scene_json"), ButtonCallback("OnRequestSceneJsonButtonClick")]
    private Button requestSceneJsonButton;

    [BindChild("p_btn_scene_upload"), ButtonCallback("OnRequestSceneUploadButtonClick")]
    private Button requestSceneUploadButton;
    
    public void OnEnable()
    {
        
    }

    public void OnDisable()
    {
        
    }

    private void OnRequestSceneJsonButtonClick()
    {
        Debug.Log("Request Scene Json Button Clicked");
        var sceneController = ControllerRegister.Instance.GetController<SceneController>();
        if (sceneController != null)
        {
            sceneController.RequireSummaryData();
            Debug.Log("Request Summary Data");
        }
    }

    private void OnRequestSceneUploadButtonClick()
    {
        Debug.Log("Request Scene Upload Button Clicked");
        StartCoroutine(NetworkUtil.Instance.UploadSummaryData());
        Debug.Log("Upload Scene Data");
    }
    
}