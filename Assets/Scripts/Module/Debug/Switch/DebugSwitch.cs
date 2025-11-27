using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 调试用的开关，在DebugSwitch节点Editor界面上修改
/// </summary>
public class DebugSwitch : Singleton<DebugSwitch>
{
    #region 开关
    [SerializeField, Header("Debug用虚假重定位")]
    public bool DEBUG_FAKE_RELOCATE = false; 
    [SerializeField, Header("使用网络下载json")]
    public bool DEBUG_USING_NETWORK_JSON = false;
    #endregion
}
