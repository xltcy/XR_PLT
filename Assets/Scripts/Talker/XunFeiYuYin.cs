using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using UnityEngine.SceneManagement;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.Networking;
using UnityEngine.UI;
using ZenFulcrum.EmbeddedBrowser;

public class XunFeiYuYin : MonoBehaviour
{
    string APPID = "5c81de59";
    string APISecret = "ea4d5e9b06f8cfb0deae4d5360e7f8a7";
    string APIKey = "94348d7a6d5f3807176cb1f4923efa5c";
    public static XunFeiYuYin yuyin;
    public event Action<string> 语音识别完成事件;   //语音识别回调事件
    public AudioClip RecordedClip;
    ClientWebSocket 语音识别WebSocket;
    private void Awake()
    {
        if (yuyin == null)
        {
            yuyin = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        DontDestroyOnLoad(gameObject);
    }
    public static XunFeiYuYin Init(string appkey, string APISecret, string APIKey, string 语音评测APIKey)
    {
        string name = "讯飞语音";
        if (yuyin == null)
        {
            GameObject g = new GameObject(name);
            g.AddComponent<XunFeiYuYin>();
        }
        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            Permission.RequestUserPermission(Permission.Microphone);
        }
        yuyin.APPID = appkey;
        yuyin.APISecret = APISecret;
        yuyin.APIKey = APIKey;
        //if (!yuyin.讯飞语音加)Debug.LogWarning("未安装或正确设置讯飞语音+将使用在线收费版讯飞引擎");

        //yuyin.javaClass.CallStatic("设置语音识别参数", new object[] { "language", "zh_cn" });//设置语音识别为中文
        return yuyin;
    }

    string GetUrl(string uriStr)
    {
        Uri uri = new Uri(uriStr);
        string date = DateTime.Now.ToString("r");
        string signature_origin = string.Format("host: " + uri.Host + "\ndate: " + date + "\nGET " + uri.AbsolutePath + " HTTP/1.1");
        HMACSHA256 mac = new HMACSHA256(Encoding.UTF8.GetBytes(APISecret));
        string signature = Convert.ToBase64String(mac.ComputeHash(Encoding.UTF8.GetBytes(signature_origin)));
        string authorization_origin = string.Format("api_key=\"{0}\",algorithm=\"hmac-sha256\",headers=\"host date request-line\",signature=\"{1}\"", APIKey, signature);
        string authorization = Convert.ToBase64String(Encoding.UTF8.GetBytes(authorization_origin));
        string url = string.Format("{0}?authorization={1}&date={2}&host={3}", uri, authorization, date, uri.Host);
        return url;
    }
    #region 语音识别

    public void 开始语音识别()
    {
        if (语音识别WebSocket != null && 语音识别WebSocket.State == WebSocketState.Open)
        {
            Debug.LogWarning("开始语音识别失败！，等待上次识别连接结束");
            return;
        }
        连接语音识别WebSocket();
        RecordedClip = Microphone.Start(null, false, 60, 16000);
    }

    public IEnumerator 停止语音识别()
    {
        Microphone.End(null);
        yield return new WaitUntil(() => 语音识别WebSocket.State != WebSocketState.Open);
        Debug.Log("识别结束，停止录音");
    }



    async void 连接语音识别WebSocket()
    {
        using (语音识别WebSocket = new ClientWebSocket())
        {
            CancellationToken ct = new CancellationToken();
            Uri url = new Uri(GetUrl("wss://iat-api.xfyun.cn/v2/iat"));
            try
            {
                await 语音识别WebSocket.ConnectAsync(url, ct);
            } catch (Exception e)
            {
                // connect fail.
                Debug.Log("Socket Connect Fail:" + e.ToString());
                语音识别完成事件?.Invoke("");
                return;
            }
            
            Debug.Log("连接成功,SocketState:" + 语音识别WebSocket.State.ToString());
            StartCoroutine(发送录音数据流(语音识别WebSocket));
            StringBuilder stringBuilder = new StringBuilder();
            while (语音识别WebSocket.State == WebSocketState.Open)
            {
                var result = new byte[4096];
                await 语音识别WebSocket.ReceiveAsync(new ArraySegment<byte>(result), ct);//接受数据
                List<byte> list = new List<byte>(result);
                while (list.Count > 0 && list[list.Count - 1] == 0x00) {
                    list.RemoveAt(list.Count - 1);//去除空字节
                }

                string str = Encoding.UTF8.GetString(list.ToArray());
                Debug.Log("接收消息：" + str);
                stringBuilder.Append(获取识别单词(str));
                JSONNode js = new JSONNode();
                if (JSONParser.TryDeserializeObject(str, out js))
                {
                    JSONNode data = js["data"];
                    if (data["status"] == 2)
                    {
                        语音识别WebSocket.Abort();
                    }
                }
            }
            Debug.LogWarning("断开连接");
            string s = stringBuilder.ToString();

            if (!string.IsNullOrEmpty(s))
            {
                Debug.LogWarning("识别到声音：" + s);
                TextBubble.SetGlobalText("您的提问是:\n" + s);
                语音识别完成事件?.Invoke(s);
            }
            else {
                语音识别完成事件?.Invoke("");
            }
        }
    }

