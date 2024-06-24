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
    shoulder,
    fingers
}

public class ExperimentManager : MonoBehaviour
{

    IEnumerator Definitions()
    {
        filePath = @"C:\Users\s4659771\Documents\VRXPerceptStudy\DATA";

        // CCT DEFINITIONS -----------------------------------------------------------------------

        // WARNING: nTrials (below) needs to be a multiple of 4
        nTrials = 60; // [= 60] Total number of trials, congruant + incongruent  

        // WARNING: nTrials_noGo (below) needs to be a multiple of 2  
        nTrials_noGo = 8; // [= 8] Number of no go trials

        // The variable nTrials_targetOnly, defines the total number of trials in which no visual 
        // distractor will happen (only vibration). They are necessary in order to accustom the 
        // participants to the vibrotactile elevation discrimination task. The number of 
        // nTrials_targetOnly = 10 was defined based on previous studies.
        // Importantly, this is only added to "pre" tests, either H2S or H2H.
        nTrials_targetOnly = 10; // [= 10]  

        // The variable nTrials_familiarization, defines the total number of trials (congruant + 
        // incongruent) that will preceed the actual test. The number of 
        // nTrials_familiarization = 20 was defined based on previous studies.
        // Importantly, this is only added to "pre" tests, either H2S or H2H.
        nTrials_familiarization = 20; // [= 20]

        // The variable nTrials_noGoFamiliarization, defines the total number of trials (congruant + 
        // incongruent) that will be noGo, during familiarization phase. The number of 
        // nTrials_noGoFamiliarization = 2 was defined based.
        // Importantly, this is only added to "pre" tests, either H2S or H2H.
        nTrials_noGoFamiliarization = 2; // [= 2]
        // ---------------------------------------------------------------------------------------

        // PRACTICE DEFINITIONS ------------------------------------------------------------------
        // The variable nRepetitions_primaryPrac define the number of repetitions that will be done
        // during the primary practice (main practice, that is the first one).
        nRepetitions_primaryPrac = 50;

        // The variable nnRepetitions_refresherPrac define the number of repetitions that will be
        // done during the refresher practice (second practice, following the first post CCT).
        nRepetitions_refresherPrac = 5;

        // The variable nnRepetitions_debriefPrac define the number of repetitions that will be
        // done during the debriefing practice (last practice, following the last CCT).
        nRepetitions_debriefPrac = 5;
        // ---------------------------------------------------------------------------------------
        
        yield return null;
    }

    [SerializeField]
    public string ArduinoPort = "";

    [Header("Experiment Configuration")]
    // Choose the type of control (finger tracking or shoulder movement), which is related to the participant experimental group
    public CtrlMode ControlMode = CtrlMode.None;
    // Collect participants ID and save data accordingly
    public string ID = "";

    public float TableHeight = 0.0f;
    
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

    // The variable nTrials_noGoFamiliarization, defines the total number of trials (congruant + 
    // incongruent) that will be noGo, during familiarization phase. The number of 
    // nTrials_noGoFamiliarization = 2 was defined based.
    // Importantly, this is only added to "pre" tests, either H2S or H2H.
    [ReadOnly]
    public int nTrials_noGoFamiliarization; 
    // ---------------------------------------------------------------------------------------

    // PRACTICE DEFINITIONS ------------------------------------------------------------------
    // The variable nRepetitions_primaryPrac define the number of repetitions that will be done
    // during the primary practice (main practice, that is the first one).
    [ReadOnly]
    public int nRepetitions_primaryPrac;

    // The variable nRepetitions_refresherPrac define the number of repetitions that will be
    // done during the refresher practice (second practice, following the first post CCT).
    [ReadOnly]
    public int nRepetitions_refresherPrac;

    // The variable nRepetitions_debriefPrac define the number of repetitions that will be
    // done during the debriefing practice (last practice, following the last CCT).
    [ReadOnly]
    public int nRepetitions_debriefPrac;
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

