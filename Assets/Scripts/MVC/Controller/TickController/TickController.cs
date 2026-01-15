using System;
using System.Collections.Generic;
using TickSystem;
using UnityEngine;

/// <summary>
/// Tick管理器 - 自动管理所有Tickable对象的Tick调用
/// 支持多种Tick类型：每帧、每秒、自定义间隔、FixedUpdate、LateUpdate
/// </summary>
public class TickController : BaseController
{
    // 内部类：用于跟踪间隔Tick的时间
    private class IntervalTickWrapper
    {
        public ITickerInterval Ticker;
        public float Timer;
    }

    private class SecondTickWrapper
    {
        public ITickerSecond Ticker;
        public float Timer;
    }

    private class HalfSecondTickWrapper
    {
        public ITickerHalfSecond Ticker;
        public float Timer;
    }

    // 存储不同类型的Tickable对象
    private List<ITickerUpdate> tickables = new List<ITickerUpdate>();
    private List<SecondTickWrapper> tickablesSecond = new List<SecondTickWrapper>();
    private List<HalfSecondTickWrapper> tickablesHalfSecond = new List<HalfSecondTickWrapper>();
    private List<IntervalTickWrapper> tickablesInterval = new List<IntervalTickWrapper>();
    private List<ITickerFixedUpdate> tickablesFixed = new List<ITickerFixedUpdate>();
    private List<ITickerLateUpdate> tickablesLate = new List<ITickerLateUpdate>();

    // 延迟添加/移除列表
    private List<ITickerUpdate> toAddTickable = new List<ITickerUpdate>();
    private List<ITickerUpdate> toRemoveTickable = new List<ITickerUpdate>();
    private List<ITickerSecond> toAddSecond = new List<ITickerSecond>();
    private List<ITickerSecond> toRemoveSecond = new List<ITickerSecond>();
    private List<ITickerHalfSecond> toAddHalfSecond = new List<ITickerHalfSecond>();
    private List<ITickerHalfSecond> toRemoveHalfSecond = new List<ITickerHalfSecond>();
    private List<ITickerInterval> toAddInterval = new List<ITickerInterval>();
    private List<ITickerInterval> toRemoveInterval = new List<ITickerInterval>();
    private List<ITickerFixedUpdate> toAddFixed = new List<ITickerFixedUpdate>();
    private List<ITickerFixedUpdate> toRemoveFixed = new List<ITickerFixedUpdate>();
    private List<ITickerLateUpdate> toAddLate = new List<ITickerLateUpdate>();
    private List<ITickerLateUpdate> toRemoveLate = new List<ITickerLateUpdate>();

    private bool isUpdating = false;
    private bool isFixedUpdating = false;
    private bool isLateUpdating = false;
    
    private bool needsSortTickable = false;
    private bool needsSortSecond = false;
    private bool needsSortHalfSecond = false;
    private bool needsSortInterval = false;
    private bool needsSortFixed = false;
    private bool needsSortLate = false;

    #region Helper Methods
    
    /// <summary>
    /// 获取对象的IsTickEnabled状态（支持可选接口）
    /// </summary>
    private static bool GetIsTickEnabled(object obj)
    {
        if (obj is ITickerEnabled enabled)
            return enabled.IsTickEnabled;
        
        // 默认启用
        return true;
    }
    
    /// <summary>
    /// 获取对象的TickPriority（支持可选接口）
    /// </summary>
    private static int GetTickPriority(object obj)
    {
        if (obj is ITickerPriority priority)
            return priority.TickPriority;
        
        // 默认优先级为0
        return 0;
    }
    
    #endregion

    /// <summary>
    /// 注册对象（自动识别实现的接口类型）
    /// </summary>
    public static void Register(object obj)
    {
        if (obj == null)
        {
            Debug.LogWarning("尝试注册null对象到TickManager");
            return;
        }

        var instance = ControllerRefer.TickController;

        // 检查并注册所有实现的接口
        if (obj is ITickerUpdate tickable)
            instance.RegisterTickable(tickable);
        
        if (obj is ITickerSecond tickableSecond)
            instance.RegisterSecond(tickableSecond);
        
        if (obj is ITickerHalfSecond tickableHalfSecond)
            instance.RegisterHalfSecond(tickableHalfSecond);
        
        if (obj is ITickerInterval tickableInterval)
            instance.RegisterInterval(tickableInterval);
        
        if (obj is ITickerFixedUpdate tickableFixed)
            instance.RegisterFixed(tickableFixed);
        
        if (obj is ITickerLateUpdate tickableLate)
            instance.RegisterLate(tickableLate);
    }

