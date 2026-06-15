public class LLMModelConfig
{
    public class BaseConfig
    {
        public string ModelName;
        public string APIKey;
        public string BaseUrl;
    }

    public class Chat
    {
        public static BaseConfig Local = new BaseConfig
        {
            ModelName = "qwen2.5:72b",
            APIKey = "",
            BaseUrl = "http://60.205.232.241:7171/ai_app/chat/",
        };

        public static BaseConfig Deepseek = new BaseConfig
        {
            ModelName = "deepseek-chat",
            APIKey = "sk-e7203af36173493d810f16bef38c25ec",
            BaseUrl = "https://api.deepseek.com/chat/completions",
        };

        public static BaseConfig Rag = new BaseConfig
        {
            ModelName = "model",
            APIKey = "ragflow-k3YzUzZjRhYThjOTExZjA5YmExZWVjNm",
            BaseUrl = "http://10.243.57.216/api/v1/chats_openai/cecbd516a8ca11f0aa56eec6aeaa6391/chat/completions",
        };
    }

    
    public class MimoTTSConfig : BaseConfig
    {
        public string FemaleVoiceName;
        public string MaleVoiceName;
        public string AudioFormat;
        public string Prompt;
    }

    public class CosyVoiceTTSConfig : BaseConfig
    {
        public string FemaleVoiceName;
        public string MaleVoiceName;
        public string AudioFormat;
        public int SampleRate;
    }

    public class Speech
    {
        public static MimoTTSConfig MimoTTS = new MimoTTSConfig
        {
            ModelName = "mimo-v2.5-tts",
            APIKey = "sk-cjl7runr2v3vln8zvwivggfyhukvaioj8o2vqthv30e8nl8h",
            BaseUrl = "https://api.xiaomimimo.com/v1/chat/completions",
            FemaleVoiceName = "茉莉",
            MaleVoiceName = "白桦",
            AudioFormat = "wav",
            Prompt = "专业、自然、亲切的中文实验室讲解员语气，语速适中，吐字清晰。"
        };

        public static CosyVoiceTTSConfig CosyVoice = new CosyVoiceTTSConfig
        {
            ModelName = "cosyvoice-v3-flash",
            //APIKey = "sk-ws-H.RERXRMR.Y7K6.MEUCIQCJeJkzxYJma6iIEb3TZqqDivnwIrGda7b1L9KT9H8ydgIgQWQLOD1jU8ujadiW4ctVq6hlrb46B0LJMRcHhnXL9Fk",
            APIKey = "sk-8f2c80a894944575917ed51b0d66826f",
            BaseUrl = "https://dashscope.aliyuncs.com/api/v1/services/audio/tts/SpeechSynthesizer",
            FemaleVoiceName = "longanling_v3",
            MaleVoiceName = "longanzhi_v3",
            AudioFormat = "wav",
            SampleRate = 24000
        };
    }

    public static BaseConfig Local => Chat.Local;
    public static BaseConfig Deepseek => Chat.Deepseek;
    public static BaseConfig Rag => Chat.Rag;
    public static MimoTTSConfig MimoTTS => Speech.MimoTTS;
    public static CosyVoiceTTSConfig CosyVoice => Speech.CosyVoice;
}
