using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using UnityEngine;

public class ProgramEvent
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum ProgramEventType
    {
        HIGHLIGHT_OBJECT,
    }
    
    #region jsonConverter
    [JsonConverter(typeof(ProgramEventParamConverter))] // 关键
    public class ProgramEventParamBase
    {
        public ProgramEventType type;
        public virtual string GetEventConstant()
        {
            return string.Empty;
        }
    }

    public class ProgramEventParamConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(ProgramEventParamBase);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
            JsonSerializer serializer)
        {
            var jo = JObject.Load(reader);
            var typeToken = jo["type"].Value<string>();
            if (typeToken == null)
                throw new Exception("Missing 'type' field in ProgramEventParam.");

            ProgramEventType type = (ProgramEventType)Enum.Parse(typeof(ProgramEventType), typeToken);
            ProgramEventParamBase programEventParam;

            switch (type)
            {
                case ProgramEventType.HIGHLIGHT_OBJECT:
                    programEventParam = new HighlightNodeEventParam();
                    break;
                //... 添加更多事件类型
                default:
                    throw new Exception($"Unknown ProgramEvent type: {type}");
            }

            serializer.Populate(jo.CreateReader(), programEventParam);
            return programEventParam;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            JObject jo = JObject.FromObject(value, serializer);
            jo.WriteTo(writer);
        }
    }
    #endregion jsonConverter
    
    //... 添加更多事件类型
    #region 具体参数类
    public class HighlightNodeEventParam : ProgramEventParamBase
    {
        public string nodeName;
        public Color highlightColor;
        public float highlightWidth;
        
        public override string GetEventConstant()
        {
            return EventConstant.HIGHLIGHT_OBJECT;
        }
    }
    
    #endregion
}
