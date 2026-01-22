#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Network.Editor
{
    [CustomEditor(typeof(NetworkServiceManagerInfo))]
    public class NetworkServiceManagerInfoEditor : UnityEditor.Editor
    {
        private NetworkServiceManagerInfo info;

        private void OnEnable()
        {
            info = (NetworkServiceManagerInfo)target;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            
            if (!Application.isPlaying)
            {
                EditorGUILayout.Space(10);
                EditorGUILayout.HelpBox("运行时信息仅在程序运行时可用", MessageType.Info);
                return;
            }
            
            var manager = info?.Manager;
            
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("运行时信息", EditorStyles.boldLabel);
            
            if (manager == null)
            {
                EditorGUILayout.HelpBox("Manager 未初始化，请在运行时查看", MessageType.Warning);
                return;
            }
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                EditorGUILayout.LabelField($"活跃请求数: {manager.ActiveRequests}");
                EditorGUILayout.LabelField($"正在请求: {manager.IsRequesting}");
                EditorGUILayout.LabelField($"基础URL: {manager.BaseUrl}");
            }
            EditorGUILayout.EndVertical();
            
            // Lockable 信息
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("锁定与加载圈信息", EditorStyles.boldLabel);
            
            var activeLockables = manager.ActiveLockables;
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                EditorGUILayout.LabelField($"活跃锁定数: {activeLockables.Count}");
                EditorGUILayout.LabelField($"全屏锁定请求数: {manager.FullScreenLockCount}");
                EditorGUILayout.LabelField($"局部锁定数: {manager.LocalLockCount}");
                
                if (activeLockables.Count > 0)
                {
                    EditorGUILayout.Space(5);
                    EditorGUILayout.LabelField("详细信息:", EditorStyles.boldLabel);
                    
                    foreach (var kvp in activeLockables)
                    {
                        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                        {
                            if (kvp.Key == null)
                            {
                                EditorGUILayout.LabelField($"全屏锁定", EditorStyles.boldLabel);
                                EditorGUILayout.LabelField($"  请求数: {kvp.Value}");
                                EditorGUILayout.HelpBox("使用全屏加载圈", MessageType.Info);
                            }
                            else
                            {
                                EditorGUILayout.LabelField($"局部锁定: {kvp.Key.name}", EditorStyles.boldLabel);
                                EditorGUILayout.ObjectField("  对象", kvp.Key, typeof(Transform), true);
                                EditorGUILayout.LabelField($"  请求数: {kvp.Value}");
                                EditorGUILayout.HelpBox($"在 {kvp.Key.name} 上显示加载圈", MessageType.Info);
                            }
                        }
                        EditorGUILayout.EndVertical();
                        EditorGUILayout.Space(3);
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("当前没有活跃的锁定", MessageType.Info);
                }
            }
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space(10);
        }
    }
}

#endif