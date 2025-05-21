using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainThreadDispatcher : MonoBehaviour
{
    internal static MainThreadDispatcher Instance;

    private Queue<Action> queue = new Queue<Action>(8);

    private volatile bool queued = false;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        if (Instance == null)
        {
            Instance = new GameObject("Dispatcher").AddComponent<MainThreadDispatcher>();
            DontDestroyOnLoad(Instance.gameObject);
        }
    }

    // Update is called once per frame
    void Update()
    {
        while (queued)
        {
            Action action = null;
            lock (queue)
            {
                action = queue.Dequeue();
                if (queue.Count == 0) queued = false;
            }

            action();
        }
    }

    public static void InvokeOnMainThread(Action action)
    {
        if (action is null) throw new ArgumentNullException(nameof(action));
        lock (Instance.queue)
        {
            Instance.queue.Enqueue(action);
            Instance.queued = true;
        }
    }
}
