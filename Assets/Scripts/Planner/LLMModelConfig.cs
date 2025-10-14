public class LLMModelConfig
{
    public class BaseConfig
    {
        public string ModelName;
        public string APIKey;
        public string BaseUrl;
    }

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