using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// GameObject池管理器 - 负责Prefab的加载、实例化和回收
/// </summary>
public class GameObjectPoolManager : BaseManager
{
    // 预制体缓存
    private Dictionary<string, GameObject> _prefabCache = new Dictionary<string, GameObject>();
    
    // 实例池 - 用于复用
    private Dictionary<string, Queue<GameObject>> _instancePools = new Dictionary<string, Queue<GameObject>>();
    
    // 活跃实例跟踪
    private Dictionary<GameObject, string> _activeInstances = new Dictionary<GameObject, string>();
    
    private readonly object _cacheLock = new object();
    
    // 日志开关
    private bool enableLog = false;    
    // 对象池根节点
    private Transform _poolRoot;
    #region 初始化

    public override void OnRegister()
    {
        base.OnRegister();
        
        // 创建对象池根节点
        GameObject poolRootObj = new GameObject("[GameObjectPoolManager Pool]");
        _poolRoot = poolRootObj.transform;
        UnityEngine.Object.DontDestroyOnLoad(poolRootObj);
        
        if (enableLog) Debug.Log("[GameObjectPoolManager] 初始化完成");
    }

    public override void OnUnregister()
    {
        base.OnUnregister();
        
        // 清理所有活跃实例
        ClearAllInstances();
        
        // 清理对象池
        ClearAllPools();
        
        // 清理预制体缓存
        _prefabCache.Clear();
        
        // 销毁对象池根节点
        if (_poolRoot != null)
        {
            UnityEngine.Object.Destroy(_poolRoot.gameObject);
            _poolRoot = null;
        }
        
        if (enableLog) Debug.Log("[GameObjectPoolManager] 资源已清理");
    }

    #endregion

    #region 同步加载

    /// <summary>
    /// 同步加载预制体，只加载，不实例化
    /// </summary>
    /// <param name="path">Resources 路径</param>
    /// <returns>预制体，失败返回 null</returns>
    public GameObject LoadPrefab(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            Debug.LogError("[GameObjectPoolManager] 加载路径为空");
            return null;
        }

