using Lean.Touch;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;


public class SwipeManager : MonoBehaviour
{
    #region PublicVariable
    public UnityEvent _onSwipeUp;
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
        // ��黬������
        if (IsSwipeUp(finger))
        {
            // ������ĺ���
            _onSwipeUp?.Invoke();
        }
        else if (IsSwipeLeft(finger))
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
        _swipeHandlers.Add(_onSwipeUp);
        _swipeHandlers.Add(_onSwipeLeft);
        _swipeHandlers.Add(_onSwipeRight);
    }
    #endregion


    #region Behaviour
    // Start is called before the first frame update
    void Start()
    {
        HandlerListInit();
    }

    private void OnEnable()
    {
        // ���Ļ����¼�
        LeanTouch.OnFingerSwipe += HandleSwipe;
    }

    private void OnDisable()
    {
        // ȡ�����Ļ����¼�
        LeanTouch.OnFingerSwipe -= HandleSwipe;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    #endregion

    #region Other

    private bool IsSwipeUp(LeanFinger finger)
    {
        // ��ȡ�����ķ���
        var swipeDelta = finger.SwipeScreenDelta;

        // �жϻ����Ƿ���Ҫ�����ϵ�
        return swipeDelta.y > 0 && swipeDelta.y > Math.Abs(swipeDelta.x) && IsTagUI(finger)==false;
    }

    private bool IsSwipeLeft(LeanFinger finger)
    {
        // ��ȡ�����ķ���
        var swipeDelta = finger.SwipeScreenDelta;

        // �жϻ����Ƿ���Ҫ�������
        return swipeDelta.x < 0 && -swipeDelta.x > Math.Abs(swipeDelta.y) && IsTagUI(finger)==false;
    }

    private bool IsSwipeRight(LeanFinger finger)
    {
        // ��ȡ�����ķ���
        var swipeDelta = finger.SwipeScreenDelta;

        // �жϻ����Ƿ���Ҫ�����ҵ�
        return swipeDelta.x > 0 && swipeDelta.x > Math.Abs(swipeDelta.y) && IsTagUI(finger)==false;
    }

    private bool IsTagUI(LeanFinger finger)
    {
        // ���� PointerEventData
        PointerEventData pointerEventData = new PointerEventData(_eventSystem)
        {
            position = finger.StartScreenPosition
        };

        // �洢���߼����
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
