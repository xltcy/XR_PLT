using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// 网络请求参数
/// </summary>
public class RequestParams
{
    public string url;
    public string method = "GET";
    public object requestData;
    public Dictionary<string, string> headers = new Dictionary<string, string>();
    public Dictionary<string, string> queryParams = new Dictionary<string, string>();
    public int timeout = 30;
    public bool showLoading = true;
    public bool retryOnFailure = true;
    public object localData; // 仅本地使用的数据，会传递到response，不参与网络传输

    // 用户定义，一般为NetworkConstant中的常量
    public string networkConstant;

    public void Send(Transform lockable = null, NetworkServiceSystem.ResponseEvent callback = null)
    {
        NetworkServiceSystem.Instance.SendRequest(this, lockable, callback);
    }
}

public class GetSummaryJsonRequestParams : RequestParams
{
    public GetSummaryJsonRequestParams()
    {
        url = NetworkServiceSystem.Instance.BuildUrl(NetworkUtil.GET_SCENE_LIST_INTERFACE);
        networkConstant = NetworkConstant.SUMMARY_JSON;
    }
}

public class GetSceneDataRequestParams : RequestParams
{
    public GetSceneDataRequestParams(SummaryItemData sceneItemData)
    {
        localData = sceneItemData;
        url = NetworkServiceSystem.Instance.BuildUrl($"{NetworkUtil.GET_SCENE_CONFIG_INTERFACE}?sceneKey={UnityWebRequest.EscapeURL(sceneItemData.sceneKey)}");
        networkConstant = NetworkConstant.SCENE_DATA;
    }
}

public class UploadSceneDataRequestParams : RequestParams
{
    public UploadSceneDataRequestParams(string sceneKey, string json)
    {
        url = NetworkServiceSystem.Instance.BuildUrl($"{NetworkUtil.UPLOAD_SCENE_CONFIG_INTERFACE}?key={UnityWebRequest.EscapeURL(sceneKey)}");
        method = "POST";
        requestData = new Dictionary<string, string>()
        {
            { "config", json }
        };
        networkConstant = NetworkConstant.UPLOAD_SCENE_DATA;
        headers.Add("Content-Type", "application/x-www-form-urlencoded");
    }
}

public class GetSonarPoseParams : RequestParams
{
    public GetSonarPoseParams(string name, byte[] imageData)
    {
        url = NetworkServiceSystem.Instance.BuildUrl($"/media_app/obj_pose_estimate/?obj_name={name}");
        method = "POST";
        requestData = new Dictionary<string, byte[]>()
        {
            { "image", imageData }
        };
        networkConstant = NetworkConstant.GET_SONAR_POSE;
        headers.Add("Content-Type", "application/x-www-form-urlencoded");
    }

}