        lock (_cacheLock)
        {
            // 检查缓存
            if (_prefabCache.TryGetValue(path, out GameObject cachedPrefab))
            {
                return cachedPrefab;
            }

            // 从 Resources 加载
            GameObject prefab = Resources.Load<GameObject>(path);
            if (prefab == null)
            {
                Debug.LogError($"[GameObjectPoolManager] 无法加载预制体: {path}");
                return null;
            }

            // 缓存预制体
            _prefabCache[path] = prefab;
            if (enableLog) Debug.Log($"[GameObjectPoolManager] 预制体已加载并缓存: {path}");
            
            return prefab;
        }
    }

    /// <summary>
    /// 同步加载并且实例化对象（使用对象池优化）
    /// </summary>
    /// <param name="path">Resources 路径</param>
    /// <param name="parent">父物体</param>
    /// <param name="usePool">是否使用对象池</param>
    /// <returns>实例化的对象，失败返回 null</returns>
    public GameObject Instantiate(string path, Transform parent = null, bool usePool = true)
    {
        GameObject prefab = LoadPrefab(path);
        if (prefab == null)
        {
            return null;
        }

        GameObject instance = null;
        
        // 尝试从对象池获取
        if (usePool && TryGetFromPool(path, out instance))
        {
            instance.transform.SetParent(parent);
            instance.SetActive(true);
            if (enableLog) Debug.Log($"[GameObjectPoolManager] 从对象池获取实例: {path}");
        }
        else
        {
            // 创建新实例
            instance = UnityEngine.Object.Instantiate(prefab, parent);
            if (enableLog) Debug.Log($"[GameObjectPoolManager] 创建新实例: {path}");
        }

        // 记录活跃实例
        _activeInstances[instance] = path;
        
        return instance;
    }

    /// <summary>
    /// 回收对象（放入对象池或销毁）
    /// </summary>
    /// <param name="instance">要回收的对象</param>
    /// <param name="usePool">是否放入对象池</param>
    public void Recycle(GameObject instance, bool usePool = true)
    {
        if (instance == null)
        {
            return;
        }

        // 获取资源路径
        if (!_activeInstances.TryGetValue(instance, out string path))
        {
            // 不是通过 GameObjectPoolManager 创建的对象，直接销毁
            UnityEngine.Object.Destroy(instance);
            Debug.LogWarning($"[GameObjectPoolManager] 销毁未跟踪的对象: {instance.name}");
            return;
        }

        // 从活跃实例中移除
        _activeInstances.Remove(instance);

        if (usePool)
        {
            // 放入对象池
            ReturnToPool(path, instance);
            if (enableLog) Debug.Log($"[GameObjectPoolManager] 对象已回收到池: {path}");
        }
        else
        {
            // 直接销毁
            UnityEngine.Object.Destroy(instance);
            if (enableLog) Debug.Log($"[GameObjectPoolManager] 对象已销毁: {path}");
        }
    }

    #endregion

    #region 异步加载

    /// <summary>
    /// 异步加载预制体，只加载，不实例化
    /// </summary>
    /// <param name="path">Resources 路径</param>
    /// <param name="onLoaded">加载完成回调</param>
    public void LoadPrefabAsync(string path, Action<GameObject> onLoaded)
    {
        if (string.IsNullOrEmpty(path))
        {
            Debug.LogError("[GameObjectPoolManager] 加载路径为空");
            onLoaded?.Invoke(null);
            return;
        }

        // 检查缓存
        lock (_cacheLock)
        {
            if (_prefabCache.TryGetValue(path, out GameObject cachedPrefab))
            {
                onLoaded?.Invoke(cachedPrefab);
                return;
            }
        }

        // 异步加载
        ManagerRefer.CoroutineManager.StartManagedCoroutine(LoadPrefabAsyncCoroutine(path, onLoaded), this);
    }

    private IEnumerator LoadPrefabAsyncCoroutine(string path, Action<GameObject> onLoaded)
    {
        ResourceRequest request = Resources.LoadAsync<GameObject>(path);
        yield return request;

        GameObject prefab = request.asset as GameObject;
        
        if (prefab == null)
        {
            Debug.LogError($"[GameObjectPoolManager] 异步加载预制体失败: {path}");
            onLoaded?.Invoke(null);
            yield break;
        }

        lock (_cacheLock)
        {
            // 缓存预制体
            if (!_prefabCache.ContainsKey(path))
            {
                _prefabCache[path] = prefab;
                if (enableLog) Debug.Log($"[GameObjectPoolManager] 预制体异步加载并缓存: {path}");
            }
        }

        onLoaded?.Invoke(prefab);
    }

    /// <summary>
    /// 异步实例化对象
    /// </summary>
    /// <param name="path">Resources 路径</param>
    /// <param name="parent">父物体</param>
    /// <param name="onInstantiated">实例化完成回调</param>
    /// <param name="usePool">是否使用对象池</param>
    public void InstantiateAsync(string path, Transform parent, Action<GameObject> onInstantiated, bool usePool = true)
    {
        LoadPrefabAsync(path, prefab =>
        {
            if (prefab == null)
            {
                onInstantiated?.Invoke(null);
                return;
            }

            GameObject instance = null;

            // 尝试从对象池获取
            if (usePool && TryGetFromPool(path, out instance))
            {
                instance.transform.SetParent(parent);
                instance.SetActive(true);
                if (enableLog) Debug.Log($"[GameObjectPoolManager] 从对象池获取实例: {path}");
            }
            else
            {
                // 创建新实例
                instance = UnityEngine.Object.Instantiate(prefab, parent);
                if (enableLog) Debug.Log($"[GameObjectPoolManager] 异步创建新实例: {path}");
            }

            // 记录活跃实例
            _activeInstances[instance] = path;

            onInstantiated?.Invoke(instance);
        });
    }

    #endregion

    #region 对象池管理

    private bool TryGetFromPool(string path, out GameObject instance)
    {
        lock (_cacheLock)
        {
            if (_instancePools.TryGetValue(path, out Queue<GameObject> pool) && pool.Count > 0)
            {
                instance = pool.Dequeue();
                
                // 确保对象仍然有效
                if (instance != null)
                {
                    return true;
                }
            }
        }

        instance = null;
        return false;
    }

    private void ReturnToPool(string path, GameObject instance)
    {
        if (instance == null)
        {
            return;
        }

        // 重置对象状态
        instance.SetActive(false);
        instance.transform.SetParent(_poolRoot);

        lock (_cacheLock)
        {
            if (!_instancePools.ContainsKey(path))
            {
                _instancePools[path] = new Queue<GameObject>();
            }

            _instancePools[path].Enqueue(instance);
        }
    }

    /// <summary>
    /// 清理指定路径的对象池
    /// </summary>
    /// <param name="path">资源路径</param>
    public void ClearPool(string path)
    {
        lock (_cacheLock)
        {
            if (_instancePools.TryGetValue(path, out Queue<GameObject> pool))
            {
                while (pool.Count > 0)
                {
                    GameObject instance = pool.Dequeue();
                    if (instance != null)
                    {
                        UnityEngine.Object.Destroy(instance);
                    }
                }
                _instancePools.Remove(path);
                if (enableLog) Debug.Log($"[GameObjectPoolManager] 已清理对象池: {path}");
            }
        }
    }

    /// <summary>
    /// 清理所有对象池
    /// </summary>
    public void ClearAllPools()
    {
        lock (_cacheLock)
        {
            foreach (var pool in _instancePools.Values)
            {
                while (pool.Count > 0)
                {
                    GameObject instance = pool.Dequeue();
                    if (instance != null)
                    {
                        UnityEngine.Object.Destroy(instance);
                    }
                }
            }
            _instancePools.Clear();
            if (enableLog) Debug.Log("[GameObjectPoolManager] 已清理所有对象池");
        }
    }

    #endregion

    #region 实例管理

    /// <summary>
    /// 清理所有活跃实例
    /// </summary>
    public void ClearAllInstances()
    {
        foreach (var instance in _activeInstances.Keys)
        {
            if (instance != null)
            {
                UnityEngine.Object.Destroy(instance);
            }
        }
        _activeInstances.Clear();
        if (enableLog) Debug.Log("[GameObjectPoolManager] 已清理所有活跃实例");
    }

    /// <summary>
    /// 获取活跃实例数量
    /// </summary>
    public int ActiveInstanceCount => _activeInstances.Count;

    /// <summary>
    /// 获取指定路径的池中对象数量
    /// </summary>
    public int GetPoolCount(string path)
    {
        lock (_cacheLock)
        {
            if (_instancePools.TryGetValue(path, out Queue<GameObject> pool))
            {
                return pool.Count;
            }
            return 0;
        }
    }

    #endregion
}
