using Lean.Touch;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;


public class SwipeManager : BaseController
{
    #region PublicVariable
    public UnityEvent _onSwipeLeft;
    public UnityEvent _onSwipeRight;
    public List<UnityEvent> _swipeHandlers;
    [Header("UI Manager")]
    public EventSystem _eventSystem;
    public GraphicRaycaster _graphicRaycaster;
    #endregion

    #region PrivateVariable
    private LayerMask buttonLayerMask;
    #endregion

    #region Handler
    private void HandleSwipe(LeanFinger finger)
    {
        // 检查滑动方向
        if (IsSwipeLeft(finger))
        {
            _onSwipeLeft?.Invoke();
        }
        else if (IsSwipeRight(finger))
        {
            _onSwipeRight?.Invoke();
        }
    }

    private void HandlerListInit()
    {
        _swipeHandlers.Add(_onSwipeLeft);
        _swipeHandlers.Add(_onSwipeRight);
    }
    #endregion


    #region Behaviour

    public override void OnRegister()
    {
        base.OnRegister();
        Init();
    }

    public override void OnUnregister()
    {
        base.OnUnregister();
    }

    // Start is called before the first frame update
    void Init()
    {
        HandlerListInit();
    }

    private void OnEnable()
    {
        // 订阅滑动事件
        LeanTouch.OnFingerSwipe += HandleSwipe;
    }

    private void OnDisable()
    {
        // 取消订阅滑动事件
        LeanTouch.OnFingerSwipe -= HandleSwipe;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    #endregion

    #region Other

    private bool IsSwipeLeft(LeanFinger finger)
    {
        // 获取滑动的方向
        var swipeDelta = finger.SwipeScreenDelta;

        // 判断滑动是否主要是向左的
        return swipeDelta.x < 0 && -swipeDelta.x > Math.Abs(swipeDelta.y) && IsTagUI(finger)==false;
    }

    private bool IsSwipeRight(LeanFinger finger)
    {
        // 获取滑动的方向
        var swipeDelta = finger.SwipeScreenDelta;

        // 判断滑动是否主要是向右的
        return swipeDelta.x > 0 && swipeDelta.x > Math.Abs(swipeDelta.y) && IsTagUI(finger)==false;
    }

    private bool IsTagUI(LeanFinger finger)
    {
        // 创建 PointerEventData
        PointerEventData pointerEventData = new PointerEventData(_eventSystem)
        {
            position = finger.StartScreenPosition
        };

        // 存储射线检测结果
        List<RaycastResult> results = new List<RaycastResult>();
        _graphicRaycaster.Raycast(pointerEventData, results);

        foreach (var result in results)
        {
            if (result.gameObject.layer == LayerMask.NameToLayer("UI"))
            {
                return true;
            }
        }
        return false;
    }
    #endregion
}
