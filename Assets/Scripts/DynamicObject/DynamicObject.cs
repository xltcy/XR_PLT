using System;
using System.Collections;
using System.Collections.Generic;
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
            case ActionType.Explosion:
                SetExplosion(isStartAction);
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
                FindObjectOfType<SceneController>().ConsoleClickTrigger(generateActionId, dispatchAction, true);
            }
            clickActionStack.Pop();
        } else
        {
            clickActionStack.Push(newAction);
        }
        FindObjectOfType<SceneController>().ConsoleClickTrigger(generateActionId, newAction, isExit);
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
        var trans = FindObjectOfType<Camera>().transform.position - transform.TransformPoint(action.movedPointPosition);
        transform.Translate(trans, Space.World);
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
            var targetRotation = Quaternion.LookRotation(targetTransform.forward, targetTransform.up);
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

        BoxCollider boxCollider = GetComponent<BoxCollider>();
        float r = boxCollider.size.magnitude / 2;
        Vector3 n = trans / trans.magnitude;
        return max(trans - n * r * 3, trans * 0.72f);
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
}
