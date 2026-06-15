using System;
using System.Collections.Generic;

namespace Network.RequestParam
{
    /// <summary>
    /// MimoTTS 非流式语音合成请求。
    /// </summary>
    public static class MimoTTS
    {
        public class RequestParam : BaseRequestParam
        {
            public RequestParam(LLMModelConfig.MimoTTSConfig config, string text, string voiceName)
            {
                url = config.BaseUrl;
                method = "POST";
                networkConstant = NetworkConstant.MIMO_TTS;
                timeout = 60;
                headers["api-key"] = config.APIKey;
                requestData = new RequestBody
                {
                    model = config.ModelName,
                    messages = new List<MessageBody>
                    {
                        new MessageBody
                        {
                            role = "user",
                            content = config.Prompt ?? string.Empty
                        },
                        new MessageBody
                        {
                            role = "assistant",
                            content = text
                        }
                    },
                    audio = new AudioBody
                    {
                        voice = string.IsNullOrEmpty(voiceName) ? "mimo_default" : voiceName,
                        format = string.IsNullOrEmpty(config.AudioFormat) ? "wav" : config.AudioFormat
                    },
                    stream = false
                };
            }

            [Serializable]
            private class RequestBody
            {
                public string model;
                public List<MessageBody> messages;
                public AudioBody audio;
                public bool stream;
            }

            [Serializable]
            private class MessageBody
            {
                public string role;
                public string content;
            }

            [Serializable]
            private class AudioBody
            {
                public string voice;
                public string format;
            }
        }
    }
}
