using Lean.Touch;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UniGLTF;
using UnityEngine;
using static UnityEditor.ShaderGraph.Internal.KeywordDependentCollection;


public class LeanTouchHandler : MonoBehaviour
{
    int count;
    Transform _origin_parent;

    private void OnEnable()
    {
        LeanTouch.OnFingerTap += HandleTap;
        LeanTouch.OnFingerSwipe += HandleSwipe;
    }

    private void OnDisable()
    {
        LeanTouch.OnFingerTap -= HandleTap;
        LeanTouch.OnFingerSwipe -= HandleSwipe;
    }

    private void HandleTap(LeanFinger finger)
    {
        const float DISTANCE_TO_CAM = 0.4f;

        if (count % 2 == 0)
        {
            // 恢复材质
            FindObjectOfType<MaterialManager>().RestoreMaterials(gameObject.transform.GetComponent<MeshRenderer>());

            // 把物体移动到相机前
            _origin_parent = transform.parent;
            var cam = Camera.main;
            gameObject.transform.SetParent(cam.transform, false);
            if (Application.platform == RuntimePlatform.Android)
            {
                Vector3 targetPosition = cam.transform.position +
                                         cam.transform.forward * DISTANCE_TO_CAM;
                Vector3 offset = transform.GetChildByName("center").position;
                targetPosition = targetPosition - offset;
                transform.position = targetPosition;
                Debug.Log(transform.position);
                Debug.Log(transform.localPosition);
            }
            else
            {
                transform.localPosition = new Vector3(0.28f, 0.24f, -0.95f);
            }
            transform.RotateAround(transform.GetChildByName("center").position, cam.transform.forward, 180.0f);
        }
        else
        {
            // 隐藏材质
            FindObjectOfType<MaterialManager>().ReplaceAllMaterials(gameObject.transform.GetComponent<MeshRenderer>());

            // 把物体放回原位
            transform.SetParent(_origin_parent, false);
            transform.localPosition = Vector3.zero;
            transform.rotation = Quaternion.identity;
        }
        count++;
    }

    private void HandleSwipe(LeanFinger finger)
    {
        const float ROTATE_ANGLE = 45.0f;

        if (IsSwipeLeft(finger))
        {
            // 物体绕z轴顺时针转angle度
            RotateController.RotateToTarget(gameObject, ROTATE_ANGLE, Camera.main.transform.up);
        }
        else if (IsSwipeRight(finger))
        {
            // 物体绕z轴逆时针转angle度
            RotateController.RotateToTarget(gameObject, -ROTATE_ANGLE, Camera.main.transform.up);
        }
    }

    private bool IsSwipeLeft(LeanFinger finger)
    {
        // 获取滑动的方向
        var swipeDelta = finger.SwipeScreenDelta.normalized;

        // 判断滑动是否主要是向左的
        return swipeDelta.x < 0 && -swipeDelta.x > Math.Abs(swipeDelta.y);
    }

    private bool IsSwipeRight(LeanFinger finger)
    {
        // 获取滑动的方向
        var swipeDelta = finger.SwipeScreenDelta.normalized;

        // 判断滑动是否主要是向右的
        return swipeDelta.x > 0 && swipeDelta.x > Math.Abs(swipeDelta.y);
    }

    private void Start()
    {
        count = 0;
    }
}
