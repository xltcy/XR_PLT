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