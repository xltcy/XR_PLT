using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Numerics;

// ---------- Trigger 信息 ----------
[Serializable]
public class ActionTriggerData
{
    // ------------Essential------------//
    [JsonConverter(typeof(StringEnumConverter))]
    public TriggerMode mode = TriggerMode.Immediate;
    // delay time(s) all mode
    public float delay = 0;

    //-------------Optional------------//
    // AfterAction mode
    public int afterActionId; // 关键字或动作名
    public bool isWhenActionStart = true; // true follow startTrigger
    // VoiceKeyword mode
    public string matchPattern;
    // PassbySpot mode
    public Vector3 spotPosition;

    //-------------JsonIgnore-----------//
    [JsonIgnore]
    public Dictionary<int, bool> nextActionIds = new Dictionary<int, bool>(); // [ActionId: isStartAction] use to calculate startWithTrigger.
    

    public ActionTriggerData(TriggerMode mode, float delay = 0)
    {
        this.mode = mode;
        this.delay = delay;
    }

    public static ActionTriggerData NewImmediateTrigger(float delaySecondTime = 0)
    {
        return new ActionTriggerData(TriggerMode.Immediate, delaySecondTime);
    }

    public static ActionTriggerData NewAfterActionTrigger(int afterActionId, bool isWhenActionStart = true, float delaySecondTime = 0)
    {
        var res = new ActionTriggerData(TriggerMode.AfterAction, delaySecondTime);
        res.afterActionId = afterActionId;
        res.isWhenActionStart = isWhenActionStart;
        return res;
    }

    public static ActionTriggerData NewVoiceKeywordTrigger(string keywordPattern, float delaySecondTime = 0)
    {
        var res = new ActionTriggerData(TriggerMode.VoiceKeyword, delaySecondTime);
        res.matchPattern = keywordPattern;
        return res;
    }

    public static ActionTriggerData NewNeverTrigger()
    {
        return new ActionTriggerData(TriggerMode.Never);
    }

    public static ActionTriggerData NewPassbySpotTrigger(Vector3 spotPos, float delaySecondTime = 0)
    {
        var res = new ActionTriggerData(TriggerMode.PassbySpot, delaySecondTime);
        res.spotPosition = spotPos;
        return res;
    }

    public ActionTriggerCommand GetTriggerCommands(int actionId)
    {
        if (mode == TriggerMode.VoiceKeyword)
        {
            return new ActionTriggerCommand("", actionId, this);
        }
        return null;
    }

    
}

[JsonConverter(typeof(StringEnumConverter))]
public enum TriggerMode
{
    // do when init
    Immediate,
    // do after an action finish.
    AfterAction,
    // voiceController match patterns
    VoiceKeyword,
    // never do something or auto stop
    Never,
    // Passby a spot
    PassbySpot,
}
