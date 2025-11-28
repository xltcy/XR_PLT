using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SceneData
{
    public string sceneModelPath;
    public string sceneMaterialPath;
    public string sceneName;
    public Vector3 initPosition;
    public long timestampMs;
    public string relocateUrlMid;
    // Use in SelectDesController
    public List<ExplanationPoint> explanationPoints;
    public List<ActionBase> globalActions;
    // Auto add gameObject
    public List<ObjectData> objects;

    // LLM System Prompt, 用于初始化对话，仅在对话开始时发送一次
    public string systemPrompt;
    // LLM Quest Prompt, 其中{0}将被替换为用户问题，在每一句对话都会发送该字符串
    public string questPrompt;

    // todo delete
    void temp()
    {
        // GetTimeStamp;
        long timestampMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        Debug.Log(timestampMs);
    }
}

[Serializable]
public class ExplanationPoint
{
    public string id;
    public string title;
    public Vector3 position;
    public string initialIntroduction;
    public string arriveIntroduction;
    public string thumb;
    public List<ActionBase> actions;
}