# Tick系统 - 快速参考

## 所有接口一览

```csharp
// 每帧执行
public class MyClass : ITickable
{
    public void Tick() { }
}

// 每秒执行
public class MyClass : ITickableSecond
{
    public void TickSecond() { }
}

// 每0.5秒执行
public class MyClass : ITickableHalfSecond
{
    public void TickHalfSecond() { }
}

// 自定义间隔执行
public class MyClass : ITickableInterval
{
    public void TickInterval() { }
    public float TickIntervalTime => 3f;  // 3秒间隔
}

// 在FixedUpdate执行（物理）
public class MyClass : ITickableFixedUpdate
{
    public void TickFixed() { }
}

// 在LateUpdate执行（相机）
public class MyClass : ITickableLateUpdate
{
    public void TickLate() { }
}
```

## 多接口同时使用

```csharp
// 一个类可以同时实现多个接口
public class MyClass : MonoBehaviour, ITickable, ITickableSecond, ITickableInterval
{
    void Start() => TickController.Register(this);  // 自动注册所有接口
    void OnDestroy() => TickController.Unregister(this);  // 自动注销所有接口

    public void Tick() { /* 每帧 */ }
    public void TickSecond() { /* 每秒 */ }
    public void TickInterval() { /* 自定义间隔 */ }
    public float TickIntervalTime => 5f;
}
```

## 常用代码模板

### MonoBehaviour模板
```csharp
using UnityEngine;
using TickSystem;

public class MyComponent : MonoBehaviour, ITickableSecond
{
    void Start() => TickController.Register(this);
    void OnDestroy() => TickController.Unregister(this);
    
    public void TickSecond()
    {
        // 你的代码
    }
}
```

### 纯C#类模板
```csharp
using TickSystem;

public class MyManager : ITickableSecond
{
    public MyManager()
    {
        TickController.Register(this);
    }
    
    public void TickSecond()
    {
        // 你的代码
    }
    
    public void Dispose()
    {
        TickController.Unregister(this);
    }
}
```

## 选择合适的Tick类型

| 需求 | 推荐接口 |
|------|----------|
| 移动、输入检测 | `ITickable` (每帧) |
| 倒计时、定时检测 | `ITickableSecond` (每秒) |
| 技能冷却、AI决策 | `ITickableInterval` (自定义) |
| 物理计算 | `ITickableFixedUpdate` |
| 相机跟随 | `ITickableLateUpdate` |

## 可选属性

```csharp
// 控制是否执行
public bool IsTickEnabled => true;  // 默认启用

// 控制执行顺序（数值越小越先执行）
public int TickPriority => 0;  // 默认优先级
```

## 实用工具

```csharp
// 获取统计信息
Debug.Log(TickController.GetStatistics());

// 获取总数
int count = TickController.GetTickableCount();
```
