using System;

namespace Network.RequestParam
{
    /// <summary>
    /// DashScope CosyVoice 非流式语音合成请求。成功响应中包含 output.audio.url。
    /// </summary>
    public static class CosyVoiceTTS
    {
        public class RequestParam : BaseRequestParam
        {
            public RequestParam(LLMModelConfig.CosyVoiceTTSConfig config, string text, string voiceName, bool stream = false)
            {
                url = config.BaseUrl;
                method = "POST";
                networkConstant = NetworkConstant.COSYVOICE_TTS;
                timeout = 60;
                headers["Authorization"] = $"Bearer {NormalizeApiKey(config.APIKey)}";
                headers["Content-Type"] = "application/json";
                if (stream)
                {
                    headers["X-DashScope-SSE"] = "enable";
                    headers["Accept"] = "text/event-stream";
                }

                requestData = new RequestBody
                {
                    model = config.ModelName,
                    input = new InputBody
                    {
                        text = text,
                        voice = voiceName,
                        format = stream ? "pcm" : string.IsNullOrEmpty(config.AudioFormat) ? "wav" : config.AudioFormat,
                        sample_rate = config.SampleRate <= 0 ? 24000 : config.SampleRate
                    }
                };
            }

            [Serializable]
            private class RequestBody
            {
                public string model;
                public InputBody input;
            }

            [Serializable]
            private class InputBody
            {
                public string text;
                public string voice;
                public string format;
                public int sample_rate;
            }

            private static string NormalizeApiKey(string apiKey)
            {
                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    return string.Empty;
                }

                const string bearerPrefix = "Bearer ";
                apiKey = apiKey.Trim();
                if (apiKey.StartsWith(bearerPrefix, System.StringComparison.OrdinalIgnoreCase))
                {
                    apiKey = apiKey.Substring(bearerPrefix.Length).Trim();
                }

                return apiKey;
            }
        }
    }
}