    /// <summary>
    /// 取消注册对象（自动识别实现的接口类型）
    /// </summary>
    public static void Unregister(object obj)
    {
        if (obj == null)
            return;
        
        var instance = ControllerRefer.TickController;
        
        // 检查并取消注册所有实现的接口
        if (obj is ITickerUpdate tickable)
            instance.UnregisterTickable(tickable);
        
        if (obj is ITickerSecond tickableSecond)
            instance.UnregisterSecond(tickableSecond);
        
        if (obj is ITickerHalfSecond tickableHalfSecond)
            instance.UnregisterHalfSecond(tickableHalfSecond);
        
        if (obj is ITickerInterval tickableInterval)
            instance.UnregisterInterval(tickableInterval);
        
        if (obj is ITickerFixedUpdate tickableFixed)
            instance.UnregisterFixed(tickableFixed);
        
        if (obj is ITickerLateUpdate tickableLate)
            instance.UnregisterLate(tickableLate);
    }

    #region Register/Unregister Internal Methods
    
    private void RegisterTickable(ITickerUpdate tickerUpdate)
    {
        if (isUpdating)
        {
            if (!toAddTickable.Contains(tickerUpdate) && !tickables.Contains(tickerUpdate))
                toAddTickable.Add(tickerUpdate);
        }
        else
        {
            if (!tickables.Contains(tickerUpdate))
            {
                tickables.Add(tickerUpdate);
                needsSortTickable = true;
            }
        }
    }

    private void UnregisterTickable(ITickerUpdate tickerUpdate)
    {
        if (isUpdating)
        {
            if (!toRemoveTickable.Contains(tickerUpdate))
                toRemoveTickable.Add(tickerUpdate);
            toAddTickable.Remove(tickerUpdate);
        }
        else
        {
            tickables.Remove(tickerUpdate);
        }
    }

    private void RegisterSecond(ITickerSecond ticker)
    {
        if (isUpdating)
        {
            if (!toAddSecond.Contains(ticker) && !tickablesSecond.Exists(w => w.Ticker == ticker))
                toAddSecond.Add(ticker);
        }
        else
        {
            if (!tickablesSecond.Exists(w => w.Ticker == ticker))
            {
                tickablesSecond.Add(new SecondTickWrapper { Ticker = ticker, Timer = 0f });
                needsSortSecond = true;
            }
        }
    }

    private void UnregisterSecond(ITickerSecond ticker)
    {
        if (isUpdating)
        {
            if (!toRemoveSecond.Contains(ticker))
                toRemoveSecond.Add(ticker);
            toAddSecond.Remove(ticker);
        }
        else
        {
            tickablesSecond.RemoveAll(w => w.Ticker == ticker);
        }
    }

    private void RegisterHalfSecond(ITickerHalfSecond ticker)
    {
        if (isUpdating)
        {
            if (!toAddHalfSecond.Contains(ticker) && !tickablesHalfSecond.Exists(w => w.Ticker == ticker))
                toAddHalfSecond.Add(ticker);
        }
        else
        {
            if (!tickablesHalfSecond.Exists(w => w.Ticker == ticker))
            {
                tickablesHalfSecond.Add(new HalfSecondTickWrapper { Ticker = ticker, Timer = 0f });
                needsSortHalfSecond = true;
            }
        }
    }

    private void UnregisterHalfSecond(ITickerHalfSecond ticker)
    {
        if (isUpdating)
        {
            if (!toRemoveHalfSecond.Contains(ticker))
                toRemoveHalfSecond.Add(ticker);
            toAddHalfSecond.Remove(ticker);
        }
        else
        {
            tickablesHalfSecond.RemoveAll(w => w.Ticker == ticker);
        }
    }

