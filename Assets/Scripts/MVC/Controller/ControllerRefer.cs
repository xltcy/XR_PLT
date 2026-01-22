// =====================================================
// 自动生成的 Controller 引用类
// 生成时间: 2026-01-22 19:38:47
// 包含 19 个继承 BaseController 的类
// =====================================================

using UnityEngine;

/// <summary>
/// Controller 快速引用器 - 仅继承 BaseController 的类
/// 自动生成，请勿手动修改
/// </summary>
public static class ControllerRefer
{
    private static CamTrajController _camTrajController;
    public static CamTrajController CamTrajController
    {
        get
        {
            return _camTrajController ??= ControllerRegister.Instance?.GetController<CamTrajController>();
        }
    }

    private static Click3DObjectManager _click3DObjectManager;
    public static Click3DObjectManager Click3DObjectManager
    {
        get
        {
            return _click3DObjectManager ??= ControllerRegister.Instance?.GetController<Click3DObjectManager>();
        }
    }

    private static EditModeManager _editModeManager;
    public static EditModeManager EditModeManager
    {
        get
        {
            return _editModeManager ??= ControllerRegister.Instance?.GetController<EditModeManager>();
        }
    }

    private static ExternalModelController _externalModelController;
    public static ExternalModelController ExternalModelController
    {
        get
        {
            return _externalModelController ??= ControllerRegister.Instance?.GetController<ExternalModelController>();
        }
    }

    private static MaterialController _materialController;
    public static MaterialController MaterialController
    {
        get
        {
            return _materialController ??= ControllerRegister.Instance?.GetController<MaterialController>();
        }
    }

    private static MeshController _meshController;
    public static MeshController MeshController
    {
        get
        {
            return _meshController ??= ControllerRegister.Instance?.GetController<MeshController>();
        }
    }

    private static MoveController _moveController;
    public static MoveController MoveController
    {
        get
        {
            return _moveController ??= ControllerRegister.Instance?.GetController<MoveController>();
        }
    }

    private static RelocateController _relocateController;
    public static RelocateController RelocateController
    {
        get
        {
            return _relocateController ??= ControllerRegister.Instance?.GetController<RelocateController>();
        }
    }

    private static RotateController _rotateController;
    public static RotateController RotateController
    {
        get
        {
            return _rotateController ??= ControllerRegister.Instance?.GetController<RotateController>();
        }
    }

    private static SceneController _sceneController;
    public static SceneController SceneController
    {
        get
        {
            return _sceneController ??= ControllerRegister.Instance?.GetController<SceneController>();
        }
    }

    private static SelectDesController _selectDesController;
    public static SelectDesController SelectDesController
    {
        get
        {
            return _selectDesController ??= ControllerRegister.Instance?.GetController<SelectDesController>();
        }
    }

    private static SMPLController _sMPLController;
    public static SMPLController SMPLController
    {
        get
        {
            return _sMPLController ??= ControllerRegister.Instance?.GetController<SMPLController>();
        }
    }

    private static SpeechManager _speechManager;
    public static SpeechManager SpeechManager
    {
        get
        {
            return _speechManager ??= ControllerRegister.Instance?.GetController<SpeechManager>();
        }
    }

    private static SwipeManager _swipeManager;
    public static SwipeManager SwipeManager
    {
        get
        {
            return _swipeManager ??= ControllerRegister.Instance?.GetController<SwipeManager>();
        }
    }

    private static TickController _tickController;
    public static TickController TickController
    {
        get
        {
            return _tickController ??= ControllerRegister.Instance?.GetController<TickController>();
        }
    }

    private static TrackingImageManager _trackingImageManager;
    public static TrackingImageManager TrackingImageManager
    {
        get
        {
            return _trackingImageManager ??= ControllerRegister.Instance?.GetController<TrackingImageManager>();
        }
    }

    private static UIStateManager _uIStateManager;
    public static UIStateManager UIStateManager
    {
        get
        {
            return _uIStateManager ??= ControllerRegister.Instance?.GetController<UIStateManager>();
        }
    }

    private static VideoPlayController _videoPlayController;
    public static VideoPlayController VideoPlayController
    {
        get
        {
            return _videoPlayController ??= ControllerRegister.Instance?.GetController<VideoPlayController>();
        }
    }

    private static VoiceController _voiceController;
    public static VoiceController VoiceController
    {
        get
        {
            return _voiceController ??= ControllerRegister.Instance?.GetController<VoiceController>();
        }
    }

    /// <summary>
    /// 通用获取 Controller 方法
    /// </summary>
    public static T Get<T>() where T : BaseController
    {
        return ControllerRegister.Instance?.GetController<T>();
    }

    /// <summary>
    /// 根据类型名称字符串获取 Controller
    /// </summary>
    /// <param name="controllerName">Controller 类型名称</param>
    /// <returns>BaseController 实例，如果未找到则返回 null</returns>
    public static BaseController GetByName(string controllerName)
    {
        if (string.IsNullOrEmpty(controllerName))
        {
            Debug.LogWarning("Controller 名称不能为空");
            return null;
        }

        // 使用 switch 语句根据名称返回对应的 Controller
        switch (controllerName)
        {
            case "CamTrajController":
                return CamTrajController;
            case "Click3DObjectManager":
                return Click3DObjectManager;
            case "EditModeManager":
                return EditModeManager;
            case "ExternalModelController":
                return ExternalModelController;
            case "MaterialController":
                return MaterialController;
            case "MeshController":
                return MeshController;
            case "MoveController":
                return MoveController;
            case "RelocateController":
                return RelocateController;
            case "RotateController":
                return RotateController;
            case "SceneController":
                return SceneController;
            case "SelectDesController":
                return SelectDesController;
            case "SMPLController":
                return SMPLController;
            case "SpeechManager":
                return SpeechManager;
            case "SwipeManager":
                return SwipeManager;
            case "TickController":
                return TickController;
            case "TrackingImageManager":
                return TrackingImageManager;
            case "UIStateManager":
                return UIStateManager;
            case "VideoPlayController":
                return VideoPlayController;
            case "VoiceController":
                return VoiceController;
            default:
                Debug.LogWarning($"未找到名为 {controllerName} 的 Controller");
                return null;
        }
    }

    /// <summary>
    /// 重置所有 Controller 引用（场景切换时调用）
    /// </summary>
    public static void ResetAll()
    {
        _camTrajController = null;
        _click3DObjectManager = null;
        _editModeManager = null;
        _externalModelController = null;
        _materialController = null;
        _meshController = null;
        _moveController = null;
        _relocateController = null;
        _rotateController = null;
        _sceneController = null;
        _selectDesController = null;
        _sMPLController = null;
        _speechManager = null;
        _swipeManager = null;
        _tickController = null;
        _trackingImageManager = null;
        _uIStateManager = null;
        _videoPlayController = null;
        _voiceController = null;
    }
}
