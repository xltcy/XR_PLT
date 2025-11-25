using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(RocketIssueController))]
public class RocketIssueControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        RocketIssueController controller = (RocketIssueController)target;
        if (GUILayout.Button("一阶段发射"))
        {
            controller.Issue_Stage1();
        }
        if (GUILayout.Button("重置一阶段发射"))
        {
            controller.Reset_Issue_Stage1();
        }
    }
}