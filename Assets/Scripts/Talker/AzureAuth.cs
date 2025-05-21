using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CognitiveServices.Speech;

static class AzureAuth
{
    public static readonly SpeechConfig SpeechConfig = SpeechConfig.FromSubscription("92558acf9b4343989b766a852130b139", "eastasia");
    static AzureAuth()
    {
        //SpeechConfig.SpeechRecognitionLanguage = Config.Configs.Language;
        //SpeechConfig.SpeechSynthesisLanguage = Config.Configs.Language;
        SpeechConfig.SpeechRecognitionLanguage = Config.Configs.Language;
        SpeechConfig.SpeechSynthesisLanguage = Config.Configs.Language;
    }
}
