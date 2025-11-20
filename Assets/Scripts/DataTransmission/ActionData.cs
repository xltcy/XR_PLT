using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using UnityEngine;

[JsonConverter(typeof(StringEnumConverter))]
public enum ActionType
{
    GenerateObject,
    ObjectVisible,
    PlayVideo,
    MoveObject,
    RotateObject,
    HighlightObject,
    Introduce,
    Explosion,
    WaveGenerate,
    AvatarAnim,
    CustomFunction,
}

[JsonConverter(typeof(ActionConverter))] // 关键
public abstract class ActionBase
{
    public ActionType type;
    public ActionTriggerData startTrigger;
    public ActionTriggerData stopTrigger;
    public int id;

    public bool IsClickTriggered(Click3DObjectManager.ClickAction newClickAction, bool isExitNew, out bool isStartTriggered)
    {
        var startTriggered = startTrigger.IsClickTriggered(newClickAction, isExitNew);
        var stopTriggered = stopTrigger.IsClickTriggered(newClickAction, isExitNew);
        isStartTriggered = startTriggered;
        return startTriggered || stopTriggered;
    }
}

/**
 * Add GameObject for an ExplanationPoint
 * Default not visible.
 * Use [ObjectVisibleAction] startTrigger stopTrigger to controll visibility.
 */
public class AddObjectAction : ActionBase
{
    public string objectDataId;
    public Vector3 position;
    public Vector4 rotation;
    public Vector3 scale;

    public AddObjectAction()
    {
        type = ActionType.GenerateObject;
        startTrigger = ActionTriggerData.NewImmediateTrigger();
        stopTrigger = ActionTriggerData.NewNeverTrigger();
    }

    public Quaternion GetRotationQuaternion()
    {
        return new Quaternion(rotation.x, rotation.y, rotation.z, rotation.w);
    }

    public void SetRotationQuaternion(Quaternion q)
    {
        rotation = new Vector4(q.x, q.y, q.z, q.w);
    }
}

public class PlayVideoAction : ActionBase
{
    public string videoPath;
    public Vector3 position;
    public Vector4 rotation;
    public Vector3 scale;

    public Quaternion GetRotationQuaternion()
    {
        return new Quaternion(rotation.x, rotation.y, rotation.z, rotation.w);
    }

    public void SetRotationQuaternion(Quaternion q)
    {
        rotation = new Vector4(q.x, q.y, q.z, q.w);
    }
}

public class IntroduceAction : ActionBase
{
    public string introduction;
}

public class AvatarAnimAction: ActionBase
{
    public string animTrigger;
}

/**
 * Use to Converte Abstract class ActionBase.
 */
public class ActionConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(ActionBase);
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        var jo = JObject.Load(reader);
        var typeToken = jo["type"].Value<string>();
        if (typeToken == null)
            throw new Exception("Missing 'type' field in Action.");

        ActionType type = (ActionType)Enum.Parse(typeof(ActionType), typeToken);
        ActionBase action;

        switch (type)
        {
            case ActionType.GenerateObject:
                action = new AddObjectAction();
                break;
            case ActionType.ObjectVisible:
                action = new ObjectVisibleAction();
                break;
            case ActionType.PlayVideo:
                action = new PlayVideoAction();
                break;
            case ActionType.MoveObject:
                action = new MoveObjectAction();
                break;
            case ActionType.RotateObject:
                action = new RotateObjectAction();
                break;
            case ActionType.HighlightObject:
                action = new HighlightObjectAction();
                break;
            case ActionType.Introduce:
                action = new IntroduceAction();
                break;
            case ActionType.Explosion:
                action = new ExplosionAction();
                break;
            case ActionType.WaveGenerate:
                action = new WaveGenerateAction();
                break;
            case ActionType.AvatarAnim:
                action = new AvatarAnimAction();
                break;
            case ActionType.CustomFunction:
                action = new CustomFunctionAction();
                break;
            default:
                throw new Exception($"Unknown action type: {type}");
        }

        serializer.Populate(jo.CreateReader(), action);
        return action;
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        JObject jo = JObject.FromObject(value, serializer);
        jo.WriteTo(writer);
    }
}
