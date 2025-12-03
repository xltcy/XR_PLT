#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Game.Network.Editor
{
    [CustomEditor(typeof(NetworkServiceSystem))]
    public class NetworkServiceSystemEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            
            NetworkServiceSystem system = (NetworkServiceSystem)target;
            
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("运行时信息", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                EditorGUILayout.LabelField($"活跃请求数: {system.ActiveRequests}");
                EditorGUILayout.LabelField($"正在请求: {system.IsRequesting}");
                EditorGUILayout.LabelField($"基础URL: {system.BaseUrl}");
            }
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space(10);
            
            if (GUILayout.Button("测试连接"))
            {
                system.Get("api/health", null, callback: (result, response) =>
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