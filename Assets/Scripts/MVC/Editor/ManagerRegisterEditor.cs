using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ManagerRegister))]
public class ManagerRegisterEditor : Editor
{
    private SerializedProperty managersProp;
    
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawPropertiesExcluding(serializedObject, "managers");
        
        GUI.enabled = false;
        DrawPropertiesExcluding(serializedObject, "logInitialization", "warnInitialization", "errorInitialization", "m_Script");
        GUI.enabled = true;
        
        serializedObject.ApplyModifiedProperties();
    }
}