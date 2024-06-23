using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(ExperimentManager), true)]
public class ExperimentManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        ExperimentManager exam = (ExperimentManager)target;
        
        if(GUILayout.Button("Previous Stage"))
        {
            exam.previousStage();
        }

        if(GUILayout.Button("Next Stage"))
        {
            exam.nextStage();
        }

        if(GUILayout.Button("Start CCT"))
        {
            exam.startCCT();
        }
        
    }

}
