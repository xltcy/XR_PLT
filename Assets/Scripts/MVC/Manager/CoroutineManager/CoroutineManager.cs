using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 协程管理器，允许纯C#类调用Unity协程
/// </summary>
public class CoroutineManager : BaseManager
{
    #region 字段与属性
    private CoroutineRunner runner;
    private Dictionary<string, Coroutine> namedCoroutines = new Dictionary<string, Coroutine>();
    private Dictionary<object, List<Coroutine>> ownerCoroutines = new Dictionary<object, List<Coroutine>>();
    
    // 无owner的协程使用这个默认owner
    private static readonly object DefaultOwner = new object();

    // 高优先级初始化
    public override int InitPriority => 0;
    
    // 启动时初始化
    public override ManagerRegister.InitTiming InitTiming => ManagerRegister.InitTiming.OnSceneLoaded;

    #endregion

    #region 生命周期
    public override void OnRegister()
    {
        base.OnRegister();

        // 创建协程运行器GameObject
        GameObject runnerObj = new GameObject("CoroutineRunner");
        runnerObj.transform.parent = GameObject.Find("DontDestroyOnLoad").transform;
        runner = runnerObj.AddComponent<CoroutineRunner>();
    }

    public override void OnUnregister()
    {
        StopAllManagedCoroutines();
        
        if (runner != null)
        {
            GameObject.Destroy(runner.gameObject);
            runner = null;
        }

        base.OnUnregister();
    }
    #endregion 生命周期

    #region 协程管理

    /// <summary>
    /// 启动一个协程（可指定所有者，销毁时可自动停止）
    /// </summary>
    public Coroutine StartManagedCoroutine(IEnumerator routine, object owner = null)
    {
        if (runner == null)
        {
            Debug.LogError("[CoroutineManager] CoroutineRunner未初始化");
            return null;
        }

        if (owner == null)
        {
            owner = DefaultOwner;
        }

        Coroutine coroutine = runner.StartCoroutine(routine);

        // 跟踪所有者的协程
        if (!ownerCoroutines.ContainsKey(owner))
        {
            ownerCoroutines[owner] = new List<Coroutine>();
        }
        ownerCoroutines[owner].Add(coroutine);

        return coroutine;
    }

    /// <summary>
    /// 启动一个带名称的协程（同名协程会被停止，可指定所有者）
    /// </summary>
    public Coroutine StartManagedCoroutine(string name, IEnumerator routine, object owner = null)
    {
        if (runner == null)
        {
            Debug.LogError("[CoroutineManager] CoroutineRunner未初始化");
            return null;
        }

        if (owner == null)
        {
            owner = DefaultOwner;
        }

        // 如果已存在同名协程，先停止
        if (namedCoroutines.TryGetValue(name, out Coroutine existingCoroutine))
        {
            runner.StopCoroutine(existingCoroutine);
            RemoveCoroutineFromOwners(existingCoroutine);
            namedCoroutines.Remove(name);
        }

        Coroutine coroutine = runner.StartCoroutine(routine);
        namedCoroutines[name] = coroutine;

        // 跟踪所有者的协程
        if (!ownerCoroutines.ContainsKey(owner))
        {
            ownerCoroutines[owner] = new List<Coroutine>();
        }
        ownerCoroutines[owner].Add(coroutine);

        return coroutine;
    }

    /// <summary>
    /// 停止一个协程
    /// </summary>
    public void StopManagedCoroutine(Coroutine coroutine)
    {
        if (runner != null && coroutine != null)
        {
            runner.StopCoroutine(coroutine);
            RemoveCoroutineFromOwners(coroutine);
        }
    }

    /// <summary>
    /// 停止指定所有者的所有协程
    /// </summary>
    public void StopManagedCoroutines(object owner)
    {
        if (owner == null || runner == null)
            return;

        if (ownerCoroutines.TryGetValue(owner, out List<Coroutine> coroutines))
        {
            foreach (var coroutine in coroutines)
            {
                if (coroutine != null)
                {
                    runner.StopCoroutine(coroutine);
                }
            }
            ownerCoroutines.Remove(owner);
        }
    }

    /// <summary>
    /// 停止一个协程（通过IEnumerator）
    /// 注意：只能停止用同一个IEnumerator对象启动的协程
    /// </summary>
    public void StopManagedCoroutine(IEnumerator routine)
    {
        if (runner != null && routine != null)
        {
            runner.StopCoroutine(routine);
            // 注：无法通过IEnumerator获取Coroutine对象，因此无法从跟踪中移除
            // 建议使用StopManagedCoroutine(Coroutine)或StopManagedCoroutines(owner)
        }
    }