    private void RegisterInterval(ITickerInterval ticker)
    {
        if (isUpdating)
        {
            if (!toAddInterval.Contains(ticker) && !tickablesInterval.Exists(w => w.Ticker == ticker))
                toAddInterval.Add(ticker);
        }
        else
        {
            if (!tickablesInterval.Exists(w => w.Ticker == ticker))
            {
                tickablesInterval.Add(new IntervalTickWrapper { Ticker = ticker, Timer = 0f });
                needsSortInterval = true;
            }
        }
    }

    private void UnregisterInterval(ITickerInterval ticker)
    {
        if (isUpdating)
        {
            if (!toRemoveInterval.Contains(ticker))
                toRemoveInterval.Add(ticker);
            toAddInterval.Remove(ticker);
        }
        else
        {
            tickablesInterval.RemoveAll(w => w.Ticker == ticker);
        }
    }

    private void RegisterFixed(ITickerFixedUpdate ticker)
    {
        if (isFixedUpdating)
        {
            if (!toAddFixed.Contains(ticker) && !tickablesFixed.Contains(ticker))
                toAddFixed.Add(ticker);
        }
        else
        {
            if (!tickablesFixed.Contains(ticker))
            {
                tickablesFixed.Add(ticker);
                needsSortFixed = true;
            }
        }
    }

    private void UnregisterFixed(ITickerFixedUpdate ticker)
    {
        if (isFixedUpdating)
        {
            if (!toRemoveFixed.Contains(ticker))
                toRemoveFixed.Add(ticker);
            toAddFixed.Remove(ticker);
        }
        else
        {
            tickablesFixed.Remove(ticker);
        }
    }

    private void RegisterLate(ITickerLateUpdate ticker)
    {
        if (isLateUpdating)
        {
            if (!toAddLate.Contains(ticker) && !tickablesLate.Contains(ticker))
                toAddLate.Add(ticker);
        }
        else
        {
            if (!tickablesLate.Contains(ticker))
            {
                tickablesLate.Add(ticker);
                needsSortLate = true;
            }
        }
    }

    private void UnregisterLate(ITickerLateUpdate ticker)
    {
        if (isLateUpdating)
        {
            if (!toRemoveLate.Contains(ticker))
                toRemoveLate.Add(ticker);
            toAddLate.Remove(ticker);
        }
        else
        {
            tickablesLate.Remove(ticker);
        }
    }
    
    #endregion

    private void Update()
    {
        // 处理每帧Tick
        ProcessTickables();
        
        // 处理间隔Tick
        ProcessIntervalTicks();
    }

    private void FixedUpdate()
    {
        ProcessFixedUpdate();
    }

    private void LateUpdate()
    {
        ProcessLateUpdate();
    }

    #region Process Methods

    private void ProcessTickables()
    {
        if (needsSortTickable)
        {
            tickables.Sort((a, b) => GetTickPriority(a).CompareTo(GetTickPriority(b)));
            needsSortTickable = false;
        }

        isUpdating = true;

        for (int i = 0; i < tickables.Count; i++)
        {
            var tickable = tickables[i];
            
            if (tickable is UnityEngine.Object obj && obj == null)
            {
                toRemoveTickable.Add(tickable);
                continue;
            }

            if (GetIsTickEnabled(tickable))
            {
                try
                {
                    tickable.Tick();
                }
                catch (Exception e)
                {
                    Debug.LogError($"Tick异常 in {tickable.GetType().Name}: {e}");
                }
            }
        }

        isUpdating = false;
        ApplyPendingChanges(tickables, toAddTickable, toRemoveTickable, ref needsSortTickable);
    }

    private void ProcessIntervalTicks()
    {
        float deltaTime = Time.deltaTime;
        
        // 处理每秒Tick
        ProcessSecondTick(deltaTime);
        
        // 处理每0.5秒Tick
        ProcessHalfSecondTick(deltaTime);
        
        // 处理自定义间隔Tick
        ProcessCustomIntervalTick(deltaTime);
    }

