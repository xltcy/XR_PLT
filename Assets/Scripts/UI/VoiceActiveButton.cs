using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// State Change:
/// 1. normal: enable,clickable.
/// 2. pressed: is being pressed by user.
/// 3. consoling: disable, unclickable.
/// </summary>
public class VoiceActiveButton : Button
{
    // 将枚举移到类定义的开始处
    public enum State
    {
        NORMAL,
        PRESSED,
        CONSOLING
    }

    public State state = State.NORMAL;
    public UnityEvent onPointerDown;
    public UnityEvent onPointerUp;
    private bool errorHasOccur = false;

    public override void OnPointerDown(PointerEventData eventData)
    {
        if (!IsActive() || !IsInteractable())
            return;

        base.OnPointerDown(eventData);
        state = State.PRESSED;
        errorHasOccur = false;
        UIFixByState();
        onPointerDown?.Invoke();
    }

    public override void OnPointerUp(PointerEventData eventData)
    {
        if (!IsActive() || !IsInteractable())
            return;

        base.OnPointerUp(eventData);
        if (!errorHasOccur)
        {
            state = State.CONSOLING;
            UIFixByState();
        }
        onPointerUp?.Invoke();
    }

    public void ResetBtn()
    {
        errorHasOccur = true;
        state = State.NORMAL;
        UIFixByState();
    }

    private void UIFixByState()
    {
        this.enabled = state != State.CONSOLING;
        string text = state switch
        {
            State.NORMAL => "语音命令",
            State.PRESSED => "请讲话...",
            State.CONSOLING => "正在处理",
            _ => ""
        };

        Color color = state switch
        {
            State.NORMAL => Color.white,
            State.PRESSED => Color.blue,
            State.CONSOLING => Color.red,
            _ => Color.white
        };

        GetComponentInChildren<Text>().text = text;
        GetComponent<Image>().color = color;
    }

    // 如果不需要这些方法，可以移除它们
    public override void OnPointerClick(PointerEventData eventData)
    {
        base.OnPointerClick(eventData);
    }

    public override void OnPointerEnter(PointerEventData eventData)
    {
        base.OnPointerEnter(eventData);
    }

    public override void OnPointerExit(PointerEventData eventData)
    {
        base.OnPointerExit(eventData);
    }
}