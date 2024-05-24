// Developed by: Lucas Cardoso
// First version: 03/May/2024
// Latest release: 03/May/2024
// Description: 

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum CtrlMode
{
    None,
    Shoulder,
    Fingers
}

public class ExperimentManager : MonoBehaviour
{

    [Header("Experiment Configuration")]
    // Choose the type of control (finger tracking or shoulder movement), which is related to the participant experimental group
    public CtrlMode ControlMode = CtrlMode.None;
    // Collect participants ID and save data accordingly
    public string ID = "";

    private Transform head;
    private Transform origin;
    private Transform target;

    [Header("CCT Status")]
    [Tooltip("")]
    public string CongruentTrials;
    public string IncongruentTrials;
    public string NoGoTrials;

    void Start()
    {
        head = GameObject.Find("Rig/Camera Offset/Main Camera").transform;
        origin = GameObject.Find("Rig").transform;
        target = GameObject.Find("Scene/Recenter Position").transform;
    }

    void Update()
    {

        if (Input.GetKeyDown("space"))
        {
            Recenter(); 
        }
        
    }

    public void Recenter()
    {
        Vector3 offset = head.position - origin.position;
        offset.y = 0;
        origin.position = target.position - offset;

        Vector3 targetForward = target.forward;
        targetForward.y = 0;
        Vector3 cameraForward = head.forward;
        cameraForward.y = 0;

        float angle = Vector3.SignedAngle(cameraForward,targetForward, Vector3.up);
        origin.RotateAround(head.position, Vector3.up, angle);
    }
    
    public void ResetView()
    {
        Recenter();
    }

    public void startTest()
    {
        //StartCCT = true;
    }
}
