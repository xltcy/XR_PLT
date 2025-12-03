using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Prompt
{
    private string basePrompt;

    public static string generateShengnaPompt(string p)
    {
        p += "\n\n你现在在带领游客参观声呐，每次回答不要超过五句话，注意，当游客没有提及到声呐相关的问题时候，你不要主动提及";
        return p;
    }

    public string navigatePrompt(string p)
    {

        return p;
    }

    public static string GenerateSystemPrompt()
    {
        var sceneData = ControllerRefer.SceneController.GetCurSceneData();
        string systemPrompt = sceneData != null ? sceneData.systemPrompt : string.Empty;
        
        return systemPrompt;
    }
    
    public static string GetCurSceneLLMQuestPrompt(string voiceInput)
    {
        var sceneData = ControllerRefer.SceneController.GetCurSceneData();
        if (sceneData != null)
        {
            string prompt = sceneData.questPrompt;
            if (prompt.Contains("{0}"))
            {
                prompt = prompt.Replace("{0}", voiceInput);
            }
            else
            {
                prompt += $"；用户的问题是:{voiceInput}";
            }
            return prompt;
        }
        
        return string.Empty;
    }
}

