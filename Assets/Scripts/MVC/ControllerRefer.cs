// =====================================================
// 自动生成的 Controller 引用类
// 生成时间: 2025-12-02 18:42:01
// 包含 18 个用户 Controller
// =====================================================

using UnityEngine;

/// <summary>
/// Controller 快速引用器 - 仅用户脚本
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

    private static Speech2BlendshapeController _speech2BlendshapeController;
    public static Speech2BlendshapeController Speech2BlendshapeController
    {
        get
        {
            return _speech2BlendshapeController ??= ControllerRegister.Instance?.GetController<Speech2BlendshapeController>();
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

    private static VideoManager _videoManager;
    public static VideoManager VideoManager
    {
        get
        {
            return _videoManager ??= ControllerRegister.Instance?.GetController<VideoManager>();
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
        _speech2BlendshapeController = null;
        _speechManager = null;
        _swipeManager = null;
        _trackingImageManager = null;
        _uIManager = null;
        _videoManager = null;
        _voiceController = null;
    }
}
