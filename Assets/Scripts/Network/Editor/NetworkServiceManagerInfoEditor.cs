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
                EditorGUILayout.HelpBox("运行时信息仅在游戏运行时可用", MessageType.Info);
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
            
            EditorGUILayout.Space(10);
            
            if (GUILayout.Button("测试连接"))
            {
                manager.Get("api/health", null, callback: (result, response) =>
                {
                    if (response.success)
                    {
                        Debug.Log("✅ 服务器连接正常");
                    }
                    else
                    {
                        Debug.LogError("❌ 服务器连接失败");
                    }
                });
            }
        }
    }
}

#endif