    /// <summary>
    /// 通过名称停止协程
    /// </summary>
    public void StopManagedCoroutine(string name)
    {
        if (namedCoroutines.TryGetValue(name, out Coroutine coroutine))
        {
            if (runner != null)
            {
                runner.StopCoroutine(coroutine);
            }
            RemoveCoroutineFromOwners(coroutine);
            namedCoroutines.Remove(name);
        }
    }

    /// <summary>
    /// 停止所有协程
    /// </summary>
    private void StopAllManagedCoroutines()
    {
        if (runner != null)
        {
            runner.StopAllCoroutines();
        }
        namedCoroutines.Clear();
        ownerCoroutines.Clear();
    }

    /// <summary>
    /// 从所有者跟踪中移除协程
    /// </summary>
    private void RemoveCoroutineFromOwners(Coroutine coroutine)
    {
        foreach (var kvp in ownerCoroutines)
        {
            if (kvp.Value.Remove(coroutine))
            {
                if (kvp.Value.Count == 0)
                {
                    ownerCoroutines.Remove(kvp.Key);
                }
                break;
            }
        }
    }
    #endregion 协程管理

    // ========== 具体使用看这里 ==========
    // 使用StartManagedCoroutine启动协程
    // 也可以使用下面的便捷方法
    #region 便捷协程方法
    /// <summary>
    /// 延迟执行（秒，可指定所有者）
    /// </summary>
    public Coroutine DelayedCall(float delay, Action callback, object owner = null)
    {
        return StartManagedCoroutine(DelayedCallCoroutine(delay, callback), owner);
    }

    /// <summary>
    /// 延迟执行（帧，可指定所有者）
    /// </summary>
    public Coroutine DelayedCallFrames(int frames, Action callback, object owner = null)
    {
        return StartManagedCoroutine(DelayedCallFramesCoroutine(frames, callback), owner);
    }

    /// <summary>
    /// 等待条件满足后执行（可指定所有者）
    /// </summary>
    public Coroutine WaitUntil(Func<bool> condition, Action callback, object owner = null)
    {
        return StartManagedCoroutine(WaitUntilCoroutine(condition, callback), owner);
    }

    /// <summary>
    /// 等待条件不满足后执行（可指定所有者）
    /// </summary>
    public Coroutine WaitWhile(Func<bool> condition, Action callback, object owner = null)
    {
        return StartManagedCoroutine(WaitWhileCoroutine(condition, callback), owner);
    }

    /// <summary>
    /// 重复执行（秒间隔，可指定所有者）
    /// </summary>
    public Coroutine RepeatCall(float interval, Action callback, int count = -1, object owner = null)
    {
        return StartManagedCoroutine(RepeatCallCoroutine(interval, callback, count), owner);
    }

    /// <summary>
    /// 下一帧执行（可指定所有者）
    /// </summary>
    public Coroutine NextFrame(Action callback, object owner = null)
    {
        return DelayedCallFrames(1, callback, owner);
    }

    /// <summary>
    /// 等待帧结束执行（可指定所有者）
    /// </summary>
    public Coroutine EndOfFrame(Action callback, object owner = null)
    {
        return StartManagedCoroutine(EndOfFrameCoroutine(callback), owner);
    }
    #endregion 便捷协程方法

    // ========== 内部协程方法 ==========
    #region 内部协程方法

    private IEnumerator DelayedCallCoroutine(float delay, Action callback)
    {
        yield return new WaitForSeconds(delay);
        callback?.Invoke();
    }

    private IEnumerator DelayedCallFramesCoroutine(int frames, Action callback)
    {
        for (int i = 0; i < frames; i++)
        {
            yield return null;
        }
        callback?.Invoke();
    }

    private IEnumerator WaitUntilCoroutine(Func<bool> condition, Action callback)
    {
        yield return new WaitUntil(condition);
        callback?.Invoke();
    }

    private IEnumerator WaitWhileCoroutine(Func<bool> condition, Action callback)
    {
        yield return new WaitWhile(condition);
        callback?.Invoke();
    }

    private IEnumerator RepeatCallCoroutine(float interval, Action callback, int count)
    {
        int executed = 0;
        while (count < 0 || executed < count)
        {
            callback?.Invoke();
            executed++;
            yield return new WaitForSeconds(interval);
        }
    }

    private IEnumerator EndOfFrameCoroutine(Action callback)
    {
        yield return new WaitForEndOfFrame();
        callback?.Invoke();
    }

    /// <summary>
    /// 内部协程运行器（MonoBehaviour）
    /// </summary>
    private class CoroutineRunner : MonoBehaviour
    {
        // MonoBehaviour的协程功能会被外部使用
    }
    #endregion 内部协程方法
}