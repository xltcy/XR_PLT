using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Click3DObjectManager : MonoBehaviour
{
    public Camera arCamera;

    public Color normalColor;
    public Color highlightColor;
    public Color hideColor;

    private String logStr = "";

    private List<ClickableObject> clickableObjs = new List<ClickableObject>();
    private DateTime touchStartTime;
    private int TOUCH_TIME_THRESHOLD = 200;
    private int LONG_TOUCH_TIME_TRHRESHOLD = 1000;
    private bool isAllInvisible = false;
    // Start is called before the first frame update
    void Start()
    {
        // InitClickableObjs();
    }

    // Update is called once per frame
    void Update()
    {
        if (Application.platform == RuntimePlatform.Android)
        {
            FingerClick();
        } else
        {
            MouseClick();
        }
    }

    public void RegisteClickableObject(ClickableObject item)
    {
        if (item.colorFollowParent)
        {
            item.highlightColor = highlightColor;
            item.normalColor = normalColor;
            item.hideColor = hideColor;
        }
        clickableObjs.Add(item);
    }

    private void MouseClick()
    {
        if (Input.GetMouseButtonDown(0))
        {
            touchStartTime = DateTime.Now;
        }
        if (Input.GetMouseButtonUp(0))
        {
            bool isShortTouch = (DateTime.Now - touchStartTime).TotalMilliseconds < TOUCH_TIME_THRESHOLD;
            bool isLongTouch = (DateTime.Now - touchStartTime).TotalMilliseconds > LONG_TOUCH_TIME_TRHRESHOLD;
            if (isShortTouch || isLongTouch)
            {
                Vector3 mousePos = Input.mousePosition;
                Vector3 worldPoint = Camera.main.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, arCamera.nearClipPlane));
                Ray ray = arCamera.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    foreach (var item in clickableObjs)
                    {
                        if (hit.collider.gameObject == item.gameObject)
                        {
                            Debug.Log("Object Clicked:" + item.name);
                            HitObject(item, isLongTouch);
                        }
                    }
                }
            }
        }
    }

    private void FingerClick()
    {
        if (Input.touchCount == 1)
        {
            Touch touch = Input.GetTouch(0);
            switch (touch.phase)
            {
                case TouchPhase.Began:
                    touchStartTime = DateTime.Now;
                    break;
                case TouchPhase.Ended:
                    bool isShortTouch = (DateTime.Now - touchStartTime).TotalMilliseconds < TOUCH_TIME_THRESHOLD;
                    bool isLongTouch = (DateTime.Now - touchStartTime).TotalMilliseconds > LONG_TOUCH_TIME_TRHRESHOLD;
                    if (isShortTouch || isLongTouch)
                    {
                        Vector2 pos = touch.position;
                        Vector3 worldPoint = Camera.main.ScreenToWorldPoint(new Vector3(pos.x, pos.y, arCamera.nearClipPlane));
                        Ray ray = arCamera.ScreenPointToRay(pos);
                        if (Physics.Raycast(ray, out RaycastHit hit))
                        {
                            foreach (var item in clickableObjs)
                            {
                                if (hit.collider.gameObject == item.gameObject)
                                {
                                    Debug.Log("Object Clicked:" + item.name);
                                    HitObject(item, isLongTouch);
                                }
                            }
                        }
                    }
                    break;
                default: break;
            }
        }
    }

    /**
     * Init clickable objects in start func.
     */
    private void InitClickableObjs()
    {
        if (clickableObjs == null)
        {
            clickableObjs = new List<ClickableObject>();
        }
        clickableObjs.Clear();

        foreach (var item in FindObjectsOfType<ClickableObject>())
        {
            clickableObjs.Add(item);
            if (item.colorFollowParent)
            {
                item.highlightColor = highlightColor;
                item.normalColor = normalColor;
                item.hideColor = hideColor;
            }
        }
    }

    private void HitObject(ClickableObject clickableObject, bool isLongTouch)
    {
        // clicked object current state.
        bool isNormal = clickableObject.state == ClickableObject.ClickableObjectState.NORMAL;
        if (isNormal)
        {
            // virtual man point to item & introduce.FixMe
           // manCtrl.PointingAtAndIntroduce(clickableObject.GetCenterPosInWorldSpace(), clickableObject.objIntroduction);
        }

        if (isLongTouch)
        {
            if (!isNormal)
            {
                // long touch in HIGHLIGHT will turn to CLOSEUP.
                clickableObject.switchMode(ClickableObject.ClickableObjectState.CLOSEUP);
            }
            // longtouch in NORMAL will be ignored.
            return;
        }

        foreach (var item in clickableObjs)
        {
            // default NORMAL. HIGHLIGHT/CLOSEUP/INVISIBLE to NORMAL
            ClickableObject.ClickableObjectState newState = ClickableObject.ClickableObjectState.NORMAL;
            if (item == clickableObject && isNormal)
            {
                // NORMAL to HIGHLIGHT
                newState = ClickableObject.ClickableObjectState.HIGHTLIGHT;
                
            } else if (isNormal)
            {
                // NORMAL to INVISIBLE
                newState = ClickableObject.ClickableObjectState.INVISIBLE;
            }
            item.switchMode(newState);
        }
    }

    public void TestObjectLongClick()
    {
        HitObject(clickableObjs[1], true);
    }

    public void TestObjectClick()
    {
        HitObject(clickableObjs[1], false);
    }

    public void SetObjectsVisibility()
    {
        ClickableObject.ClickableObjectState newState = ClickableObject.ClickableObjectState.NORMAL;
        if (isAllInvisible)
        {
            newState = ClickableObject.ClickableObjectState.INVISIBLE;
        }
        
        foreach (var item in clickableObjs)
        {
            item.switchMode(newState);
        }
        isAllInvisible = !isAllInvisible;
    }
}
