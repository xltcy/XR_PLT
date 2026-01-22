using UnityEngine;
using TickSystem;

/// <summary>
/// 示例1：MonoBehaviour类使用每帧Tick
/// </summary>
public class TickExampleMono : MonoBehaviour, ITickerUpdate
{
    private float timer = 0f;

    void Start()
    {
        TickController.RegisterTick(this);
        Debug.Log("MonoBehaviour示例已注册到Tick系统");
    }

    void OnDestroy()
    {
        TickController.UnRegisterTick(this);
    }

    public void Tick()
    {
        timer += Time.deltaTime;
        if (timer >= 1f)
        {
            Debug.Log($"[每帧Tick] 时间: {Time.time:F2}");
            timer = 0f;
        }
    }
}

/// <summary>
/// 示例2：使用每秒Tick
/// </summary>
public class TickExampleSecond : MonoBehaviour, ITickerSecond
{
    void Start()
    {
        TickController.RegisterTick(this);
        Debug.Log("每秒Tick示例已注册");
    }

    void OnDestroy()
    {
        TickController.UnRegisterTick(this);
    }

    public void TickSecond()
    {
        Debug.Log($"[每秒Tick] 时间: {Time.time:F2}");
    }
}

/// <summary>
/// 示例3：使用每0.5秒Tick
/// </summary>
public class TickExampleHalfSecond : MonoBehaviour, ITickerHalfSecond
{
    void Start()
    {
        TickController.RegisterTick(this);
        Debug.Log("每0.5秒Tick示例已注册");
    }

    void OnDestroy()
    {
        TickController.UnRegisterTick(this);
    }

    public void TickHalfSecond()
    {
        Debug.Log($"[每0.5秒Tick] 时间: {Time.time:F2}");
    }
}

/// <summary>
/// 示例4：使用自定义间隔Tick（2秒）
/// </summary>
public class TickExampleCustomInterval : MonoBehaviour, ITickerInterval
{
    [SerializeField] private float intervalTime = 2f;

    void Start()
    {
        TickController.RegisterTick(this);
        Debug.Log($"自定义间隔Tick示例已注册 (间隔: {intervalTime}秒)");
    }

    void OnDestroy()
    {
        TickController.UnRegisterTick(this);
    }

    public void TickInterval()
    {
        Debug.Log($"[自定义间隔Tick {intervalTime}秒] 时间: {Time.time:F2}");
    }

    public float TickIntervalTime => intervalTime;
}

/// <summary>
/// 示例5：使用FixedUpdate Tick（物理相关）
/// </summary>
public class TickExampleFixed : MonoBehaviour, ITickerFixedUpdate
{
    private int count = 0;

    void Start()
    {
        TickController.RegisterTick(this);
        Debug.Log("FixedUpdate Tick示例已注册");
    }

    void OnDestroy()
    {
        TickController.UnRegisterTick(this);
    }

    public void TickFixed()
    {
        count++;
        if (count % 50 == 0)
        {
            Debug.Log($"[FixedUpdate Tick] 次数: {count}");
        }
    }
}

/// <summary>
/// 示例6：使用LateUpdate Tick（相机跟随等）
/// </summary>
public class TickExampleLate : MonoBehaviour, ITickerLateUpdate
{
    void Start()
    {
        TickController.RegisterTick(this);
        Debug.Log("LateUpdate Tick示例已注册");
    }

    void OnDestroy()
    {
        TickController.UnRegisterTick(this);
    }

    public void TickLate()
    {
        // 在所有Update之后执行，适合相机跟随
    }
}

/// <summary>
/// 示例7：同时实现多个Tick接口
/// </summary>
public class TickExampleMulti : MonoBehaviour, ITickerUpdate, ITickerSecond
{
    void Start()
    {
        TickController.RegisterTick(this);  // 自动识别并注册所有接口
        Debug.Log("多接口Tick示例已注册");
    }

    void OnDestroy()
    {
        TickController.UnRegisterTick(this);  // 自动取消所有接口
    }

    public void Tick()
    {
        // 每帧执行
    }

    public void TickSecond()
    {
        Debug.Log("[多接口] 每秒Tick");
    }
}

/// <summary>
/// 示例8：纯C#类使用多个Tick接口
/// </summary>
public class TickExamplePureCS : ITickerSecond, ITickerInterval
{
    private string name;
    private float customInterval;

    public TickExamplePureCS(string name, float customInterval = 3f)
    {
        this.name = name;
        this.customInterval = customInterval;
        TickController.RegisterTick(this);  // 自动注册所有实现的接口
        Debug.Log($"纯C#类 '{name}' 已注册 (间隔: {customInterval}秒)");
    }

    public void TickSecond()
    {
        Debug.Log($"[纯C#类 {name}] 每秒Tick");
    }

    public void TickInterval()
    {
        Debug.Log($"[纯C#类 {name}] 自定义间隔Tick ({customInterval}秒)");
    }

    public float TickIntervalTime => customInterval;

    public void Dispose()
    {
        TickController.UnRegisterTick(this);
        Debug.Log($"纯C#类 '{name}' 已注销");
    }
}

/// <summary>
/// 示例9：继承其他类的同时使用Tick
/// </summary>
public class TickBaseController : MonoBehaviour
{
    public virtual void DoSomething()
    {
        Debug.Log("基类功能");
    }
}

public class TickExampleDerived : TickBaseController, ITickerSecond
{
    void Start()
    {
        TickController.RegisterTick(this);
        Debug.Log("派生类每秒Tick示例已注册");
    }

    void OnDestroy()
    {
        TickController.UnRegisterTick(this);
    }

    public void TickSecond()
    {
        Debug.Log("[派生类] 每秒Tick，同时可以调用基类方法");
        DoSomething();
    }
}

/// <summary>
/// 示例10：测试管理器 - 演示完整功能
/// </summary>
public class TickSystemDemo : MonoBehaviour
{
    private TickExamplePureCS pureCS1;
    private TickExamplePureCS pureCS2;

    void Start()
    {
        // 创建纯C#对象实例
        pureCS1 = new TickExamplePureCS("测试对象1", 2f);
        pureCS2 = new TickExamplePureCS("测试对象2", 5f);

        // 输出统计信息
        Debug.Log(TickController.GetStatistics());
        
        // 5秒后输出统计
        Invoke(nameof(ShowStats), 5f);
    }

    void ShowStats()
    {
        Debug.Log("5秒后的统计信息:");
        Debug.Log(TickController.GetStatistics());
    }

    void OnDestroy()
    {
        pureCS1?.Dispose();
        pureCS2?.Dispose();
    }
}
