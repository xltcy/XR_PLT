using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DebugCameraComponent : BaseStateComponent
{
    [BindChild("Cx")]
    private TMP_InputField cx_Input;
    
    [BindChild("Cy")]
    private TMP_InputField cy_Input;
    
    [BindChild("focal")]
    private TMP_InputField focal_Input;
    
    [BindChild("Cy调节")]
    private Slider cy滑动条;
    
    [BindChild("cx_cy_focal_text")]
    private Text cx_cy_focal;
    
    private float cx;

    private float cy;

    private float focal;

    private float cx_cy = 1.6f;

    private Camera arCamera;
    private EditModeManager editModeManager;

    private void Start()
    {
        editModeManager = ControllerRegister.Instance.GetController<EditModeManager>();
        arCamera = editModeManager.GetARCamera();
    }

    private void OnEnable()
    {
        cx_Input?.AddValueChangeListener(cxChange);
        cy_Input?.AddValueChangeListener(cyChange);
        focal_Input?.AddValueChangeListener(focalChange);
        cy滑动条?.AddValueChangeListener(cySlide);
    }

    private void OnDisable()
    {
        cx_Input?.RemoveAllValueChangeListeners();
        cy_Input?.RemoveAllValueChangeListeners();
        focal_Input?.RemoveAllValueChangeListeners();
        cy滑动条?.RemoveAllValueChangeListeners();
    }
    
    void Update()
    {
        if (cx_cy_focal != null)
        {
            cx_cy_focal.text = "cx = " + arCamera.GetComponent<Camera>().sensorSize.x + "\n" + "cy = " + arCamera.GetComponent<Camera>().sensorSize.y + "\n" + "focal = " + arCamera.GetComponent<Camera>().focalLength;
        }
    }
    
    void cxChange(string value)
    {
        cx = Utils.StrToFloat(value);
        cy = cx / cx_cy;
        arCamera.GetComponent<Camera>().sensorSize = new Vector2(cx, cy);
    }

    void cyChange(string value)
    {
        cy = Utils.StrToFloat(value);
        cx = cy * cx_cy;
        arCamera.GetComponent<Camera>().sensorSize = new Vector2(cx, cy);
    }

    void focalChange(string value)
    {
        focal = Utils.StrToFloat(value);
        arCamera.GetComponent<Camera>().focalLength = focal;
    }

    void cySlide(float value)
    {
        cy = value;
        cx = cy * cx_cy;
        arCamera.GetComponent<Camera>().sensorSize = new Vector2(cx, cy);
    }

}