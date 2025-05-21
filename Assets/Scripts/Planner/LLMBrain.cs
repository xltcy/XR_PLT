using System.Collections;
using System.Collections.Generic;
using System.Text;
//using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine.Rendering;
using static LLMBrain;
using System;

public class LLMBrain : MonoBehaviour
{
    // DeepSeek API Key and Url
    private string deepseekApiKey = "sk-b87ae2f2816444d0a25b27c5766d7c02";
    private string deepseekApiUrl = "https://api.deepseek.com/chat/completions";

    // DouBao API Key and Url
    private string doubaoApiKey = "334d2eaf-a0bb-429d-a844-500f7673f5e6";
    private string doubaoApiUrl = "https://ark.cn-beijing.volces.com/api/v3/chat/completions";

    // UI
    public InputField userInput;
    public Text LLMOutput;
    public SMPLController smplController;

    private List<Dictionary<string, string>> messages = new List<Dictionary<string, string>>();
    private List<Dictionary<string, string>> tools = new List<Dictionary<string, string>>();

    private Dictionary<string, Vector3> desposition = new Dictionary<string, Vector3>
    {
        {"door", new Vector3(2.8f, -1.2f, -2.6f) },
        {"BlackWidow", new Vector3(-1.1f, -1.2f, 2.5f) }
    };

    [System.Serializable]
    public class Tool
    {
        public string type;
        public Function function;
    }

    [System.Serializable]
    public class Function
    {
        public string name;
        public string description;
        public Parameters parameters;
    }

    [System.Serializable]
    public class Parameters
    {
        public string type;
        public Dictionary<string, Property> properties;
        public List<string> required;
    }

    [System.Serializable]
    public class Property
    {
        public string type;
        public string description;
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
        public TooCalls[] tool_calls;
    }

    [System.Serializable]
    public class TooCalls
    {
        public string id;
        public string type;
        public FunctionCall function;

    }

    [Serializable]
    public class FunctionCall
    {
        public string name;
        public string arguments;
    }

    private Tool getPositionTool = new Tool
    {
        type = "function",
        function = new Function
        {
            name = "GetPositon",
            description = "�û�ѯ�ʷ�������壬������������꣬ e.g. (1,2,1), ���ʵ���ʱ����Ҫ����ΪDoor���в�ѯ",
            parameters = new Parameters
            {
                type = "object",
                properties = new Dictionary<string, Property>
                    {
                        {"name", new Property
                            {
                                type = "string",
                                description = "��ʾ���������������"
                            }
                        }
                    },
                required = new List<string> { "name" }
            },
        }
    };

    private Tool setDestination = new Tool
    {
        type = "function",
        function = new Function
        {
            name = "TakeMeTo",
            description = "�û�����ָ���������˴���ιۣ������˽�Ŀ�ĵ�����Ϊ�û���ȥ�ĵط�",
            parameters = new Parameters
            {
                type = "object",
                properties = new Dictionary<string, Property>
                    {
                        {"name", new Property
                            {
                                type = "string",
                                description = "��ʾҪ�ι۶�������֣�����֧�ֵĵط���{Door��BlackWillow��BeiJing1�� C919}"
                            }
                        }
                    },
                required = new List<string> { "name" }
            },
        }
    };

    private List<LLMResponse> responses = new List<LLMResponse>();


    // Start is called before the first frame update
    void Start()
    {
        messages.Add(new Dictionary<string, string> { { "role", "system" }, { "content", "����һ�����Σ����Դ����Ҳι۷��䣬���Ҹ��㷢��ָ��ʱ����Ҫ�ȷֽ��������Ȼ������ִ�У��ֽ������������Ϊ���ҵ�Ŀ�����꣬�ٵ���Ѱ·�㷨��·,...�����е�����ֽ������������" } });
    }

    public void OnSendButtonClicked()
    {
        string userMessage = userInput.text;
        if (string.IsNullOrEmpty(userMessage)) return;

        messages.Add(new Dictionary<string, string> { { "role", "user" }, { "content", userMessage } });

        Debug.Log("Msg in messages: " + userMessage);
        LLMOutput.text += "\nuser: " + userMessage;

        //StartCoroutine(CallDeepSeekAPI());
        //StartCoroutine(CallDeepSeekAPIWithTools());
        //StartCoroutine(CallDoubaoAPI());
        //StartCoroutine(CallDoubaoAPIWithFC());
        //StartCoroutine(CallDouBaoAPIList());

        if(messages.Count == 2)
        {
            StartCoroutine(CallDeepSeekAPI());
        }else
        {
            StartCoroutine(CallDoubaoAPIWithFC());
        }
    }

    private IEnumerator CallDouBaoAPIList()
    {
        yield return StartCoroutine(CallDoubaoAPIWithFC());
        //Debug.Log("Msg in response1: " + responses[0].ToString());
        messages.Add(new Dictionary<string, string> { { "role", "assistant" }, { "content", responses[0].choices[0].message.content } });

        yield return StartCoroutine(CallDoubaoAPIWithFC());
        
    }

    private IEnumerator CallDoubaoAPI()
    {
        var requestData = new
        {
            model = "ep-20250111195654-kwt28",
            messages = messages,
            stream = false
        };

        string jsonData = JsonConvert.SerializeObject(requestData);

        // ���� UnityWebRequest
        UnityWebRequest request = new UnityWebRequest(doubaoApiUrl, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + doubaoApiKey);

        Debug.Log("Msg in sent message before: " + messages.ToString());

        // ��������
        yield return request.SendWebRequest();

        Debug.Log("Msg in sent message after: " + request.downloadHandler.text);

        if (request.result == UnityWebRequest.Result.Success)
        {
            // ������Ӧ
            var response = JsonConvert.DeserializeObject<LLMResponse>(request.downloadHandler.text);
            string botMessage = response.choices[0].message.content;

            // ��ʾ��Ӧ
            LLMOutput.text += "\nAI: " + botMessage;
            userInput.text = "";

            // ��� AI ��Ϣ���Ի���ʷ
            messages.Add(new Dictionary<string, string> { { "role", "assistant" }, { "content", botMessage } });
        }
        else
        {
            Debug.LogError("Msg in Error: " + request.error);
        }
    }

