using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[JsonConverter(typeof(StringEnumConverter))]
public enum ObjectActionType
{
    
}

//GenerateObject,
//ObjectVisible,
//MoveObject,
//RotateObject,
//HighlightObject,
//Explosion,
//WaveGenerate,

public class ObjectActionBase : ActionBase
{
    public int generateActionId;
}

public class ObjectVisibleAction : ObjectActionBase
{
}

public class MoveObjectAction : ObjectActionBase
{
    public MoveType moveType;

    // For MovePointTo_Camera mode.
    public Vector3 movedPointPosition;

    [JsonConverter(typeof(StringEnumConverter))]
    public enum MoveType
    {
        // move to front of camera, auto calculate position.
        MoveTo_Auto,
        // move a point in model to camera's postion.
        MovePointTo_Camera
    }
} 

public class RotateObjectAction : ObjectActionBase
{
    public RotateType rotateType;
    //-------------Rotate Along Object self's axies param------------//
    // rotate degree once; > 0 clockwise; < 0 anticlockwise.
    public float velocity = 1f;
    // empty => rotate total object; otherwise rotate partially.
    public List<string> rotatableParts;
    //-------------Rotate Object as other's Rotation------------//
    public Vector3 objectForward;
    public Vector3 objectUp;
    //-------------------------------------------------------------//

    public Vector3 GetRotateAlongAxies(Transform transform)
    {
        var axies = new Vector3();
        switch (rotateType)
        {
            case RotateType.RoateAlongAxies_Forward:
                axies = transform.forward;
                break;
            case RotateType.RotateAlongAxies_Up:
                axies = transform.up;
                break;
            case RotateType.RotateAlongAxies_Right:
                axies = transform.right;
                break;
            case RotateType.RoateAlongAxies_X:
                axies = new Vector3(1f, 0f, 0f);
                break;
            case RotateType.RoateAlongAxies_Y:
                axies = new Vector3(0f, 1f, 0f);
                break;
            case RotateType.RoateAlongAxies_Z:
                axies = new Vector3(0f, 0f, 1f);
                break;
            default:
                break;
        }
        return axies;
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum RotateType
    {
        //-------------Rotate Along Object self's axies------------//
        RoateAlongAxies_Forward,
        RotateAlongAxies_Up,
        RotateAlongAxies_Right,
        RoateAlongAxies_X,
        RoateAlongAxies_Y,
        RoateAlongAxies_Z,
        //-------------Rotate Object as other's Rotation------------//
        RotateToCamera
    }
}

public class HighlightObjectAction : ObjectActionBase
{
    public Color highlightColor;
    public float highlightWidth;
}

public class ExplosionAction : ObjectActionBase
{
    //TODO
}

public class WaveGenerateAction : ObjectActionBase
{
    // TODO
}

public class CustomFunctionAction : ObjectActionBase
{
    public string customFunctionName;
}

