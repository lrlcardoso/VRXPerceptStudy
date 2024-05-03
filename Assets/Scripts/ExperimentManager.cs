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

    void Start()
    {
        
    }

    void Update()
    {
        
    }
}
