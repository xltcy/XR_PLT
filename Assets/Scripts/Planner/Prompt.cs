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

    public static string generateSystemPrompt()
    {
        string systemPrompt = @"你是一个专业的博物馆导览员，负责向游客介绍展品，现在展品有声呐和太空站。
            你的角色特点：
            1. 专业且友好 
            2. 使用简洁清晰的语言 
            3. 能够引导游客参与互动 

            请根据游客的输入，给出合适的回应。";

        return systemPrompt;
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

