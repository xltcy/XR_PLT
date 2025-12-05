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
     public const string SERVER_URL = "http://60.205.232.241:7171/";
    
    //重定向请求接口示例，现已被GetRelocateUrlSuffix方法取代
    public const string RELOCATE_REQUEST_URL_SUFFIX_INTERFACE = "media_app/request_NVLAD_redir/?source_location=";
    public const string GET_SCENE_LIST_INTERFACE = "media_app/get_scence_list/";
    public const string GET_SCENE_CONFIG_INTERFACE = "media_app/get_config/";
    public const string UPLOAD_SCENE_CONFIG_INTERFACE = "/media_app/update_config/";
    
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

    #region 场景列表
    /// <summary>
    /// 获取本地测试场景列表
    /// </summary>
    /// <param name="onSuccess"></param>
    /// <param name="onFail"></param>
    public void GetSceneSummaryTestRequest(Action<SummaryData> onSuccess, Action<string> onFail)
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
    #endregion 场景列表
    
    #region 某一场景数据
    /// <summary>
    /// 获取本地测试场景数据
    /// </summary>
    /// <param name="sceneItemData"></param>
    /// <param name="onSuccess"></param>
    /// <param name="onFail"></param>
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
    }

    /// <summary>
    /// 上传场景数据
    /// </summary>
    /// <returns></returns>
    public void UploadSummaryData()
    {
        if (!DebugSwitch.Instance.DEBUG_USING_NETWORK_JSON)
        {
            return;
        }
        
        var curSceneItemData = ControllerRefer.MeshController.GetCurrentSummaryItemData();
        if (curSceneItemData == null)
        {
            Debug.LogWarning("当前场景数据为空，无法上传");
            return;
        }

        var jsonText = Resources.Load<TextAsset>("Configs/" + localSceneDataJsonDict[curSceneItemData.sceneKey]).text;
        var reqParams = new UploadSceneDataRequestParams(curSceneItemData.sceneKey, jsonText);
        reqParams.Send(null, (result, response) =>
        {
            if (result)
            {
                Debug.Log("上传成功: " + response.rawResponse);
            }
            else
            {
                Debug.LogError("上传失败: " + response.error);
            }
        });
    }
    
    private Dictionary<string, string> localSceneDataJsonDict = new Dictionary<string, string>()
    {
        {"4", "test-HKG"},
        {"3", "test-SJS"},
        {"6", "test-GXL"},
    };

    #endregion 某一场景数据
}
