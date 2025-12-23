using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(DebugSwitch))]
public class DebugSwitchEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // 保留原有字段显示
        DrawDefaultInspector();

        GUILayout.Space(6);
        if (GUILayout.Button("切换图片显示"))
        {
            var ds = (DebugSwitch)target;
            ds.ToggleImgDisplay();
            // 可选：让编辑器知道对象已改变
            EditorUtility.SetDirty(ds);
        }
    }
}