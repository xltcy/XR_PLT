using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace NetworkServiceExample
{
    /// <summary>
    /// 网络服务使用示例
    /// </summary>
    public class NetworkServiceExample : MonoBehaviour
    {
        private void Start()
        {
            TestSummaryJson();
        }

        private void OnEnable()
        {
            ManagerRefer.NetworkServiceManager.AddResponseListener(NetworkConstant.SUMMARY_JSON, TestSummaryJsonCallback);
        }

        private void OnDisable()
        {
            ManagerRefer.NetworkServiceManager.RemoveResponseListener(NetworkConstant.SUMMARY_JSON, TestSummaryJsonCallback);
        }

        private void TestSummaryJson()
        {
            var getSummaryJsonRequestParams = new Network.RequestParam.GetSummaryJson.RequestParam();
            ManagerRefer.NetworkServiceManager.SendRequest(getSummaryJsonRequestParams);
        }

        private void TestSummaryJsonCallback(bool result, NetworkResponse response)
        {
            if (result)
            {
                string jsonText = response.rawResponse;
                    
                // 解析 JSON
                SummaryData data = JsonUtility.FromJson<SummaryData>(jsonText);
                    
                Debug.Log("场景Summary数据：" + jsonText);
                
                // 保存到本地文件
                // string localPath = Path.Combine(Application.persistentDataPath, "downloaded-summary.json");
                // File.WriteAllText(localPath, jsonText);
                // Debug.Log("文件保存到: " + localPath);
            }
        }
    }
}
