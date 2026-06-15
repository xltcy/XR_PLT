using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(DebugSwitch))]
public class DebugSwitchEditor : Editor
{
    private SpeechManager.SpeechSynthesisMode selectedSpeechMode = SpeechManager.SynthesisMode;

    public override void OnInspectorGUI()
    {
        // 保留原有字段显示
        DrawDefaultInspector();

        DrawSpeechSynthesisModeMenu();

        GUILayout.Space(6);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("切换图片显示"))
        {
            var ds = (DebugSwitch)target;
            ds.ToggleImgDisplay();
            // 可选：让编辑器知道对象已改变
            EditorUtility.SetDirty(ds);
        }
        
        if (GUILayout.Button("切换图片层级"))
        {
            var ds = (DebugSwitch)target;
            ds.ToggleImgLayer();
            // 可选：让编辑器知道对象已改变
            EditorUtility.SetDirty(ds);
        }
        GUILayout.EndHorizontal();
    }

    private void DrawSpeechSynthesisModeMenu()
    {
        GUILayout.Space(8);
        EditorGUILayout.LabelField("语音合成调试", EditorStyles.boldLabel);

        selectedSpeechMode = SpeechManager.SynthesisMode;
        EditorGUI.BeginChangeCheck();
        selectedSpeechMode = (SpeechManager.SpeechSynthesisMode)EditorGUILayout.EnumPopup("语音模式", selectedSpeechMode);
        if (EditorGUI.EndChangeCheck())
        {
            SpeechManager.SetSynthesisMode(selectedSpeechMode);
            EditorUtility.SetDirty((DebugSwitch)target);
        }

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("语音模式切换主要用于运行时调试。", MessageType.Info);
        }
    }
}
