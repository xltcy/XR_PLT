using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// 网络请求参数，只修改自己需要的部分即可
/// </summary>
public abstract class BaseRequestParam
{
    public string url;
    public string method = "GET";
    public string networkConstant;    // 用户定义，一般为NetworkConstant中的常量
    public Dictionary<string, string> queryParams = new Dictionary<string, string>(); // 会拼接在url后的参数
    
    //====================== 根据method选择填充的数据字段 ==========================
    public object requestData;
    // FormData 属性，支持多种类型
    public List<FormField> FormDataFields { get; set; }
    public string customBoundary; // 自定义boundary，用于multipart/form-data请求
    
    //====================== 一般不需要改 ==========================
    public Dictionary<string, string> headers = new Dictionary<string, string>();   //一般来讲不需要手动设置
    public int timeout = 30;
    public bool showLoading = true;
    public bool retryOnFailure = false;

    //====================== 本地使用 ==========================
    public object localData; // 仅本地使用的数据，会传递到response，不参与网络传输
    
    public virtual void Send(Transform lockable = null, NetworkServiceManager.ResponseEvent callback = null)
    {
        ManagerRefer.NetworkServiceManager.SendRequest(this, true, lockable, callback);
    }

    public virtual void SendWithoutLockable(NetworkServiceManager.ResponseEvent callback = null)
    {
        ManagerRefer.NetworkServiceManager.SendRequest(this, false, null, callback);
    }
}