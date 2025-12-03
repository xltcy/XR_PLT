using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

/**
 * Use to manage dynamically generated objects.
 * One script binding to one object after generation.
 * Console Actions for generated objects.
 */
public class DynamicObject : MonoBehaviour
{
    public int generateActionId;
    // use to manage click3DObject state.
    private Stack<Click3DObjectManager.ClickAction> clickActionStack = new Stack<Click3DObjectManager.ClickAction>();

    // Rotating Flag.
    private RotateObjectAction rotateAction;
    // use to cal rotate angle while do rotate auto.
    private float totalRotate = 0f;
    private List<Transform> rotateParts = new List<Transform>();
    // use to reset pose
    private Vector3 originPos;
    // use to reset rot when rotate to a new rotation.
    private Quaternion originRot;

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        // Rotate Logic
        if (rotateAction != null)
        {
            foreach (var item in rotateParts)
            {
                item.Rotate(rotateAction.GetRotateAlongAxies(item), rotateAction.velocity, Space.Self);
            }
            totalRotate += rotateAction.velocity;
            if (totalRotate >= 360)
            {
                totalRotate -= 360;
            }
        }
    }

    public void ConsoleActions(ActionBase action, bool isStartAction, Action onComplete)
    {
        switch(action.type)
        {
            case ActionType.ObjectVisible:
                var visibleAction = action as ObjectVisibleAction;
                SetVisible(isStartAction);
                break;
            case ActionType.MoveObject:
                var moveAction = action as MoveObjectAction;
                SetMovement(moveAction, isStartAction);
                break;
            case ActionType.RotateObject:
                var rotateObjectAction = action as RotateObjectAction;
                SetRotate(rotateObjectAction, isStartAction);
                break;
            case ActionType.HighlightObject:
                var highlightObjectAction = action as HighlightObjectAction;
                SetHighlight(highlightObjectAction, isStartAction);
                break;
            case ActionType.WaveGenerate:
                GenerateWave(isStartAction);
                break;
            case ActionType.Explosion:
                SetExplosion(isStartAction);
                break;
            case ActionType.CustomObjectFunction:
                var customObjectAction = action as CustomObjectFunctionAction;
                ExecuteCustomObjectAction(customObjectAction, isStartAction);
                break;
            default:
                // nothing
                break;
        }
    }

    /**
     * Manage Stack update;Call Console trigger.
     */
    public void UpdateClickStateFromAction(Click3DObjectManager.ClickAction newAction)
    {
        if (clickActionStack.Count == 0 && newAction == Click3DObjectManager.ClickAction.Longclick)
        {
            // Dispatch Longclick in normal.
            return;
        }
        var isExit = clickActionStack.Contains(newAction);
        if (isExit)
        {
            while(clickActionStack.Peek() != newAction)
            {
                var dispatchAction = clickActionStack.Pop();
                ControllerRefer.SceneController.ConsoleClickTrigger(generateActionId, dispatchAction, true);
            }
            clickActionStack.Pop();
        } else
        {
            clickActionStack.Push(newAction);
        }
        ControllerRefer.SceneController.ConsoleClickTrigger(generateActionId, newAction, isExit);
    }

    private void SetVisible(bool isStartAction)
    {
        gameObject.SetActive(isStartAction);
    }

    private void SetMovement(MoveObjectAction action, bool isStartAction)
    {
        if (!isStartAction)
        {
            ResetPosition();
            return;
        }
        switch(action.moveType)
        {
            case MoveObjectAction.MoveType.MoveTo_Auto:
                SetAutoMove();
                break;
            case MoveObjectAction.MoveType.MovePointTo_Camera:
                SetMovePointToCamera(action);
                break;
            default:
                break;
        }
    }

    /**
     * Move Object;MoveTo_Auto.
     * Move Object to the front of Camera.
     */
    private void SetAutoMove()
    {
        originPos = transform.position;
        var trans = GetTranslateMovement();
        transform.Translate(trans, Space.World);
    }

    /**
     * Move object;MovePointTo_Camera;
     * Make a point in model to camera's postion.
     */
    private void SetMovePointToCamera(MoveObjectAction action)
    {
        originPos = transform.position;
        Transform cam = Camera.main.transform;

        Vector3 leftOffset = - cam.right.normalized * 1.0f;
        Vector3 placePos = cam.position + leftOffset;
        placePos.y = placePos.y - 0.7f;

        transform.position = placePos;
        //var trans = cam.position - transform.TransformPoint(action.movedPointPosition);
        //var trans = placePos - transform.TransformPoint(action.movedPointPosition);

        //transform.Translate(trans, Space.World);

        //Vector3 euler = transform.rotation.eulerAngles;
        //transform.rotation = Quaternion.Euler(0f, euler.y, 0f);
    }

    private void ResetPosition()
    {
        transform.position = originPos;
    }

    /**
     * Distribute rotate action.
     */
    private void SetRotate(RotateObjectAction rotateObjectAction, bool isStartAction)
    {
        // aksdjfls
        switch(rotateObjectAction.rotateType)
        {
            case RotateObjectAction.RotateType.RoateAlongAxies_Forward:
            case RotateObjectAction.RotateType.RotateAlongAxies_Up:
            case RotateObjectAction.RotateType.RotateAlongAxies_Right:
            case RotateObjectAction.RotateType.RoateAlongAxies_X:
            case RotateObjectAction.RotateType.RoateAlongAxies_Y:
            case RotateObjectAction.RotateType.RoateAlongAxies_Z:
                SetAutoRotate(rotateObjectAction, isStartAction);
                break;
            case RotateObjectAction.RotateType.RotateToCamera:
                SetRotateToObject(FindObjectOfType<Camera>().transform, rotateObjectAction.objectForward, rotateObjectAction.objectUp,isStartAction);
                break;
            default:
                break;
        }
    }

    private void SetAutoRotate(RotateObjectAction rotateObjectAction, bool isStartAction)
    {
        if (isStartAction)
        {
            StartRotating(rotateObjectAction);
        } else
        {
            StopRotating();
        }
    }

    private void SetRotateToObject(Transform targetTransform, Vector3 objectForward, Vector3 objectUp, bool isStartAction)
    {
        if (isStartAction)
        {
            originRot = gameObject.transform.rotation;
            // rotate
            var worldForward = transform.TransformDirection(objectForward);
            var worldUp = transform.TransformDirection(objectUp);
            var curRotation = Quaternion.LookRotation(worldForward, worldUp);
            //var targetRotation = Quaternion.LookRotation(targetTransform.forward, targetTransform.up);
            var targetRotation = Quaternion.LookRotation(new Vector3(targetTransform.forward.x,0,targetTransform.forward.z), new Vector3(0,1,0));
            var offsetRotation = targetRotation * Quaternion.Inverse(curRotation);
            transform.rotation = offsetRotation * transform.rotation;
        } else
        {
            // reset
            transform.rotation = originRot;
        }
    }

    private void StartRotating(RotateObjectAction rotateObjectAction)
    {
        
        // init rotateParts
        if (rotateObjectAction != null && rotateObjectAction.rotatableParts.Count > 0)
        {
            Transform[] allChildren = gameObject.GetComponentsInChildren<Transform>();
            
            foreach (var part in rotateObjectAction.rotatableParts)
            {
                foreach (Transform child in allChildren)
                {
                    if (child.name == part)
                    {
                        Debug.Log("找到子物体: " + child.name);
                        rotateParts.Add(child);
                        break;
                    }
                }
            }
        } else
        {
            rotateParts.Add(transform);
        }

        rotateAction = rotateObjectAction;
    }

    private void StopRotating()
    {
        var rotateObjectAction = rotateAction;
        rotateAction = null;
        if (rotateObjectAction != null)
        {
            foreach (var item in rotateParts)
            {
                item.Rotate(rotateObjectAction.GetRotateAlongAxies(item), 360 - totalRotate, Space.Self);
            }
            totalRotate = 0f;
        }
        rotateParts.Clear();
    }

    private void SetHighlight(HighlightObjectAction highlightObjectAction, bool isStartAction)
    {
        Outline otl = gameObject.GetComponent<Outline>();
        if (otl == null)
        {
            otl = gameObject.AddComponent<Outline>();
        }
        otl.OutlineColor = highlightObjectAction.highlightColor;
        otl.OutlineMode = Outline.Mode.OutlineAll;
        if (isStartAction)
        {
            otl.OutlineWidth = highlightObjectAction.highlightWidth;
        }
        else
        {
            otl.OutlineWidth = 0f;
        }
    }

    private void GenerateWave(bool isStartAction)
    {
        var generator = gameObject.GetComponent<SonarWaveManager>();
        if (isStartAction)
        {
            generator.StartGenerate();
        }
        else
        {
            generator.StopGenerateAndDestroyWave();
        }
    }

    private void SetExplosion(bool isStartAction)
    {
        var nodes = transform.GetComponentsInChildren<ModelTreeNode>();
        GameObject rootNodeObject = null;
        foreach (var i in nodes)
        {
            if (i._isRoot)
            {
                rootNodeObject = i.gameObject;
                break;
            }
        }
        if (isStartAction)
        {
            ModelTreeNode.OneDofExplosion(rootNodeObject);
        }
        else
        {
            ModelTreeNode.OneDofRecovery(rootNodeObject);
        }
    }

    /// <summary>
    /// 通用旋转方法：可指定中心点、旋转轴、角度、是否使用世界坐标轴
    /// </summary>
    /// <param name="target">要旋转的物体</param>
    /// <param name="pivot">旋转中心点（世界坐标）</param>
    /// <param name="axis">旋转轴（单位向量）</param>
    /// <param name="angle">旋转角度（度）</param>
    /// <param name="useWorldAxis">是否使用世界坐标轴</param>
    public void RotateObject(Transform target, Vector3 pivot, Vector3 axis, float angle, bool useWorldAxis = true)
    {
        if (pivot == target.position)
        {
            // 自身为旋转中心
            target.Rotate(axis, angle, useWorldAxis ? Space.World : Space.Self);
        }
        else
        {
            // 指定点为旋转中心
            target.RotateAround(pivot, axis, angle);
        }
    }

    private Vector3 GetTranslateMovement()
    {
        var cameraTrans = FindObjectOfType<ARCameraManager>().gameObject.transform;

        Vector3 trans = cameraTrans.position - GetCenterPosInWorldSpace();
        trans.y = (cameraTrans.position.y - 1.1f) - GetCenterPosInWorldSpace().y;

        BoxCollider boxCollider = GetComponent<BoxCollider>();
        float r = boxCollider.size.magnitude / 2;
        Vector3 n = trans / trans.magnitude;
        return max(trans - n * r * 3, trans * 0.6f);
    }

    private Vector3 max(Vector3 v1, Vector3 v2)
    {
        if (v1.magnitude < v2.magnitude)
        {
            return v2;
        }
        return v1;
    }
    /**
     * return BoxCollider's center position in world space.
     */
    private Vector3 GetCenterPosInWorldSpace()
    {
        return transform.TransformPoint(gameObject.GetComponent<BoxCollider>().center);
    }


    /// <summary>
    /// 调用自定义函数
    /// </summary>
    /// <param name="action"></param>
    /// <param name="isStartAction">是开始还是结束</param>
    private bool ExecuteCustomObjectAction(CustomObjectFunctionAction action, bool isStartAction)
    {
        var functionName = action.customObjectFunctionName;
        if (string.IsNullOrEmpty(functionName))
        {
            Debug.LogWarning($"action id:{action.id}-{functionName}为空");
            return false;
        }
        
        var scripts = gameObject.GetComponents<MonoBehaviour>();

        try
        {
            foreach (var script in scripts)
            {
                //不调用自己
                if (!script || script == this)
                {
                    continue;
                }
                
                var method = script.GetType().GetMethod(functionName);
                if (method != null)
                {
                    ParameterInfo[] parameters = method.GetParameters();
    
                    if (parameters.Length == 0)
                    {
                        method.Invoke(script, null);
                    }
                    else if (parameters.Length == 1)
                    {
                        // 直接传递参数，依赖类型兼容性
                        method.Invoke(script, new object[] { isStartAction });
                    }
                    else if (parameters.Length == 2)
                    {
                        // 直接传递两个参数
                        method.Invoke(script, new object[] { isStartAction, action.customObjectFunctionParam });
                    }

                    return true;
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"调用失败: action id{action.id}-{functionName}() - {e.Message}");
            return false;
        }
        
        Debug.LogWarning($"调用失败: action id:{action.id}-{functionName}() - 未找到对应函数");
        return false;
    }
}
