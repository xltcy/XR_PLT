using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UniJSON;
using UnityEngine;
using UnityEngine.Networking;

/**
 * Use to send http request.
 */
public class NetworkUtil
 { 
    private const string SERVER_URL = "http://60.205.232.241:7171/";
    
    private const string RELOCATE_REQUEST_URL_SUFFIX_INTERFACE = "media_app/request_NVLAD_redir/?source_location=";
    private const string GET_SCENE_LIST_INTERFACE = "media_app/get_scence_list/";
    private const string GET_SCENE_CONFIG_INTERFACE = "media_app/get_config/";
    
    public static bool DEBUG_USING_NETWORK_JSON = false;

    private static NetworkUtil _instance;
    public static NetworkUtil Instance => _instance ??= new NetworkUtil();

    public IEnumerator RelocateByCaptureRequest(string sceneLoc, byte[] imageData, Action<float[,]> onSuccess, Action<string> onFail)
    {
        string url = SERVER_URL + RELOCATE_REQUEST_URL_SUFFIX_INTERFACE + sceneLoc;

        string timestamp = "---------------------" + System.DateTime.Now.Ticks.ToString("x");
        byte[] boundaryByte = System.Text.Encoding.UTF8.GetBytes(timestamp);

        List<IMultipartFormSection> multipartSection = new List<IMultipartFormSection>();
        multipartSection.Add(new MultipartFormFileSection("images", imageData, "image.jpg", "image/jpg"));

        UnityWebRequest req = UnityWebRequest.Post(url, multipartSection, boundaryByte);

        req.SetRequestHeader("Content-Type", "multipart/form-data; boundary=" + timestamp);

        // send HTTP request
        yield return req.SendWebRequest();

        // 处理请求结果
        if (req.result == UnityWebRequest.Result.Success)
        {
            string response = req.downloadHandler.text;
            Debug.Log("Request succeeded. Response: " + response);
            float[,] num = new float[3, 4];

            // 假设 receivedJson 是接收到的 JSON 字符串
            int startIndex = response.IndexOf("[[");
            int endIndex = response.IndexOf("]]");

            string truncatedJson = response.Substring(startIndex + 1, endIndex - startIndex + 1);

            Debug.Log("Truncated JSON: " + truncatedJson);

            string outerPattern = @"\[.*?\]"; // 匹配最外层的方括号内的内容
            string innerPattern = @"-?\d+\.\d+"; // 匹配一个浮点数

            MatchCollection outerMatches = Regex.Matches(truncatedJson, outerPattern);

            int rowIndex = 0;

            foreach (Match outerMatch in outerMatches)
            {
                string subJson = outerMatch.Value;

                MatchCollection innerMatches = Regex.Matches(subJson, innerPattern);

                int columnIndex = 0;

                foreach (Match innerMatch in innerMatches)
                {
                    string numberString = innerMatch.Value;

                    // 解析浮点数并设置到矩阵
                    float number = float.Parse(numberString);
                    num[rowIndex, columnIndex] = number;

                    columnIndex++;
                }
                rowIndex++;
            }

            onSuccess.Invoke(num);
        }
        else
        {
            Debug.LogError("Request failed. Error: " + req.error);
            Debug.Log(req.downloadHandler.text);
            onFail?.Invoke(req.error);
        }
    }

    #region 获取场景列表
    public IEnumerator GetSceneSummaryRequest(Action<SummaryData> onSuccess, Action<string> onFail)
    {
        //如果使用测试数据
        if (!DEBUG_USING_NETWORK_JSON)
        {
            yield return new WaitForSeconds(1);
            GetSceneSummaryTestRequest(onSuccess, onFail);
        }
        else
        {
            string url = SERVER_URL + GET_SCENE_LIST_INTERFACE;
        
            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                yield return request.SendWebRequest();
            
                if (request.result == UnityWebRequest.Result.Success)
                {
                    string jsonText = request.downloadHandler.text;
                    Debug.Log("下载成功: " + jsonText);
                
                    // 解析 JSON
                    SummaryData data = JsonUtility.FromJson<SummaryData>(jsonText);
                
                    // 保存到本地文件
                    string localPath = Path.Combine(Application.persistentDataPath, "downloaded-summary.json");
                    File.WriteAllText(localPath, jsonText);
                    Debug.Log("文件保存到: " + localPath);
                    onSuccess.Invoke(data);
                }
                else
                {
                    string errorMsg = $"请求失败 - 状态: {request.result}, 错误: {request.error}, 状态码: {request.responseCode}";
                    Debug.Log(errorMsg);
                    onFail.Invoke(errorMsg);
                }
            }   
        }
    }
    
    private void GetSceneSummaryTestRequest(Action<SummaryData> onSuccess, Action<string> onFail)
    {
        // 创建模拟数据
        string localJsonPath = "test-summary.json";

        // Temp logic start
        // dont end with .json.
        var jsonString = Resources.Load<TextAsset>("Configs/" + "test-summary").text;
        if (jsonString != null)
        {
            SummaryData data = JsonConvert.DeserializeObject<SummaryData>(jsonString);
            onSuccess?.Invoke(data);
            return;
        }
        // temp logic end

        if (Application.platform == RuntimePlatform.Android)
        {
            localJsonPath = Application.persistentDataPath + localJsonPath;
        } else
        {
            localJsonPath = SceneController.TEST_JSON_PC_HOME_PATH + localJsonPath;
        }
        if (!File.Exists(localJsonPath))
        {
            string error = "找不到 scene.json！Path:" + localJsonPath;
            Debug.LogError(error);
            onFail.Invoke(error);
        }
        else
        {
            string json = File.ReadAllText(localJsonPath);
            SummaryData data = JsonConvert.DeserializeObject<SummaryData>(json);
            Debug.Log("Get Response json: Data:" + data);
            onSuccess.Invoke(data);
        }
    }
    #endregion 获取场景列表
    
    #region 获取某一场景数据
    public IEnumerator GetSceneDataRequest(SummaryItemData sceneItemData, Action<SceneData> onSuccess, Action<string> onFail)
    {
        var sceneKey = sceneItemData.sceneKey;
        //如果使用测试数据
        if (!DEBUG_USING_NETWORK_JSON)
        {
            yield return new WaitForSeconds(1); 
            GetSceneDataTestRequest(sceneItemData, onSuccess, onFail);
        }
        else
        {
            string url = SERVER_URL + GET_SCENE_CONFIG_INTERFACE;
        
            url = $"{url}?sceneKey={UnityWebRequest.EscapeURL(sceneKey)}";
        
            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                yield return request.SendWebRequest();

                if (request.result ==UnityWebRequest.Result.Success)
                {
                    string jsonText = request.downloadHandler.text;
                    Debug.Log("下载成功: " + jsonText);

                    // 解析 JSON
                    SceneData data = JsonUtility.FromJson<SceneData>(jsonText);

                    // 保存到本地文件
                    string localPath = Path.Combine(Application.persistentDataPath, $"{sceneItemData.sceneName}_{sceneItemData.sceneKey}.json");
                    File.WriteAllText(localPath, jsonText);
                    Debug.Log("文件保存到: " + localPath);
                    onSuccess?.Invoke(data);
                }
                else
                {
                    string errorMsg = $"请求失败 - 状态: {request.result}, 错误: {request.error}, 状态码: {request.responseCode}";
                    Debug.Log(errorMsg);
                    onFail?.Invoke(errorMsg);
                }
            }   
        }
    }

    public void GetSceneDataTestRequest(SummaryItemData sceneItemData, Action<SceneData> onSuccess, Action<string> onFail)
    {
        string localJsonPath = sceneItemData.sceneKey;

        // Temp logic start
        var jsonString = Resources.Load<TextAsset>("Configs/" + localJsonPath).text;
        if (jsonString != null)
        {
            var settings = new JsonSerializerSettings();
            settings.Converters.Add(new StringEnumConverter());
            SceneData data = JsonConvert.DeserializeObject<SceneData>(jsonString, settings);
            onSuccess?.Invoke(data);
        } else
        {
            onFail?.Invoke("jsonFail");
        }
        return;
        // temp logic end

        if (Application.platform == RuntimePlatform.Android)
        {
            localJsonPath = Application.persistentDataPath + localJsonPath;
        }
        else
        {
            localJsonPath = SceneController.TEST_JSON_PC_HOME_PATH + localJsonPath;
        }

        if (!File.Exists(localJsonPath))
        {
            string error = "找不到 scene.json！Path:" + localJsonPath;
            Debug.LogError(error);
            onFail.Invoke(error);
        } else
        {
            string json = File.ReadAllText(localJsonPath);
            SceneData data = JsonConvert.DeserializeObject<SceneData>(json);
            Debug.Log("Get Response json: Data:" + data);
            onSuccess.Invoke(data);
        }
    }
    #endregion 获取某一场景数据
}