    private IEnumerator CallDoubaoAPIWithFC()
    {
        //var tool = JsonConvert.SerializeObject(getPositionTool);
        var tools = new List<Tool> { getPositionTool, setDestination };
        Debug.Log("Msg tool: " + tools);
        var requestData = new 
        {
            model = "ep-20250111195654-kwt28",
            messages = messages,
            stream = false,
            tools = tools,
            tools_choice = "auto"
        };

        string jsonData = JsonConvert.SerializeObject(requestData);

        // ���� UnityWebRequest
        UnityWebRequest request = new UnityWebRequest(doubaoApiUrl, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + doubaoApiKey);

        //Debug.Log("Msg in sent message before: " + messages.ToString());

        // ��������
        yield return request.SendWebRequest();

        Debug.Log("Msg in sent message after: " + request.downloadHandler.text);

        if (request.result == UnityWebRequest.Result.Success)
        {
            // ������Ӧ
            var response = JsonConvert.DeserializeObject<LLMResponse>(request.downloadHandler.text);
            //responses.Add(response);
            string botMessage = response.choices[0].message.content;

            // ��ʾ��Ӧ
            LLMOutput.text += "\nAI: " + botMessage;
            userInput.text = "";

            // ��� AI ��Ϣ���Ի���ʷ
            messages.Add(new Dictionary<string, string> { { "role", "assistant" }, { "content", botMessage } });

            //���ú���
            if (response.choices[0].message.tool_calls != null)
            {
                if (response.choices[0].message.tool_calls[0].function.name == "TakeMeTo")
                {
                    TakeMeTo("BlackWillow");
                }
                if(response.choices[0].message.tool_calls[0].function.name == "GetPositon")
                {
                    GetPosition("BlackWillow");
                }

            }
        }
        else
        {
            Debug.LogError("Msg in Error: " + request.error);
        }
    }

    private IEnumerator CallDeepSeekAPI()
    {
        // ������������
        var requestData = new
        {
            model = "deepseek-chat",
            messages = messages,
            stream = false
        };

        string jsonData = JsonConvert.SerializeObject(requestData);

        // ���� UnityWebRequest
        UnityWebRequest request = new UnityWebRequest(deepseekApiUrl, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + deepseekApiKey);

        //Debug.Log("Msg in sent message before: ");

        // ��������
        yield return request.SendWebRequest();

        Debug.Log("Msg in sent message after: " + request.downloadHandler.text);

        if (request.result == UnityWebRequest.Result.Success)
        {
            // ������Ӧ
            var response = JsonConvert.DeserializeObject<LLMResponse>(request.downloadHandler.text);
            string botMessage = response.choices[0].message.content;

            // ��ʾ��Ӧ
            LLMOutput.text += "\nAI: " + botMessage;
            userInput.text = "";

            // ��� AI ��Ϣ���Ի���ʷ
            messages.Add(new Dictionary<string, string> { { "role", "assistant" }, { "content", botMessage } });
        }
        else
        {
            Debug.LogError("Error: " + request.error);
        }
    }

    private IEnumerator CallDeepSeekAPIWithTools()
    {
        // ������������
        Tool getPositionTool = new Tool
        {
            type = "funtion",
            function = new Function
            {
                name = "getPositon",
                description = "Return a position in Vector3, e.g. (0.1f, 0.2f. 0.4f), the user should provide a string as name",
                parameters = new Parameters
                {
                    type = "object",
                    properties = new Dictionary<string, Property>
                    {
                        {"location", new Property
                            {
                                type = "string",
                                description = "The object user want to visit, e.g. BlackWidow"
                            }
                        }
                    },
                    required = new List<string> { "location" }
                },
            }
        };
        var tools = JsonConvert.SerializeObject(getPositionTool);
        var requestData = new
        {
            model = "deepseek-chat",
            messages = messages,
            stream = false,
            tools = tools
        };

        string jsonData = JsonConvert.SerializeObject(requestData);

        // ���� UnityWebRequest
        UnityWebRequest request = new UnityWebRequest(deepseekApiUrl, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + deepseekApiKey);

        //Debug.Log("Msg in sent message before: " + messages);

        // ��������
        yield return request.SendWebRequest();

        Debug.Log("Msg in sent message after");

        if (request.result == UnityWebRequest.Result.Success)
        {
            // ������Ӧ
            var response = JsonConvert.DeserializeObject<LLMResponse>(request.downloadHandler.text);
            string botMessage = response.choices[0].message.content;

            // ��ʾ��Ӧ
            LLMOutput.text += "\nAI: " + botMessage;
            userInput.text = "";

            // ��� AI ��Ϣ���Ի���ʷ
            messages.Add(new Dictionary<string, string> { { "role", "assistant" }, { "content", botMessage } });
        }
        else
        {
            Debug.LogError("Error: " + request.error);
        }
    }

    private Vector3 GetPosition(string name)
    {
        return desposition[name];
    }

    private void TakeMeTo(string name)
    {
        Vector3 desPosition = smplController.GetDesPosition(name);
        smplController.SetDestination(desPosition);
    }

    private void CallFunction(TooCalls tooCalls)
    {
        if(tooCalls.function.name == "TakeMeTo")
        {
            TakeMeTo("BlackWillow");
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