    private void ProcessSecondTick(float deltaTime)
    {
        if (needsSortSecond)
        {
            tickablesSecond.Sort((a, b) => GetTickPriority(a.Ticker).CompareTo(GetTickPriority(b.Ticker)));
            needsSortSecond = false;
        }

        for (int i = tickablesSecond.Count - 1; i >= 0; i--)
        {
            var wrapper = tickablesSecond[i];
            
            if (wrapper.Ticker is UnityEngine.Object obj && obj == null)
            {
                toRemoveSecond.Add(wrapper.Ticker);
                continue;
            }

            if (!GetIsTickEnabled(wrapper.Ticker))
                continue;

            wrapper.Timer += deltaTime;
            if (wrapper.Timer >= 1f)
            {
                wrapper.Timer -= 1f;
                try
                {
                    wrapper.Ticker.TickSecond();
                }
                catch (Exception e)
                {
                    Debug.LogError($"TickSecond异常 in {wrapper.Ticker.GetType().Name}: {e}");
                }
            }
        }

        if (toRemoveSecond.Count > 0 || toAddSecond.Count > 0)
        {
            foreach (var t in toRemoveSecond)
                tickablesSecond.RemoveAll(w => w.Ticker == t);
            toRemoveSecond.Clear();

            foreach (var t in toAddSecond)
                tickablesSecond.Add(new SecondTickWrapper { Ticker = t, Timer = 0f });
            toAddSecond.Clear();
            needsSortSecond = true;
        }
    }

    private void ProcessHalfSecondTick(float deltaTime)
    {
        if (needsSortHalfSecond)
        {
            tickablesHalfSecond.Sort((a, b) => GetTickPriority(a.Ticker).CompareTo(GetTickPriority(b.Ticker)));
            needsSortHalfSecond = false;
        }

        for (int i = tickablesHalfSecond.Count - 1; i >= 0; i--)
        {
            var wrapper = tickablesHalfSecond[i];
            
            if (wrapper.Ticker is UnityEngine.Object obj && obj == null)
            {
                toRemoveHalfSecond.Add(wrapper.Ticker);
                continue;
            }

            if (!GetIsTickEnabled(wrapper.Ticker))
                continue;

            wrapper.Timer += deltaTime;
            if (wrapper.Timer >= 0.5f)
            {
                wrapper.Timer -= 0.5f;
                try
                {
                    wrapper.Ticker.TickHalfSecond();
                }
                catch (Exception e)
                {
                    Debug.LogError($"TickHalfSecond异常 in {wrapper.Ticker.GetType().Name}: {e}");
                }
            }
        }

        if (toRemoveHalfSecond.Count > 0 || toAddHalfSecond.Count > 0)
        {
            foreach (var t in toRemoveHalfSecond)
                tickablesHalfSecond.RemoveAll(w => w.Ticker == t);
            toRemoveHalfSecond.Clear();

            foreach (var t in toAddHalfSecond)
                tickablesHalfSecond.Add(new HalfSecondTickWrapper { Ticker = t, Timer = 0f });
            toAddHalfSecond.Clear();
            needsSortHalfSecond = true;
        }
    }

    private void ProcessCustomIntervalTick(float deltaTime)
    {
        if (needsSortInterval)
        {
            tickablesInterval.Sort((a, b) => GetTickPriority(a.Ticker).CompareTo(GetTickPriority(b.Ticker)));
            needsSortInterval = false;
        }

        for (int i = tickablesInterval.Count - 1; i >= 0; i--)
        {
            var wrapper = tickablesInterval[i];
            
            if (wrapper.Ticker is UnityEngine.Object obj && obj == null)
            {
                toRemoveInterval.Add(wrapper.Ticker);
                continue;
            }

            if (!GetIsTickEnabled(wrapper.Ticker))
                continue;

            float interval = wrapper.Ticker.TickIntervalTime;
            if (interval <= 0)
                interval = 1f;

            wrapper.Timer += deltaTime;
            if (wrapper.Timer >= interval)
            {
                wrapper.Timer -= interval;
                try
                {
                    wrapper.Ticker.TickInterval();
                }
                catch (Exception e)
                {
                    Debug.LogError($"TickInterval异常 in {wrapper.Ticker.GetType().Name}: {e}");
                }
            }
        }

        if (toRemoveInterval.Count > 0 || toAddInterval.Count > 0)
        {
            foreach (var t in toRemoveInterval)
                tickablesInterval.RemoveAll(w => w.Ticker == t);
            toRemoveInterval.Clear();

            foreach (var t in toAddInterval)
                tickablesInterval.Add(new IntervalTickWrapper { Ticker = t, Timer = 0f });
            toAddInterval.Clear();
            needsSortInterval = true;
        }
    }

