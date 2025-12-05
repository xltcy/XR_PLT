using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// API 数据模型
/// </summary>
[System.Serializable]
public class LoginRequest
{
    public string username;
    public string password;
    public string deviceId;
}

[System.Serializable]
public class LoginResponse
{
    public int code;
    public string message;
    public UserData data;
    public string token;
}

[System.Serializable]
public class UserData
{
    public string userId;
    public string username;
    public int level;
    public int exp;
    public string lastLoginTime;
}

[System.Serializable]
public class GameDataResponse
{
    public int score;
    public int coins;
    public List<ItemData> items;
}

[System.Serializable]
public class ItemData
{
    public string id;
    public string name;
    public int count;
}

/// <summary>
/// 网络服务使用示例
/// </summary>
public class NetworkServiceExample : MonoBehaviour
{
    private void Start()
    {
        // // 1. 基本GET请求
        // TestGetRequest();
        //
        // // 2. POST请求带回调
        // TestPostRequest();
        //
        // // 3. 带事件监听的请求
        // TestRequestWithEvents();
        //
        // // 4. 批量请求
        // TestBatchRequests();
        
        TestSummaryJson();
    }

    private void OnEnable()
    {
        NetworkServiceSystem.Instance.AddResponseListener(NetworkConstant.SUMMARY_JSON, TestSummaryJsonCallback);
    }

    private void OnDisable()
    {
        NetworkServiceSystem.Instance.RemoveResponseListener(NetworkConstant.SUMMARY_JSON, TestSummaryJsonCallback);
    }

    private void TestGetRequest()
    {
        Debug.Log("=== 测试GET请求 ===");
        
        NetworkServiceSystem.Instance.Get("api/user/profile", null, callback: (result, response) =>
        {
            if (response.success)
            {
                var userData = response.GetData<UserData>();
                if (userData != null)
                {
                    Debug.Log($"获取用户信息成功: {userData.username}, 等级: {userData.level}");
                }
            }
            else
            {
                Debug.LogError($"获取用户信息失败: {response.error}");
            }
        });
    }

    private void TestPostRequest()
    {
        Debug.Log("=== 测试POST请求 ===");
        
        var loginData = new LoginRequest
        {
            username = "player1",
            password = "123456",
            deviceId = SystemInfo.deviceUniqueIdentifier
        };

        string requestId = NetworkServiceSystem.Instance.Post("api/user/login", loginData, null, (result, response) =>
        {
            if (response.success)
            {
                var loginResponse = response.GetData<LoginResponse>();
                if (loginResponse != null && loginResponse.code == 200)
                {
                    Debug.Log($"登录成功: {loginResponse.data.username}");
                    
                    // 保存token
                }
            }
            else
            {
                Debug.LogError($"登录失败: {response.error}");
            }
        });

        Debug.Log($"登录请求ID: {requestId}");
    }

    private void TestRequestWithEvents()
    {
        Debug.Log("=== 测试带事件监听的请求 ===");
        
        string requestId = NetworkServiceSystem.Instance.Get("api/game/data", null, callback: (result, response) =>
        {
            // 主回调
            if (response.success)
            {
                var gameData = response.GetData<GameDataResponse>();
                Debug.Log($"游戏数据: 分数{gameData.score}, 金币{gameData.coins}");
            }
        });

        // 添加事件监听
        NetworkServiceSystem.Instance.AddResponseListener(requestId, (result, response) =>
        {
            if (response.success)
            {
                Debug.Log("请求成功事件触发");
            }
            else
            {
                Debug.Log("请求失败事件触发");
            }
        });
    }

    private void TestBatchRequests()
    {
        Debug.Log("=== 测试批量请求 ===");
        
        // 监听活跃请求数量
        NetworkServiceSystem.Instance.OnActiveRequestsChanged += count =>
        {
            Debug.Log($"活跃请求数: {count}");
        };

        // 同时发送多个请求
        for (int i = 0; i < 3; i++)
        {
            NetworkServiceSystem.Instance.Get($"api/game/item/{i}", null, callback: (result, response) =>
            {
                Debug.Log($"物品{i}请求完成: {response.success}");
            });
        }
    }

    private void TestSummaryJson()
    {
        var getSummaryJsonRequestParams = new GetSummaryJsonRequestParams();
        NetworkServiceSystem.Instance.SendRequest(getSummaryJsonRequestParams);
    }

    private void TestSummaryJsonCallback(bool result, NetworkResponse response)
    {
        if (result)
        {
            string jsonText = response.rawResponse;
                
            // 解析 JSON
            SummaryData data = JsonUtility.FromJson<SummaryData>(jsonText);
                
            // 保存到本地文件
            string localPath = Path.Combine(Application.persistentDataPath, "downloaded-summary.json");
//            File.WriteAllText(localPath, jsonText);
            Debug.Log("文件保存到: " + localPath);
        }
    }

    private void OnDestroy()
    {
        // 清理所有网络请求
        NetworkServiceSystem.Instance.ClearAllRequests();
    }
}