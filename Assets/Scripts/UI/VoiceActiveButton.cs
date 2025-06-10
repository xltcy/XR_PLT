using System.Collections;
using System.Collections.Generic;
using Unity.VectorGraphics;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

/// <summary>
/// State Change:
/// 1. normal: enable,clickable.
/// 2. pressed: is being pressed by user.
/// 3. consoling: disable, unclickable.
/// </summary>
public class VoiceActiveButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
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

    public Sprite spriteNormal;

    public Sprite[] spritePressing;
    public Sprite spriteLoading;

    [Header("动画参数")]
    public float recordingFrameRate = 0.1f; // 录音动画帧间隔
    public float loadingRotationSpeed = 200f; // 加载旋转速度

    private bool errorHasOccur = false;

    private Coroutine mockImageAnimateCoroutine;

    private SVGImage svgImage {
        get
        {
            return gameObject.GetComponentInChildren<SVGImage>();
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!gameObject.activeSelf)
            return;

        state = State.PRESSED;
        errorHasOccur = false;
        UIFixByState();
        onPointerDown?.Invoke();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!gameObject.activeSelf)
            return;

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
        // GetComponent<Image>().color = color;

        if (state != State.NORMAL && mockImageAnimateCoroutine == null)
        {
            mockImageAnimateCoroutine = StartCoroutine(MockIconAnimate());
        }
    }

    private IEnumerator MockIconAnimate()
    {
        // 录音动画
        int currentFrame = 0;
        while (state == State.PRESSED)
        {
            // 循环播放动画帧
            svgImage.sprite = spritePressing[currentFrame];
            currentFrame = (currentFrame + 1) % spritePressing.Length;

            yield return new WaitForSeconds(recordingFrameRate);
        }

        // 加载旋转动画
        svgImage.sprite = spriteLoading;
        svgImage.transform.localRotation = Quaternion.identity;
        while (state == State.CONSOLING)
        {
            svgImage.transform.Rotate(0, 0, -loadingRotationSpeed * Time.deltaTime);
            yield return null;
        }

        svgImage.sprite = spriteNormal;
        svgImage.transform.localRotation = Quaternion.identity;
        mockImageAnimateCoroutine = null;
    }
}