    [HideInInspector]
    public Vector3 calibratedPos;
    [HideInInspector]
    public Vector3 calibratedRot;
    [HideInInspector]
    public bool startCCTflag = false;

    // Other variables
    private int currentStageIndex = 0;
    private GameObject currentStage;
    private GameObject table;
    private Transform head;
    private Transform origin;
    private Transform target;
    private PinchControl pinchControl;
    private ScreenController screenController;
    private GameObject userHand;

    void Start()
    {
        head = GameObject.Find("Rig/Camera Offset/Main Camera").transform;
        origin = GameObject.Find("Rig").transform;
        target = GameObject.Find("Scene/Recenter Position").transform;

        table = GameObject.Find("Scene/Table");
        if(TableHeight!=0.0f)
        {
            Vector3 newPosition = table.transform.position;
            newPosition.y = TableHeight;
            table.transform.position = newPosition;
        }
        else
        {
            Debug.Log("Need to specify table height.");
            Application.Quit();
        }
        StartCoroutine(Initialization());

        screenController = GameObject.Find("Scene/Screen").GetComponent<ScreenController>();
        screenController.SetText(
        "Hi there!\n\n" +
        "Thank you for participating!"
        ,5);
    }

    IEnumerator Initialization()
    {
        yield return StartCoroutine(Definitions());
    
        yield return StartCoroutine(WaitToRecenter());

        yield return StartCoroutine(WaitToCalibrate());
    }
    IEnumerator WaitToRecenter()
    {
        pinchControl = GameObject.Find("Rig/Camera Offset/RightHand").GetComponent<PinchControl>();
        while (!pinchControl.delsysReady){
            yield return null;
        }
        yield return new WaitForSeconds(3f);

        Recenter();
    }

    IEnumerator WaitToCalibrate()
    {
        GameObject.Find("Rig/Camera Offset/RightHand").SetActive(false);
        userHand = GameObject.Find("Rig/Camera Offset/RightHand_CCT");
        userHand.SetActive(true);
        
        while (!Input.GetKeyDown("space")){
            yield return null;
        }

        getHandPos();
    }

    public void getHandPos()
    {
        // Get the rotation Euler angles of the GameObject
        calibratedRot = userHand.transform.rotation.eulerAngles;

        // Get the position of the GameObject
        calibratedPos = userHand.transform.position;

        // Output the rotation Euler angles and position
        Debug.Log("Calibration: OK");
        //Debug.Log(calibratedPos);
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

        Debug.Log("Recentre: OK");
    }
    
    public void startCCT()
    {
        startCCTflag = true;
    }
    
    //void Update()
    //{
    //    if (Input.GetKeyDown("space"))
    //    {
    //        Recenter(); 
    //    }
    //}

    public void nextStage()
    {
        LoadNextStage();
    }

    public void previousStage()
    {
        LoadPreviousStage();
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

            // Update any stage-specific variables or setup
            // Example: Clearing previous stage data
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
            
            screenController.SetText(
                "All done!\n\n" +
                "Thank you again for participating!"
                ,5);
        }
    }

    
    void LoadPreviousStage()
    {
        if (currentStageIndex > 0)
        {
            currentStageIndex--;

            // Destroy the current stage if it exists
            if (currentStage != null)
            {
                Destroy(currentStage);
            }

            // Instantiate the previous stage
            currentStage = Instantiate(stages[currentStageIndex], transform);

            // Update any stage-specific variables or setup
            // Example: Clearing previous stage data
            TargetOnlyTrials = "";
            FamiliarizationTrials = "";
            CCTTrials = "";
            NoGoTrials = "";
            Repetition = "";

            // Show status
            CurrentStage = currentStage.name.Replace("(Clone)", "").Trim();
        }
        else
        {
            Debug.Log("Already at the first stage.");
            // Optionally handle what happens when trying to go back from the first stage
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