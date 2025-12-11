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


}
