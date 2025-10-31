using System;
using UniGLTF;
using UnityEngine;
using UnityEngine.UI;

public class DebugUIMediator : BaseUIMediator
{
    [BindChild("p_scene_json_btn"), ButtonCallback("OnRequestSceneJsonButtonClick")]
    private Button requestSceneJsonButton;

    public void OnEnable()
    {
    }

    public void OnDisable()
    {
    }

    private void OnRequestSceneJsonButtonClick()
    {
        Debug.Log("Request Scene Json Button Clicked");
        var sceneController = FindObjectOfType<SceneController>();
        if (sceneController != null)
        {
            sceneController.RequireSummaryData();
            Debug.Log("Request Summary Data");
        }
    }
    
}