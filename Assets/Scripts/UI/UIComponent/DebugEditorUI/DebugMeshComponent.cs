using System;
using UnityEngine;
using UnityEngine.UI;

public class DebugMeshComponent : BaseStateComponent
{
    [BindChild("p_Mesh操作类型")]
    private Dropdown dropDownOpType;

    [BindChild("p_Mesh操作幅度")]
    private Dropdown dropdownOpAmp;

    [BindChild("p_Mesh操作对象")]
    private Dropdown dropdownOpTargetType;
    
    [BindChild("p_Mesh_OperationSpace")]
    private Dropdown dropdownOpSpace;
    
    [BindChild("p_模型大小调节")]
    private Slider sliderModelScale;

    [BindChild("p_Left"), ButtonCallback(nameof(OnBtnLeftClick))]
    private Button btnLeft;
    [BindChild("p_Right"), ButtonCallback(nameof(OnBtnRightClick))]
    private Button btnRight;
    [BindChild("p_Up"), ButtonCallback(nameof(OnBtnUpClick))]
    private Button btnUp;
    [BindChild("p_Down"), ButtonCallback(nameof(OnBtnDownClick))]
    private Button btnDown;
    [BindChild("p_Forward"), ButtonCallback(nameof(OnBtnForwardClick))]
    private Button btnForward;
    [BindChild("p_Back"), ButtonCallback(nameof(OnBtnBackClick))]
    private Button btnBack;

    private float trans_amp = 0.1f;
    private float rot_amp = 1f;
    private float ratio = 1;
    
    private EditModeManager.OperationTarget targetType;
    EditModeManager.OperationType operationType;
    EditModeManager.OperationSpace operationSpace = EditModeManager.OperationSpace.Camera;
    GameObject meshObj;

    private void OnEnable()
    {
        sliderModelScale?.AddValueChangeListener(OnModelSliderChange);
        dropdownOpAmp?.AddValueChangeListener(OnOperationAmpChange);
        dropdownOpTargetType?.AddValueChangeListener(OnOperationTargetChange);
        dropDownOpType?.AddValueChangeListener(OnOperationTypeChange);
        dropdownOpSpace?.AddValueChangeListener(OnOperationSpaceChange);
        
        // 初始化操作对象的类型，以获取物体
        if (dropdownOpTargetType)
        {
            OnOperationTargetChange(dropdownOpTargetType.value);
        }
    }

    private void OnDisable()
    {
        sliderModelScale?.RemoveAllValueChangeListeners();
        dropdownOpAmp?.RemoveAllValueChangeListeners();
        dropdownOpTargetType?.RemoveAllValueChangeListeners();
        dropDownOpType?.RemoveAllValueChangeListeners();
        dropdownOpSpace?.RemoveAllValueChangeListeners();
    }

    /// <summary>
    /// 统一处理物体变换
    /// </summary>
    /// <param name="opDir"></param>
    private void ProcessGoTransform(EditModeManager.OperationDirection opDir)
    {
        float stepLen = 1;
        switch (operationType)
        {
            case EditModeManager.OperationType.Move:
                stepLen = trans_amp * ratio * MeshController.GetModelScale(ControllerRefer.EditModeManager.GetMeshObj(EditModeManager.OperationTarget.Mesh));
                break;
            case EditModeManager.OperationType.Rotate:
                stepLen = rot_amp * ratio;
                break;
            case EditModeManager.OperationType.Scale:
                stepLen = ratio;
                break;
            
        }
        ControllerRefer.EditModeManager.ProcessGoTransform(meshObj, operationType, opDir, stepLen, operationSpace);
    }
    
    public void OnBtnLeftClick()
    {
        ProcessGoTransform(EditModeManager.OperationDirection.Left);
    }

    public void OnBtnRightClick()
    {
        ProcessGoTransform(EditModeManager.OperationDirection.Right);
    }
    

    public void OnBtnUpClick()
    {
        ProcessGoTransform(EditModeManager.OperationDirection.Up);
    }

    public void OnBtnDownClick()
    {
        ProcessGoTransform(EditModeManager.OperationDirection.Down);
    }

    public void OnBtnForwardClick()
    {
        ProcessGoTransform(EditModeManager.OperationDirection.Forward);
    }

    public void OnBtnBackClick()
    {
        ProcessGoTransform(EditModeManager.OperationDirection.Back);
    }
    
    /// <summary>
    /// 模型大小Slider变化
    /// </summary>
    /// <param name="value"></param>
    void OnModelSliderChange(float value)
    {
        if(meshObj != null)
        {
            meshObj.transform.localScale = Vector3.one * value;
        }
    }

    /// <summary>
    /// 操作幅度变化
    /// </summary>
    /// <param name="v"></param>
    void OnOperationAmpChange(int v)
    {
        switch (v)
        {
            case 0:ratio = 1; break;
            case 1:ratio = 2; break;
            case 2:ratio = 5; break;
            case 3:ratio = 10;break;
            default:ratio = 1; break;
        }
    }

    /// <summary>
    /// 操作目标类型变化
    /// </summary>
    /// <param name="value"></param>
    void OnOperationTargetChange(int value)
    {
        EditModeManager.OperationTarget newType;
        switch (value)
        {
            case 1:
                newType = EditModeManager.OperationTarget.Ground;
                break;
            case 0:
            default:
                newType = EditModeManager.OperationTarget.Mesh;
                break;
        }

        if (meshObj == null || targetType != newType)
        {
            targetType = newType;
            meshObj = ControllerRefer.EditModeManager.GetMeshObj(targetType);
        }
    }

    /// <summary>
    /// 操作类型变化
    /// </summary>
    /// <param name="value"></param>
    void OnOperationTypeChange(int value)
    {
        switch (value)
        {
            case 0:
                operationType = EditModeManager.OperationType.Move;
                break;
            case 1:
                operationType = EditModeManager.OperationType.Rotate;
                break;
            case 2:
                operationType = EditModeManager.OperationType.Scale;
                break;
        }
    }
    
    void OnOperationSpaceChange(int value)
    {
        switch (value)
        {
            case 0:
                operationSpace = EditModeManager.OperationSpace.Camera;
                break;
            case 1:
                operationSpace = EditModeManager.OperationSpace.World;
                break;
            case 2:
                operationSpace = EditModeManager.OperationSpace.Local;
                break;
        }
    }
}