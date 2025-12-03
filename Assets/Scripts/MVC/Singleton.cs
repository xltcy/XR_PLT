using UnityEngine;

/// <summary>
/// 单例模式基类
/// </summary>
/// <typeparam name="T">继承该接口的类型</typeparam>
public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T _instance;
    private static readonly object _lock = new object();
    private static bool _applicationIsQuitting = false;

    public static T Instance
    {
        get
        {
            if (_applicationIsQuitting && _instance == null)
            {
                Debug.LogWarning("[Singleton] Instance '" + typeof(T) +
                    "' already destroyed on application quit." +
                    " Won't create again - returning null.");
                return null;
            }

            lock (_lock)
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<T>();

                    if (_instance == null)
                    {
                        GameObject singletonObject = new GameObject();
                        _instance = singletonObject.AddComponent<T>();
                        singletonObject.name = typeof(T).ToString() + " (Singleton)";

                        // 如果是持久化单例，标记为不销毁
                        if (singletonObject.GetComponent<PersistentSingleton>() != null)
                        {
                            DontDestroyOnLoad(singletonObject);
                        }
                    }
                }
                return _instance;
            }
        }
    }

    protected virtual void Awake()
    {
        if (_instance == null)
        {
            _instance = this as T;
        }
        else if (_instance != this)
        {
            Debug.LogWarning("[Singleton] Multiple instances of " + typeof(T) + " found! Destroying duplicate.");
            Destroy(gameObject);
        }
    }

    protected virtual void OnApplicationQuit()
    {
        _applicationIsQuitting = true;
    }

    protected virtual void OnDestroy()
    {
        _applicationIsQuitting = true;
    }
}

/// <summary>
/// 标记单例为持久化（跨场景不销毁）
/// </summary>
public class PersistentSingleton : MonoBehaviour { }