using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(ExperimentManager), true)]
public class ExperimentManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        ExperimentManager exam = (ExperimentManager)target;
        //if(GUILayout.Button("Reset View"))
        //{
        //    exam.ResetView();
        //}

        if(GUILayout.Button("Start CCT"))
        {
            exam.startCCT();
        }

        if(GUILayout.Button("Next Stage"))
        {
            exam.nextStage();
        }
        
    }

}
