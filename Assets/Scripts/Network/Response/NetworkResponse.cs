using System;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// 网络响应
/// </summary>
public class NetworkResponse
{
    public bool success;
    public string error;
    public int statusCode;
    public string rawResponse;
    public long responseTime; // 响应时间（毫秒）
    public object localData; // 仅本地使用的数据，会传递到response，不参与网络传输
    public UnityWebRequest request;

    public T GetData<T>() where T : class
    {
        if (!success || string.IsNullOrEmpty(rawResponse))
            return null;

        try
        {
            return JsonUtility.FromJson<T>(rawResponse);
        }
        catch (Exception e)
        {
            Debug.LogError($"JSON解析失败: {e.Message}");
            return null;
        }
    }
}