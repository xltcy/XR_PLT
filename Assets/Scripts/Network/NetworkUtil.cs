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
    
    //重定向请求接口示例，现已被GetRelocateUrlSuffix方法取代
    private const string RELOCATE_REQUEST_URL_SUFFIX_INTERFACE = "media_app/request_NVLAD_redir/?source_location=";
    private const string GET_SCENE_LIST_INTERFACE = "media_app/get_scence_list/";
    private const string GET_SCENE_CONFIG_INTERFACE = "media_app/get_config/";
    private const string UPLOAD_SCENE_CONFIG_INTERFACE = "/media_app/update_config/";
    
    private static NetworkUtil _instance;
    public static NetworkUtil Instance => _instance ??= new NetworkUtil();

    #region 重定位
    private string GetRelocateUrlSuffix(SummaryItemData summaryItemData)
    {
        // 如果没有指定算法，则使用默认的 request_NVLAD_redir
        string relocate_algo = (summaryItemData == null || string.IsNullOrEmpty(summaryItemData.sceneRelocateAlgo)) ? "request_NVLAD_redir" : summaryItemData.sceneRelocateAlgo;
        return "media_app/" + relocate_algo + "/?source_location=";
    }

    public IEnumerator RelocateByCaptureRequest(SummaryItemData summaryItemData, string sceneLoc, byte[] imageData, Action<float[,]> onSuccess, Action<string> onFail)
    {
        string relocateRequestSuffixInterface = GetRelocateUrlSuffix(summaryItemData);
        string url = SERVER_URL + relocateRequestSuffixInterface + sceneLoc;

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
            //string innerPattern = @"-?\d+\.\d+"; // 匹配一个浮点数
            string innerPattern = @"[-+]?\d*\.?\d+([eE][-+]?\d+)?";

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
    #endregion 重定位
    
    #region 获取场景列表
    public IEnumerator GetSceneSummaryRequest(Action<SummaryData> onSuccess, Action<string> onFail)
    {
        //如果使用测试数据
        if (!DebugSwitch.Instance.DEBUG_USING_NETWORK_JSON)
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
        if (!DebugSwitch.Instance.DEBUG_USING_NETWORK_JSON)
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

    public IEnumerator UploadSummaryData()
    {
        if (!DebugSwitch.Instance.DEBUG_USING_NETWORK_JSON)
        {
            yield break;
        }
        //get summary data
        var url = SERVER_URL + UPLOAD_SCENE_CONFIG_INTERFACE;
        
        var sceneItemData = ControllerRegister.Instance.GetController<MeshController>().GetCurrentSummaryItemData();
        string localPath = Path.Combine(Application.persistentDataPath, $"{sceneItemData.sceneName}_{sceneItemData.sceneKey}.json");
        // 将数据对象转换为JSON字符串
        string json = "";
        if (File.Exists(localPath))
        {
            json = File.ReadAllText(localPath);
        }
        
        
        json = Resources.Load<TextAsset>("Configs/" + "test-HKG").text;
        
        
        // 使用WWWForm构建multipart/form-data
        WWWForm form = new WWWForm();
        // 添加config字段，值是JSON字符串
        form.AddField("config", json);
        
        
        // 创建UnityWebRequest，设置URL和方法
        using (UnityWebRequest request = UnityWebRequest.Post(url + "?key=5", form))
        {
            
            // 发送请求并等待响应
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("上传成功: " + request.downloadHandler.text);
            }
            else
            {
                Debug.LogError("上传失败: " + request.error);
            }
        }
    }
    #endregion 获取某一场景数据
}
