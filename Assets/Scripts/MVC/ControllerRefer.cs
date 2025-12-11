// =====================================================
// 自动生成的 Controller 引用类
// 生成时间: 2025-12-11 15:41:46
// 包含 16 个继承 BaseController 的类
// =====================================================

using UnityEngine;

// 请确保以下命名空间存在，或添加相应的 using 语句
// using YourNamespace; // 根据实际情况修改

/// <summary>
/// Controller 快速引用器 - 仅继承 BaseController 的类
/// 自动生成，请勿手动修改
/// </summary>
public static class ControllerRefer
{
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

    private static LoadingViewController _loadingViewController;
    public static LoadingViewController LoadingViewController
    {
        get
        {
            return _loadingViewController ??= ControllerRegister.Instance?.GetController<LoadingViewController>();
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

    private static SonarWaveManager _sonarWaveManager;
    public static SonarWaveManager SonarWaveManager
    {
        get
        {
            return _sonarWaveManager ??= ControllerRegister.Instance?.GetController<SonarWaveManager>();
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

    private static TrackingImageManager _trackingImageManager;
    public static TrackingImageManager TrackingImageManager
    {
        get
        {
            return _trackingImageManager ??= ControllerRegister.Instance?.GetController<TrackingImageManager>();
        }
    }

    private static UIManager _uIManager;
    public static UIManager UIManager
    {
        get
        {
            return _uIManager ??= ControllerRegister.Instance?.GetController<UIManager>();
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
            case "Click3DObjectManager":
                return Click3DObjectManager;
            case "EditModeManager":
                return EditModeManager;
            case "ExternalModelController":
                return ExternalModelController;
            case "LoadingViewController":
                return LoadingViewController;
            case "MeshController":
                return MeshController;
            case "MoveController":
                return MoveController;
            case "RotateController":
                return RotateController;
            case "SceneController":
                return SceneController;
            case "SelectDesController":
                return SelectDesController;
            case "SMPLController":
                return SMPLController;
            case "SonarWaveManager":
                return SonarWaveManager;
            case "SpeechManager":
                return SpeechManager;
            case "SwipeManager":
                return SwipeManager;
            case "TrackingImageManager":
                return TrackingImageManager;
            case "UIManager":
                return UIManager;
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
        _click3DObjectManager = null;
        _editModeManager = null;
        _externalModelController = null;
        _loadingViewController = null;
        _meshController = null;
        _moveController = null;
        _rotateController = null;
        _sceneController = null;
        _selectDesController = null;
        _sMPLController = null;
        _sonarWaveManager = null;
        _speechManager = null;
        _swipeManager = null;
        _trackingImageManager = null;
        _uIManager = null;
        _voiceController = null;
    }
}
