using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class EditModeManager : BaseController
{
    // Start is called before the first frame update
    public Camera arCamera;

    //操作对象的类型
    public enum OperationTarget
    {
        Mesh = 0,
        Ground = 1
    }

    //根据类型获取物体
    public GameObject GetMeshObj(OperationTarget target)
    {
        string tag = string.Empty;
        switch (target)
        {
            case OperationTarget.Ground:
                tag = "Sonar";
                break;
            case OperationTarget.Mesh:
            default:
                tag = "Mesh";
                break;
        }
        GameObject meshObj = string.IsNullOrEmpty(tag) ? null : GameObject.FindGameObjectWithTag(tag);
        return meshObj;
    }
    
    public Camera GetARCamera()
    {
        return arCamera;
    }
    
    //物体变换的类型
    public enum OperationType
    {
        Move = 0,
        Rotate = 1,
        Scale = 2
    }

    //物体变换的方向
    public enum OperationDirection
    {
        Forward,
        Back,
        Left,
        Right,
        Up,
        Down
    }
    
    //处理物体变换
    public void ProcessGoTransform(GameObject go, OperationType opType, OperationDirection opDir, float stepLen)
    {
        if (go == null) return;
        Vector3 vec = Vector3.one;
        if (opType == OperationType.Move)
        {
            vec = new Vector3(opDir == OperationDirection.Left ? -stepLen : opDir == OperationDirection.Right ? stepLen : 0,
                opDir == OperationDirection.Down ? -stepLen : opDir == OperationDirection.Up ? stepLen : 0,
                opDir == OperationDirection.Back ? -stepLen : opDir == OperationDirection.Forward ? stepLen : 0);
        }
        else if (opType == OperationType.Rotate)
        {
            vec = new Vector3(opDir == OperationDirection.Down ? -stepLen : opDir == OperationDirection.Up ? stepLen : 0,
                opDir == OperationDirection.Left ? stepLen : opDir == OperationDirection.Right ? -stepLen : 0,
                opDir == OperationDirection.Back ? -stepLen : opDir == OperationDirection.Forward ? stepLen : 0);
        }

        switch (opType)
        {
            case OperationType.Move:
                go.transform.Translate(vec);
                break;
            case OperationType.Rotate:
                go.transform.Rotate(vec);
                break;
            case OperationType.Scale:
                float factor = opDir == OperationDirection.Down || opDir == OperationDirection.Left || opDir == OperationDirection.Back ? -1 : 1;
                go.transform.localScale += factor * new Vector3(stepLen, stepLen, stepLen);
                break;
        }
    }

}