    string 获取识别单词(string str)
    {
        StringBuilder stringBuilder = new StringBuilder();
        if (!string.IsNullOrEmpty(str))
        {
            JSONNode recivejson = JSONNode.Parse(str);
            JSONNode ws = recivejson["data"]["result"]["ws"];
            foreach (JSONNode item in ws)
            {
                JSONNode cw = item["cw"];
                foreach (JSONNode item1 in cw)
                {
                    stringBuilder.Append((string)item1["w"]);
                }
            }
        }
        return stringBuilder.ToString();
    }

    public static byte[] 获取音频流片段(int star, int length, AudioClip recordedClip)
    {
        float[] soundata = new float[length];
        recordedClip.GetData(soundata, star);
        int rescaleFactor = 32767;
        byte[] outData = new byte[soundata.Length * 2];
        for (int i = 0; i < soundata.Length; i++)
        {
            short temshort = (short)(soundata[i] * rescaleFactor);
            byte[] temdata = BitConverter.GetBytes(temshort);
            outData[i * 2] = temdata[0];
            outData[i * 2 + 1] = temdata[1];
        }
        return outData;
    }

    void 发送数据(byte[] audio, int status, ClientWebSocket socket)
    {
        if (socket.State != WebSocketState.Open)
        {
            return;
        }
        JSONNode jn = new JSONNode
        {
            {
                "common",new JSONNode{{ "app_id",APPID}}},
            {
                "business",new JSONNode{
                    { "language","zh_cn"},//识别语音
                    {  "domain","iat"},
                    {  "accent","mandarin"},
                    { "vad_eos",2000}
                }
            },
            {
                "data",new JSONNode{
                    { "status",0 },
                    { "encoding","raw" },
                    { "format","audio/L16;rate=16000"}
                 }
            }
        };
        JSONNode data = jn["data"];
        if (status < 2)
        {
            data["audio"] = Convert.ToBase64String(audio);
        }
        data["status"] = status;
        Debug.Log("发送消息:" + jn);
        socket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(jn)), WebSocketMessageType.Binary, true, new CancellationToken()); //发送数据
    }

    IEnumerator 发送录音数据流(ClientWebSocket socket)
    {
        yield return new WaitWhile(() => Microphone.GetPosition(null) <= 0 && Microphone.IsRecording(null));
        float t = 0;
        int position = Microphone.GetPosition(null);
        const float waitTime = 0.04f;//每隔40ms发送音频
        int status = 0;
        int lastPosition = 0;
        const int Maxlength = 640;//最大发送长度
        while (position < RecordedClip.samples && socket.State == WebSocketState.Open)
        {
            t += waitTime;
            yield return new WaitForSecondsRealtime(waitTime);
            if (Microphone.IsRecording(null))
                position = Microphone.GetPosition(null);
            Debug.Log("录音时长：" + t + "position=" + position + ",lastPosition=" + lastPosition);
            if (position <= lastPosition)
            {
                Debug.LogWarning("字节流发送完毕！强制结束！");
                break;
            }
            int length = position - lastPosition > Maxlength ? Maxlength : position - lastPosition;
            byte[] date = 获取音频流片段(lastPosition, length, RecordedClip);
            发送数据(date, status, socket);
            lastPosition = lastPosition + length;
            status = 1;
        }
        发送数据(null, 2, socket);
        //WebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "关闭WebSocket连接",new CancellationToken());
        Microphone.End(null);
    }
    #endregion

}
