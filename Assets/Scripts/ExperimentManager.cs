// Developed by: Lucas Cardoso
// First version: 03/May/2024
// Latest release: 03/May/2024
// Description: 

using System;
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

    void Definitions()
    {
        filePath = @"C:\Users\s4659771\Documents\VRXPerceptStudy\DATA";

        // CCT DEFINITIONS -----------------------------------------------------------------------

        // WARNING: nTrials (below) needs to be a multiple of 4
        //[ReadOnly]
        nTrials = 4; // Total number of trials, congruant + incongruent  

        // WARNING: nTrials_noGo (below) needs to be a multiple of 2  
        //[ReadOnly]
        nTrials_noGo = 2; // Number of no go trials

        // The variable nTrials_targetOnly, defines the total number of trials in which no visual 
        // distractor will happen (only vibration). They are necessary in order to accustom the 
        // participants to the vibrotactile elevation discrimination task. The number of 
        // nTrials_targetOnly = 10 was defined based on previous studies.
        // Importantly, this is only added to "pre" tests, either H2S or H2H.
        //[ReadOnly]
        nTrials_targetOnly = 4;  

        // The variable nTrials_familiarization, defines the total number of trials (congruant + 
        // incongruent) that will preceed the actual test. The number of 
        // nTrials_familiarization = 20 was defined based on previous studies.
        // Importantly, this is only added to "pre" tests, either H2S or H2H.
        //[ReadOnly]
        nTrials_familiarization = 4; 
        // ---------------------------------------------------------------------------------------

        // PRACTICE DEFINITIONS ------------------------------------------------------------------
        // The variable nRepetitions_primaryPrac define the number of repetitions that will be done
        // during the primary practice (main practice, that is the first one).
        //[ReadOnly]
        nRepetitions_primaryPrac = 4;

        // The variable nnRepetitions_refresherPrac define the number of repetitions that will be
        // done during the refresher practice (second practice, following the first post CCT).
        //[ReadOnly]
        nRepetitions_refresherPrac = 1;
        // ---------------------------------------------------------------------------------------
    }

    [SerializeField]
    public string ArduinoPort = "";

    [Header("Experiment Configuration")]
    // Choose the type of control (finger tracking or shoulder movement), which is related to the participant experimental group
    public CtrlMode ControlMode = CtrlMode.None;
    // Collect participants ID and save data accordingly
    public string ID = "";
    
    [ReadOnly]
    // Define the location in which all the files will be saved
    public string filePath;

    // CCT DEFINITIONS -----------------------------------------------------------------------

    // WARNING: nTrials (below) needs to be a multiple of 4
    [ReadOnly]
    public int nTrials; // Total number of trials, congruant + incongruent  

    // WARNING: nTrials_noGo (below) needs to be a multiple of 2  
    [ReadOnly]
    public int nTrials_noGo; // Number of no go trials

    // The variable nTrials_targetOnly, defines the total number of trials in which no visual 
    // distractor will happen (only vibration). They are necessary in order to accustom the 
    // participants to the vibrotactile elevation discrimination task. The number of 
    // nTrials_targetOnly = 10 was defined based on previous studies.
    // Importantly, this is only added to "pre" tests, either H2S or H2H.
    [ReadOnly]
    public int nTrials_targetOnly;  

    // The variable nTrials_familiarization, defines the total number of trials (congruant + 
    // incongruent) that will preceed the actual test. The number of 
    // nTrials_familiarization = 20 was defined based on previous studies.
    // Importantly, this is only added to "pre" tests, either H2S or H2H.
    [ReadOnly]
    public int nTrials_familiarization; 
    // ---------------------------------------------------------------------------------------

    // PRACTICE DEFINITIONS ------------------------------------------------------------------
    // The variable nRepetitions_primaryPrac define the number of repetitions that will be done
    // during the primary practice (main practice, that is the first one).
    [ReadOnly]
    public int nRepetitions_primaryPrac;

    // The variable nnRepetitions_refresherPrac define the number of repetitions that will be
    // done during the refresher practice (second practice, following the first post CCT).
    [ReadOnly]
    public int nRepetitions_refresherPrac;
    // ---------------------------------------------------------------------------------------

    public List<GameObject> stages;  // List of stage prefabs
    
    [ReadOnly]
    public string CurrentStage;

    [Header("CCT Status")]
    [Tooltip("")]
    [ReadOnly]
    public string TargetOnlyTrials;
    [ReadOnly]
    public string FamiliarizationTrials;
    [ReadOnly]
    public string CCTTrials;
    [ReadOnly]
    public string NoGoTrials;

    [Header("Practice Status")]
    [Tooltip("")]
    [ReadOnly]
    public string Repetition;

    // Other variables
    private int currentStageIndex = 0;
    private GameObject currentStage;
    private Transform head;
    private Transform origin;
    private Transform target;

    void Start()
    {
        head = GameObject.Find("Rig/Camera Offset/Main Camera").transform;
        origin = GameObject.Find("Rig").transform;
        target = GameObject.Find("Scene/Recenter Position").transform;

        Definitions();
    
        LoadNextStage();
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
    
    void Update()
    {
        if (Input.GetKeyDown("space"))
        {
            Recenter(); 
            //Debug.Log(userHand.transform.position);
            //Debug.Log(userHand.transform.rotation.eulerAngles);
        }
    }

    public void nextStage()
    {
        LoadNextStage();
    }

    void LoadNextStage()
    {
        if (currentStageIndex < stages.Count)
        {
            // Destroy the current stage if it exists
            if (currentStage != null)
            {
                Destroy(currentStage);
            }

            // Instantiate the next stage
            currentStage = Instantiate(stages[currentStageIndex], transform);
            TargetOnlyTrials = "";
            FamiliarizationTrials = "";
            CCTTrials = "";
            NoGoTrials = "";
            Repetition = "";

            // Show status
            CurrentStage = currentStage.name.Replace("(Clone)", "").Trim();

            currentStageIndex++;
        }
        else
        {
            Debug.Log("All stages completed.");
        }
    }

    // Method to get the component from the current stage
    public T GetCurrentStageComponent<T>() where T : Component
    {
        if (currentStage != null)
        {
            return currentStage.GetComponent<T>();
        }
        return null;
    }
}