using UnityEngine;

/// <summary>
/// 挂载此脚本的GameObject将自动设置为DontDestroyOnLoad
/// </summary>
public class PersistentObject : MonoBehaviour
{
    [Tooltip("是否在Awake时自动设置为DontDestroyOnLoad")]
    [SerializeField] private bool autoSetOnAwake = true;

    private void Awake()
    {
        if (autoSetOnAwake)
        {
            DontDestroyOnLoad(gameObject);
        }
    }

    /// <summary>
    /// 手动设置为DontDestroyOnLoad
    /// </summary>
    public void SetPersistent()
    {
        DontDestroyOnLoad(gameObject);
    }
}