    private void ProcessFixedUpdate()
    {
        if (needsSortFixed)
        {
            tickablesFixed.Sort((a, b) => GetTickPriority(a).CompareTo(GetTickPriority(b)));
            needsSortFixed = false;
        }

        isFixedUpdating = true;

        for (int i = 0; i < tickablesFixed.Count; i++)
        {
            var tickable = tickablesFixed[i];
            
            if (tickable is UnityEngine.Object obj && obj == null)
            {
                toRemoveFixed.Add(tickable);
                continue;
            }

            if (GetIsTickEnabled(tickable))
            {
                try
                {
                    tickable.TickFixed();
                }
                catch (Exception e)
                {
                    Debug.LogError($"TickFixed异常 in {tickable.GetType().Name}: {e}");
                }
            }
        }

        isFixedUpdating = false;
        ApplyPendingChanges(tickablesFixed, toAddFixed, toRemoveFixed, ref needsSortFixed);
    }

    private void ProcessLateUpdate()
    {
        if (needsSortLate)
        {
            tickablesLate.Sort((a, b) => GetTickPriority(a).CompareTo(GetTickPriority(b)));
            needsSortLate = false;
        }

        isLateUpdating = true;

        for (int i = 0; i < tickablesLate.Count; i++)
        {
            var tickable = tickablesLate[i];
            
            if (tickable is UnityEngine.Object obj && obj == null)
            {
                toRemoveLate.Add(tickable);
                continue;
            }

            if (GetIsTickEnabled(tickable))
            {
                try
                {
                    tickable.TickLate();
                }
                catch (Exception e)
                {
                    Debug.LogError($"TickLate异常 in {tickable.GetType().Name}: {e}");
                }
            }
        }

        isLateUpdating = false;
        ApplyPendingChanges(tickablesLate, toAddLate, toRemoveLate, ref needsSortLate);
    }

    private void ApplyPendingChanges<T>(List<T> list, List<T> toAdd, List<T> toRemove, ref bool needsSort)
    {
        if (toRemove.Count > 0)
        {
            foreach (var item in toRemove)
                list.Remove(item);
            toRemove.Clear();
        }

        if (toAdd.Count > 0)
        {
            list.AddRange(toAdd);
            toAdd.Clear();
            needsSort = true;
        }
    }

    #endregion

    /// <summary>
    /// 获取当前注册的Tickable对象总数
    /// </summary>
    public static int GetTickableCount()
    {
        var instance = ControllerRefer.TickController;
        
        if (instance == null) return 0;
        
        return instance.tickables.Count + 
               instance.tickablesSecond.Count + 
               instance.tickablesHalfSecond.Count + 
               instance.tickablesInterval.Count +
               instance.tickablesFixed.Count +
               instance.tickablesLate.Count;
    }

    /// <summary>
    /// 获取各类型Tick的统计信息
    /// </summary>
    public static string GetStatistics()
    {
        var instance = ControllerRefer.TickController;
        
        if (instance == null) return "TickManager未初始化";

        return $"Tick统计:\n" +
               $"  每帧: {instance.tickables.Count}\n" +
               $"  每秒: {instance.tickablesSecond.Count}\n" +
               $"  每0.5秒: {instance.tickablesHalfSecond.Count}\n" +
               $"  自定义间隔: {instance.tickablesInterval.Count}\n" +
               $"  FixedUpdate: {instance.tickablesFixed.Count}\n" +
               $"  LateUpdate: {instance.tickablesLate.Count}\n" +
               $"  总计: {GetTickableCount()}";
    }
}
