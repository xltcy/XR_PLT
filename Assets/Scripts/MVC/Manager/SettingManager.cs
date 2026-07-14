using UnityEngine;

/// <summary>
/// 应用基础设置管理器。
/// </summary>
public class SettingManager : BaseManager
{
    public override ManagerRegister.InitTiming InitTiming => ManagerRegister.InitTiming.OnSceneLoaded;

    public bool KeepScreenAwake { get; private set; } = true;

    public override void OnRegister()
    {
        base.OnRegister();

        SetKeepScreenAwake(KeepScreenAwake);
    }

    public override void OnUnregister()
    {
        SetKeepScreenAwake(false);

        base.OnUnregister();
    }

    public void SetKeepScreenAwake(bool keepAwake)
    {
        KeepScreenAwake = keepAwake;

        if (Application.platform == RuntimePlatform.Android)
        {
            Screen.sleepTimeout = keepAwake ? SleepTimeout.NeverSleep : SleepTimeout.SystemSetting;
        }
    }
}
