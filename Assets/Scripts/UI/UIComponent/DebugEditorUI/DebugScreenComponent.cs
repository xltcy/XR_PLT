using System;
using UnityEngine;
using UnityEngine.UI;

public class DebugScreenComponent : BaseStateComponent
{
    [BindChild("屏幕大小调节")]
    private Slider 屏幕大小调节;
    
    [BindChild("放置屏幕")]
    private GameObject screen;
    
    private Vector3 screenScale;

    private void Start()
    {
        screenScale = screen ? screen.transform.localScale : new Vector3(1, 1, 1);
    }

    private void OnEnable()
    {
        屏幕大小调节?.AddValueChangeListener(屏幕Resize);
    }

    private void OnDisable()
    {
        屏幕大小调节?.RemoveAllValueChangeListeners();
    }
    
    void 屏幕Resize(float value)
    {
        screen.transform.localScale = screenScale * value;
    }
}