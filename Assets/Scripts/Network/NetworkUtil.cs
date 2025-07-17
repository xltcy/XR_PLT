using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;

/**
 * Use to send http request.
 */
public class NetworkUtil
 {
    private const string SEVER_URL = "http://123.57.25.77:7005/media_app/";
    private const string RELOCATE_REQUEST_URL_SUFFIX = "request_NVLAD_redir/?source_location=";

    private static NetworkUtil _instance;
    public static NetworkUtil Instance => _instance ??= new NetworkUtil();

    public IEnumerator RelocateByCaptureRequest(string sceneLoc, byte[] imageData, Action<float[,]> onSuccess, Action<string> onFail)
    {
        string url = SEVER_URL + RELOCATE_REQUEST_URL_SUFFIX + sceneLoc;

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

    public IEnumerator GetSceneDataRequest(string sceneLoc, Action<SceneData> onSuccess, Action<string> onFail)
    {
        // todo
        yield return new WaitForSeconds(1);
        string localJsonPath = sceneLoc;
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

}
