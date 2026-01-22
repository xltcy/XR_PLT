using System;
using TickSystem;
using UnityEngine;

public class RotatingComponent : MonoBehaviour, ITickerUpdate
{
    [Header("旋转角度/s")]
    public float rotationSpeed = 200f; // 加载旋转速度
    
    private void OnEnable()
    {
        TickController.RegisterTick(this);
    }

    private void OnDisable()
    {
        TickController.UnRegisterTick(this);
    }

    /// <summary>
    /// 继承了接口ITickerUpdate，所以是随Update调用
    /// </summary>
    public void Tick()
    {
        transform.Rotate(0, 0, -rotationSpeed * Time.deltaTime);
    }
}
