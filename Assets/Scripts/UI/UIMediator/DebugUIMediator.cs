using System;
using UniGLTF;
using UnityEngine;
using UnityEngine.UI;

public class DebugUIMediator : BaseUIMediator
{
    [BindChild("p_scene_json_btn")]
    private Button requestSceneJsonButton;

    public void OnEnable()
    {
        UIUtils.SetVisible(requestSceneJsonButton, true);
        if (requestSceneJsonButton != null)
        {
            requestSceneJsonButton.onClick.AddListener(OnRequestSceneJsonButtonClick);
        }
    }

    public void OnDisable()
    {
        if (requestSceneJsonButton != null)
        {
            requestSceneJsonButton.onClick.RemoveListener(OnRequestSceneJsonButtonClick);
        }
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