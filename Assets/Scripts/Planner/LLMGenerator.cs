using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Text;
//using UnityEditor.PackageManager.Requests;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System;

public class LLMGenerator : MonoBehaviour
{
    // LLM Configs
    public static LLMModelConfig.BaseConfig llmModelConfig = null;
    private static string modelName;
    private static string baseUrl;
    private static string apiKey;
    
    private List<Dictionary<string, string>> messages = new List<Dictionary<string, string>>();

    private static LLMGenerator llmGenerator;
    private string systemPrompt;
    
    private SceneData cachedSceneData;

    // UI
    public InputField userInput;
    public Text LLMOutput;
    public SMPLController smplController;

    // Start is called before the first frame update
    void Start()
    {
        
    }
    
    public static LLMGenerator Init()
    {
        string name = nameof(LLMGenerator);
        if (llmGenerator == null)
        {
            GameObject g = new GameObject(name);
            llmGenerator = g.AddComponent<LLMGenerator>();
        }
        
        //load LLM Model Config
        llmModelConfig = LLMModelConfig.Local;
        if (llmModelConfig != null)
        {
            baseUrl = llmModelConfig.BaseUrl;
            apiKey = llmModelConfig.APIKey;
            modelName = llmModelConfig.ModelName;
        }
        else
        {
            Debug.LogError("LLM Model Config is null, please check LLMGenerator.Init!");
        }
        
        return llmGenerator;
    }

    /// <summary>
    /// 在选定场景之后才能初始化消息列表，需要修改为依赖响应调用
    /// </summary>
    public void InitMessagesList()
    {
        messages.Clear();
        messages.Add(new Dictionary<string, string> { { "role", "system" }, { "content", Prompt.GenerateSystemPrompt() } });
    }
    
    private IEnumerator getLLMResponse()
    {
        // 准备请求数据
        var requestData = new
        {
            model = modelName,  
            messages = messages,
            stream = false,
        };

        string jsonData = JsonConvert.SerializeObject(requestData);

        // 创建 UnityWebRequest
        UnityWebRequest request = new UnityWebRequest(baseUrl, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + apiKey);

        // 发送请求
        yield return request.SendWebRequest();

        Debug.Log("LLM in sent message after");

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("LLM: " + request);
            // 解析响应
            var response = JsonConvert.DeserializeObject<LLMResponse>(request.downloadHandler.text);
            string botMessage = response.choices[0].message.content;

            // 显示响应
            LLMOutput.text += "\nAI: " + botMessage;
            userInput.text = "";

            // 添加 AI 信息到对话历史
            messages.Add(new Dictionary<string, string> { { "role", "assistant" }, { "content", botMessage } });
        }
        else
        {
            Debug.LogError("Error: " + request.error);
        }
    }

    public void OnSendButtonClicked()
    {
        string userMessage = userInput.text;
        if (string.IsNullOrEmpty(userMessage)) return;

        messages.Add(new Dictionary<string, string> { { "role", "user" }, { "content", userMessage } });

        Debug.Log("LLM: " + messages.ToString());
        if(messages.Count == 2)
        {
            LLMOutput.text += "user: " + userMessage;
        }
        else
        {
            LLMOutput.text += "\n\nuser: " + userMessage;
        }

        //StartCoroutine(getLLMResponse());

        CallForLLM(
            userMessage,
            onSuccess: (response) =>
            {
                Debug.Log("LLM响应成功: " + response);
                // 处理成功响应
                SpeechManager.SayFromStr(response);
            },
            onError: (error) =>
            {
                Debug.LogError("LLM调用失败: " + error);
                // 处理错误
                // 例如：显示错误消息、重试等
            }
        );
    }

    // // 添加回调委托
    // public delegate void LLMCallback(string response);
    // public delegate void LLMErrorCallback(string error);

    // 修改后的 CallForLLM 方法
    public void CallForLLM(string prompt, Action<string> onSuccess = null, Action<string> onError = null)
    {
        messages.Add(new Dictionary<string, string> { { "role", "user" }, { "content", prompt } });

        // 修改 getLLMResponse 协程以支持回调
        StartCoroutine(getLLMResponseWithCallback(onSuccess, onError));
    }

    // 新的带回调的响应处理方法
    private IEnumerator getLLMResponseWithCallback(Action<string> onSuccess = null, Action<string> onError = null)
    {
        var requestData = new
        {
            model = modelName,
            messages = messages,
            stream = false,
        };

        string jsonData = JsonConvert.SerializeObject(requestData);

        UnityWebRequest request = new UnityWebRequest(baseUrl, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + apiKey);

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            try
            {
                var response = JsonConvert.DeserializeObject<LLMResponse>(request.downloadHandler.text);
                string botMessage = response.choices[0].message.content;

                // 更新UI
                if (LLMOutput != null)
                {
                    LLMOutput.text += "\nAI: " + botMessage;
                }

                // 添加到对话历史
                messages.Add(new Dictionary<string, string> { { "role", "assistant" }, { "content", botMessage } });

                // 调用成功回调
                onSuccess?.Invoke(botMessage);
            }
            catch (System.Exception e)
            {
                string errorMessage = $"Error parsing response: {e.Message}";
                Debug.LogError(errorMessage);
                onError?.Invoke(errorMessage);
            }
        }
        else
        {
            string errorMessage = $"API Error: {request.error}";
            Debug.LogError(errorMessage);
            onError?.Invoke(errorMessage);
        }
    }

    [System.Serializable]
    public class LLMResponse
    {
        public Choice[] choices;
    }

    [System.Serializable]
    public class Choice
    {
        public Message message;
    }

    [System.Serializable]
    public class Message
    {
        public string content;
        public string role;
    }
}
