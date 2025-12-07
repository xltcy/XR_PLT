using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// 网络请求配置
/// </summary>
public class NetworkConfig
{
    public string baseUrl = "http://60.205.232.241:7171/";
    public int timeout = 30; // 秒
    public int maxRetries = 0;
    public float retryDelay = 2f; // 重试延迟（秒）
    public bool enableLogging = true;
    
    private static NetworkConfig _instance;
    public static NetworkConfig Instance => _instance ??= new NetworkConfig();

}