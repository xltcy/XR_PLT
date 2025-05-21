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
    // ��ö���Ƶ��ඨ��Ŀ�ʼ��
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
            State.NORMAL => "��������",
            State.PRESSED => "�뽲��...",
            State.CONSOLING => "���ڴ���",
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

    // �������Ҫ��Щ�����������Ƴ�����
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