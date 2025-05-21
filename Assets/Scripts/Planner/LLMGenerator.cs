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
    private string baseUrl = "https://gemini.19980709.xyz:2053/v1/chat/completions";
    private string apiKey = "sk-Fy0ClFrgCfhnd5WLwSnT1CwjQmHnLZUU1ymiQkNc4OaS1ie8";
    private string modelName = "gemini-flash";
    private List<Dictionary<string, string>> messages = new List<Dictionary<string, string>>();

    private static LLMGenerator llmGenerator;
    private string systemPrompt = Prompt.generateSystemPrompt();

    // UI
    public InputField userInput;
    public Text LLMOutput;
    public SMPLController smplController;

    // Start is called before the first frame update
    void Start()
    {
        messages.Add(new Dictionary<string, string> { { "role", "system" }, { "content", systemPrompt } });
    }

    // Update is called once per frame
    void Update()
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
        return llmGenerator;
    }

    private IEnumerator getLLMResponse()
    {
        // ������������
        var requestData = new
        {
            model = modelName,
            messages = messages,
            stream = false,
        };

        string jsonData = JsonConvert.SerializeObject(requestData);

        // ���� UnityWebRequest
        UnityWebRequest request = new UnityWebRequest(baseUrl, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + apiKey);

        // ��������
        yield return request.SendWebRequest();

        Debug.Log("LLM in sent message after");

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("LLM: " + request);
            // ������Ӧ
            var response = JsonConvert.DeserializeObject<LLMResponse>(request.downloadHandler.text);
            string botMessage = response.choices[0].message.content;

            // ��ʾ��Ӧ
            LLMOutput.text += "\nAI: " + botMessage;
            userInput.text = "";

            // ���� AI ��Ϣ���Ի���ʷ
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
        string processedPrompt = Prompt.generateShengnaPompt(prompt);
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
