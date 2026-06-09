//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using Microsoft.CognitiveServices.Speech;

//static class AzureAuth
//{
//    public static readonly SpeechConfig SpeechConfig = SpeechConfig.FromSubscription("92558acf9b4343989b766a852130b139", "eastasia");
//    static AzureAuth()
//    {
//        //SpeechConfig.SpeechRecognitionLanguage = Config.Configs.Language;
//        //SpeechConfig.SpeechSynthesisLanguage = Config.Configs.Language;
//        SpeechConfig.SpeechRecognitionLanguage = Config.Configs.Language;
//        SpeechConfig.SpeechSynthesisLanguage = Config.Configs.Language;
//    }
//}

using Microsoft.CognitiveServices.Speech;

public static class AzureAuth
{
    public const string MaleVoiceName = "zh-CN-YunfengNeural";
    public const string FemaleVoiceName = "zh-CN-XiaoxiaoNeural";

    private static SpeechConfig _speechConfig;
    private static string _speechSynthesisVoiceName = MaleVoiceName;

    public static SpeechConfig SpeechConfig
    {
        get
        {
            if (_speechConfig == null)
            {
                _speechConfig = SpeechConfig.FromSubscription("92558acf9b4343989b766a852130b139", "eastasia");
                _speechConfig.SpeechSynthesisVoiceName = _speechSynthesisVoiceName;
                _speechConfig.SetSpeechSynthesisOutputFormat(SpeechSynthesisOutputFormat.Riff24Khz16BitMonoPcm);
            }
            return _speechConfig;
        }
    }

    public static string SpeechSynthesisVoiceName => _speechSynthesisVoiceName;

    public static void SetSpeechSynthesisVoiceName(string voiceName)
    {
        if (string.IsNullOrEmpty(voiceName) || voiceName == _speechSynthesisVoiceName)
        {
            return;
        }

        _speechSynthesisVoiceName = voiceName;
        if (_speechConfig != null)
        {
            _speechConfig.SpeechSynthesisVoiceName = _speechSynthesisVoiceName;
        }
    }
}
