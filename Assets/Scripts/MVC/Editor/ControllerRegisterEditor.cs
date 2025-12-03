using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ControllerRegister))]
public class ControllerRegisterEditor : Editor
{
    private SerializedProperty controllersProp;
    
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawPropertiesExcluding(serializedObject, "controllers");
        
        GUI.enabled = false;
        DrawPropertiesExcluding(serializedObject, "logInitialization", "warnInitialization", "errorInitialization", "m_Script");
        GUI.enabled = true;
        
        serializedObject.ApplyModifiedProperties();
    }
}