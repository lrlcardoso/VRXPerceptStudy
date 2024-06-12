using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.IO.Ports;

public enum testOptions
{
    None,
    pre,
    post
}

public enum testTypeOptions
{
    None,
    H2H,
    H2S
}

public class RunCCT : MonoBehaviour
{
    // Needs to be defined for each prefab and match with the name of the prefab
    public testOptions test = testOptions.None;
    public testTypeOptions testType = testTypeOptions.None;

    // Parameters for the CCT ----------------------------------------------------------
    // Both, activeFrames and inactiveFrames (below) are used to control the flickering frequency. 
    // It is counted in multiples of the refresh period (around 14ms, for 72Hz). So, if both are 
    // equal to 1, it means that they will alternate as "active, inactive, active, ...", thus,
    // around 36Hz.  
    int activeFrames = 1; 
    int inactiveFrames = 1;
    
    // The variable flickeringPeriodFrames controls the period that the visual distractor will  
    // flicker, again, in multiples of 14ms. For example, if flickeringPeriodFrames = 14, then
    // the flickering period is 14*14ms ~ 196ms.
    int flickeringPeriodFrames = 14;
    
    // The variables fixationTimeFramesMin and fixationTimeFramesMax will determine randomically, 
    // the variable fixationTimeFrames, that is the waiting time with the fixation mark. It is 
    // important to randomize to avoid the participant to learn the time and anticipate the 
    // reaction. It is, again, in number of frames. For example: 72*14ms ~ 1 second. 
    int fixationTimeFramesMin = 72;
    int fixationTimeFramesMax = 107;

    // The variable delayFrames controls the desirable delay between the visual distractor (that 
    // comes first), and the vibration (that comes after). For example, if delayFrames = 7, then
    // the delay is 7*14ms ~ 98ms.
    int delayFrames = 7;

    // The variable systemDelayFrames SHOULD NOT BE CHANGED. The systemDelayFrames = 3 was 
    // determined by experimental procedure (using the oscilloscope). This is necessary to ensure
    // that the motor will be activated always in the same frame as the visual distractor appears 
    // (assuming delayFrames = 0).
    int systemDelayFrames = 3;

    // The variable tooSlow is used to provide feedback to the user if the reaction time exceeds 
    // it. It is also defined in the Arduino firmware, but with a greater value. For example, if 
    // in the Arudino it is defined as tooSlow = 2 seconds, here it is defined as 1.5 seconds, 
    // meaning that the loop in the Arduino firmware will stop after 2 seconds, but "too slow" 
    // feedback will be shown for any reaction slower than 1.5 seconds.   
    int tooSlow = 1500000;
    //----------------------------------------------------------------------------------

    // GameObject that communicates with Arduino (separate thread)
    private HapticControl hapticControl;

    // GameObject to exchange info with the ExperimentManager
    private ExperimentManager experimentManager;

    // Get the position of the user's eyes, so the angle of the stopwatch can be adjusted to always face the participant
    private Transform userEyes;

    // GameObjects to load during the test
    private GameObject fixationMark;
    private GameObject handRefPos;
    private GameObject stopwatch;

    // User's hand position (wrist)
    private GameObject userHand; 

    private Animator handAnimator; 
     
    // Messages to communicate with Arduino
    private byte[] thumbShoulder_CCT = new byte[] { 0x33 };
    private byte[] indexShoulder_CCT = new byte[] { 0x34 };
    private byte[] thumbHand_CCT = new byte[] { 0x35 };
    private byte[] indexHand_CCT = new byte[] { 0x36 };
    
    // Other variables
    MonoBehaviour scriptToDisable;
    bool startRecordingFPS = false;
    int frameCount = 0;
    float deltaTime = 0.0f;
    float meanPeriod = 0.0f;
    int frameCounter;
    bool sentMsgFlag = false;
    private char[,] trials;
    const char thumb = 'T';
    const char index = 'I';
    const char noGo = 'B';
    const char none = 'N';
    private float detectionRadius = 0.1f; // Radius for detecting proximity
    private float requiredStayTime = 1f; // Time required to stay in position to trigger color change
    private float timeInPosition = 0f; // Variable to counts the time that stays in position
    private Transform indexTip;
    private Transform thumbTip;
    private Transform indexTip_handRefPos;
    private Transform thumbTip_handRefPos;
    private byte[] motor;
    int distractorCount;
    Vector3[] position;
    string strMsg;
    char button;
    string elapsedTimeStr;
    bool readingButton;
    int elapsedTime;
    Renderer renderer_fixationMark;
    int congruent = 0;
    int incongruent = 0;
    int familiarization = 0;
    int targetOnly = 0;
    int nogo = 0;
    int nTargetOnlyThumb;
    int nTargetOnlyFinger;
    int nCongruentFamiliarizationThumb;
    int nCongruentFamiliarizationFinger;
    int nIncongruentFamiliarizationThumbFinger;
    int nIncongruentFamiliarizationFingerThumb;
    int nCongruentThumb;
    int nCongruentFinger;
    int nIncongruentThumbFinger;
    int nIncongruentFingerThumb;
    int nThumbNoGo;
    int nFingerNoGo;
    string fileName;
    string filePath;
    string id;
    int nTrials;
    int nTrials_noGo;
    int nTrials_targetOnly;
    int nTrials_familiarization; 

    // TrialData class to hold each trial's data
    public class TrialData
    {
        public string Timestamp { get; set; }
        public string Test { get; set; }
        public string Type { get; set; }
        public char Vibration { get; set; }
        public char Visual_Distractor { get; set; }
        public char Response { get; set; }
        public int ElapsedTime { get; set; }
        public float MeanUpdatePeriod { get; set; }

        public override string ToString()
        {
            return $"{Timestamp},{Test},{Type},{Vibration},{Visual_Distractor},{Response},{ElapsedTime},{MeanUpdatePeriod}";
        }
    }

    // Struct to store position and rotation
    struct PositionRotationCombo
    {
        public Vector3 position;
        public Vector3 rotationEuler;

        public PositionRotationCombo(Vector3 pos, Vector3 rot)
        {
            position = pos;
            rotationEuler = rot;
        }
    }

    // Number of possibilities
    private int numberOfPossibilities = 2;
    // Number of elements in the vector
    private int numberOfElements;


    void Start()
    {  
        // Find the necessary GameObjects
        userHand = GameObject.Find("Rig/Camera Offset/RightHand");
        hapticControl = GameObject.Find("Haptic Control").GetComponent<HapticControl>();
        experimentManager = GameObject.Find("Experiment Manager").GetComponent<ExperimentManager>();
        fixationMark = Instantiate(Resources.Load<GameObject>("Prefabs/fixationMark"), Vector3.zero, Quaternion.identity);
        fixationMark.SetActive(false);
        stopwatch = Instantiate(Resources.Load<GameObject>("Models/Stopwatch/stopwatch"), Vector3.zero, Quaternion.identity);
        stopwatch.SetActive(false);
        userEyes = GameObject.Find("Rig/Camera Offset/Main Camera").transform;

        // Get important data from ExperimentManager
        id = experimentManager.ID;
        filePath = experimentManager.filePath;
        nTrials = experimentManager.nTrials;
        nTrials_noGo = experimentManager.nTrials_noGo;
        nTrials_targetOnly = experimentManager.nTrials_targetOnly;
        nTrials_familiarization = experimentManager.nTrials_familiarization; 

        // Define the name of the file that will be saved
        fileName = id + "_CCT.csv";

        // Prepare the file to save data
        // Ensure the directory exists
        if (!Directory.Exists(filePath))
        {
            Directory.CreateDirectory(filePath);
        }

        // Initialize file path
        filePath = Path.Combine(filePath, fileName);

        // Ensure the file has headers if it's new
        if (!File.Exists(filePath))
        {
            File.WriteAllText(filePath, "Timestamp,Test,Type,Vibration,Visual Distractor,Response,Elapsed Time,Mean Update Period\n");
        }

        // Array to store position and rotation combinations
        PositionRotationCombo[] positionRotationArray = new PositionRotationCombo[numberOfPossibilities];
        // Initialize the array with desired combinations of position and rotation
        positionRotationArray[0] = new PositionRotationCombo(new Vector3(0.25f, 1.02f, 0.30f), new Vector3(307.77f, 302.50f, 27.84f));
        positionRotationArray[1] = new PositionRotationCombo(new Vector3 (-0.05f, 1.03f, 0.33f), new Vector3(306.80f, 264.32f, 28.40f));


        // Disable the control of the hands during the CCT but, before, set the prefab open         
        handAnimator = userHand.GetComponent<Animator>();
        handAnimator.SetFloat("Blend", 1.0f);
        // Get the script component from the target GameObject
        scriptToDisable = (MonoBehaviour)userHand.GetComponent("PinchControl");
        // Disable the script
        scriptToDisable.enabled = false;
        
        // Create a 2D array to store the trials matrix that stores the combinations of incongruent, congruent, CCT practice (if applicable) and nogo trials 
        if(test.ToString() == "pre")
        {
            trials = new char[2, nTrials + nTrials_noGo + nTrials_targetOnly + nTrials_familiarization];

            // Define the balance among all conditions
            nTargetOnlyThumb = (nTrials_targetOnly) / 2;
            nTargetOnlyFinger = (nTrials_targetOnly) / 2;

            nCongruentFamiliarizationThumb = (nTrials_familiarization) / 4;
            nCongruentFamiliarizationFinger = (nTrials_familiarization) / 4;
            nIncongruentFamiliarizationThumbFinger = (nTrials_familiarization) / 4;
            nIncongruentFamiliarizationFingerThumb = (nTrials_familiarization) / 4;

            nCongruentThumb = nTrials / 4;
            nCongruentFinger = nTrials / 4;
            nIncongruentThumbFinger = nTrials / 4;
            nIncongruentFingerThumb = nTrials / 4;

            nThumbNoGo = nTrials_noGo / 2;
            nFingerNoGo = nTrials_noGo / 2;

            // Add all the possibilities to the matrix as per the parameters of the test
            AddTrials(trials, thumb, none, 0, nTargetOnlyThumb); // Add thumb-none
            AddTrials(trials, index, none, nTargetOnlyThumb, nTargetOnlyFinger); // Add index-none

            AddTrials(trials, thumb, thumb, nTrials_targetOnly, nCongruentFamiliarizationThumb); // Add congruent trials thumb-thumb
            AddTrials(trials, index, index, nTrials_targetOnly + nCongruentFamiliarizationThumb, nCongruentFamiliarizationFinger); // Add congruent trials index-index
            AddTrials(trials, thumb, index, nTrials_targetOnly + (2 * nCongruentFamiliarizationThumb), nIncongruentFamiliarizationThumbFinger); // Add incongruent trials thumb-index
            AddTrials(trials, index, thumb, nTrials_targetOnly +  (3 * nCongruentFamiliarizationThumb), nIncongruentFamiliarizationFingerThumb); // Add incongruent trials index-thumb

            AddTrials(trials, thumb, thumb, nTrials_targetOnly + nTrials_familiarization, nCongruentThumb); // Add congruent trials thumb-thumb
            AddTrials(trials, index, index, nTrials_targetOnly + nTrials_familiarization + nCongruentThumb, nCongruentFinger); // Add congruent trials index-index
            AddTrials(trials, thumb, index, nTrials_targetOnly + nTrials_familiarization + (2 * nCongruentThumb), nIncongruentThumbFinger); // Add incongruent trials thumb-index
            AddTrials(trials, index, thumb, nTrials_targetOnly + nTrials_familiarization + (3 * nCongruentThumb), nIncongruentFingerThumb); // Add incongruent trials index-thumb

            AddTrials(trials, thumb, noGo, nTrials_targetOnly + nTrials_familiarization + nTrials, nThumbNoGo); // Add noGo trials, thumb-N
            AddTrials(trials, index, noGo, nTrials_targetOnly + nTrials_familiarization + nTrials + nThumbNoGo, nFingerNoGo); // Add noGo trials, index-N

            // Shuffle the trials using Fisher-Yates algorithm
            Shuffle(trials, 0, nTrials_familiarization + nTrials + nTrials_noGo);
            Shuffle(trials, nTrials_targetOnly, nTrials + nTrials_noGo);
            Shuffle(trials, nTrials_targetOnly + nTrials_familiarization, 0);

            // Set the CCT status
            experimentManager.TargetOnlyTrials = "0 out of " + nTrials_targetOnly + ".";
            experimentManager.FamiliarizationTrials = "0 out of " + nTrials_familiarization + ".";
            experimentManager.CCTTrials = "0 out of " + nTrials + ".";
            experimentManager.NoGoTrials = "0  out of " + nTrials_noGo + ".";
        }
        else
        {
            trials = new char[2, nTrials + nTrials_noGo];

            // Define the balance among all conditions
            nCongruentThumb = nTrials / 4;
            nCongruentFinger = nTrials / 4;
            nIncongruentThumbFinger = nTrials / 4;
            nIncongruentFingerThumb = nTrials / 4;
            nThumbNoGo = nTrials_noGo / 2;
            nFingerNoGo = nTrials_noGo / 2;

            // Add all the possibilities to the matrix as per the parameters of the test
            AddTrials(trials, thumb, thumb, 0, nCongruentThumb); // Add congruent trials thumb-thumb
            AddTrials(trials, index, index, nCongruentThumb, nCongruentFinger); // Add congruent trials index-index
            AddTrials(trials, thumb, index, 2 * nCongruentThumb, nIncongruentThumbFinger); // Add incongruent trials thumb-index
            AddTrials(trials, index, thumb, 3 * nCongruentThumb, nIncongruentFingerThumb); // Add incongruent trials index-thumb
            AddTrials(trials, thumb, noGo, nTrials, nThumbNoGo); // Add noGo trials, thumb-N
            AddTrials(trials, index, noGo, nTrials + nThumbNoGo, nFingerNoGo); // Add noGo trials, index-N

            // Shuffle the trials using Fisher-Yates algorithm
            Shuffle(trials, 0, 0);

            // Set the CCT status
            experimentManager.TargetOnlyTrials = "NA";
            experimentManager.FamiliarizationTrials = "NA";
            experimentManager.CCTTrials = "0 out of " + nTrials + ".";
            experimentManager.NoGoTrials = "0  out of " + nTrials_noGo + ".";
        }
        
        // Display the results (for debugging)
        // PrintResults(trials);

        numberOfElements = trials.GetLength(1);
        List<int> vector = HandPosVec(numberOfPossibilities, numberOfElements);
        ShuffleVector(vector);

        // Display the shuffled vector in the console
        //foreach (int item in vector)
        //{
        //    Debug.Log(item);
        //}

        // Check if test and testType were defined
        if(test.ToString() != "None" || testType.ToString() != "None")
        {
            // Start the coroutine to execute the CCT steps in sequence
            StartCoroutine(StepsCCT(positionRotationArray, vector));
        }
        else
        {
            Debug.LogError("Variables test and testType must be defined.");
        }
    }

    IEnumerator StepsCCT(PositionRotationCombo[] positionRotationArray, List<int> vector)
    {
        startRecordingFPS = true;

        for (int trial = 0; trial < trials.GetLength(1); trial++){

            yield return StartCoroutine(positionHands(trial, positionRotationArray, vector));
        
            yield return StartCoroutine(runTrial(trial));

            yield return StartCoroutine(readResponse());

            yield return StartCoroutine(feedback(trial));

            yield return StartCoroutine(saveAndStatus(trial));
        }

        startRecordingFPS = false;
        scriptToDisable.enabled = true;
    }

    IEnumerator positionHands(int trial, PositionRotationCombo[] positionRotationArray, List<int> vector)
    {
        handRefPos = Instantiate(Resources.Load<GameObject>("Prefabs/Open_Pinch"), positionRotationArray[vector[trial]].position, Quaternion.Euler(positionRotationArray[vector[trial]].rotationEuler));
        handRefPos.GetComponentInChildren<SkinnedMeshRenderer>().material = (Material)Resources.Load("Materials/Clear", typeof(Material));

        indexTip_handRefPos = handRefPos.transform.Find("R_Wrist/R_IndexMetacarpal/R_IndexProximal/R_IndexIntermediate/R_IndexDistal/R_IndexTip");
        thumbTip_handRefPos = handRefPos.transform.Find("R_Wrist/R_ThumbMetacarpal/R_ThumbProximal/R_ThumbDistal/R_ThumbTip");

        Vector3 fixationMarkPosition = CalculateMidpoint(indexTip_handRefPos.position, thumbTip_handRefPos.position);
        
        fixationMark.SetActive(true);
        fixationMark.transform.SetPositionAndRotation(fixationMarkPosition, Quaternion.identity);
        renderer_fixationMark = fixationMark.GetComponent<MeshRenderer>();
        renderer_fixationMark.sharedMaterial.color = Color.white;

        Vector3 stopwatchPosition = fixationMarkPosition;
        stopwatchPosition.y -= 0.015f;
        stopwatch.transform.position = stopwatchPosition;

        timeInPosition = 0f;
        while (true)
        {
        // Check if the player is within the detection radius of the target position
        float distanceToTarget = Vector3.Distance(userHand.transform.position, handRefPos.transform.position);
        
            if (distanceToTarget <= detectionRadius)
            {
                // If the player is within range, start counting time
                timeInPosition += Time.deltaTime;
                // If the required time is reached, change the color of the object
                if (timeInPosition >= requiredStayTime)
                {
                    Destroy(handRefPos);
                    yield break;
                }
            }
            else
            {
                // If the player moves out of range, reset the timer
                timeInPosition = 0f;
            }
            yield return null;
        }  
    }

    IEnumerator runTrial(int trial)
    {
        // Define the motor to vibrate in this trial
        switch (testType.ToString())
        {
            case "H2H":
                switch (trials[0,trial])
                {
                    case thumb:
                        motor = thumbHand_CCT;
                        break;
                    case index:
                        motor = indexHand_CCT;
                        break;
                }
                break;
            
            case "H2S":
                switch (trials[0,trial])
                {
                    case thumb:
                        motor = thumbShoulder_CCT;
                        break;
                    case index:
                        motor = indexShoulder_CCT;
                        break;
                }
                break;
        }

        if(trials[1,trial] == 'N')
        {
            // Change the colour of the fixation mark to green
            renderer_fixationMark.sharedMaterial.color = Color.green;

            // Wait with the fixaton mark turned on
            int fixationTimeFrames = UnityEngine.Random.Range(fixationTimeFramesMin, fixationTimeFramesMax);
            frameCounter = 0;
            while (frameCounter < fixationTimeFrames)
            {
                frameCounter++;
                yield return null;
            }
            
            // Turn off the fixation mark
            fixationMark.SetActive(false);

            // Start the loop to show the visual distractor and vibrate the motor
            frameCounter = 0;
            while (frameCounter < flickeringPeriodFrames)
            {
                // Send the command to Arduino in frame (systemDelayFrames + delayFrames), to ensure the desired delay
                if ((frameCounter == (systemDelayFrames + delayFrames)) && !sentMsgFlag)
                {
                    hapticControl.comPort.Write(motor, 0, motor.Length);
                    sentMsgFlag = true;
                }

                frameCounter++;
                yield return null;
            }

            // Reset the flag after finishing the flickering sequence
            sentMsgFlag = false;
        }
        else
        {
            // Get the positions in with the visual distractors will appear
            indexTip = userHand.transform.Find("R_Wrist/R_IndexMetacarpal/R_IndexProximal/R_IndexIntermediate/R_IndexDistal/R_IndexTip");
            thumbTip = userHand.transform.Find("R_Wrist/R_ThumbMetacarpal/R_ThumbProximal/R_ThumbDistal/R_ThumbTip");

            // Define the visual distractor to show in this trial
            switch (trials[1,trial])
            {
                case thumb:
                    distractorCount = 1;
                    position = new Vector3[distractorCount];
                    position[0] = thumbTip.position;
                    break;
                case index:
                    distractorCount = 1;
                    position = new Vector3[distractorCount];
                    position[0] = indexTip.position;
                    break;
                case noGo:
                    distractorCount = 2;
                    position = new Vector3[distractorCount];
                    position[0] = indexTip.position;
                    position[1] = thumbTip.position;
                    break;
            }

            // Create the visual distractor object(s)
            GameObject[] visualDistractor = new GameObject[distractorCount];
            for (int ii = 0; ii < visualDistractor.Length; ii++)
            {
                visualDistractor[ii] = Instantiate(Resources.Load<GameObject>("Prefabs/visualDistractor"), position[ii], Quaternion.identity); 
                visualDistractor[ii].SetActive(false);
            }

            // Change the colour of the fixation mark to green
            renderer_fixationMark.sharedMaterial.color = Color.green;

            // Wait with the fixaton mark turned on
            int fixationTimeFrames = UnityEngine.Random.Range(fixationTimeFramesMin, fixationTimeFramesMax);
            frameCounter = 0;
            while (frameCounter < fixationTimeFrames)
            {
                frameCounter++;
                yield return null;
            }
            
            // Turn off the fixation mark
            fixationMark.SetActive(false);

            // Start the loop to show the visual distractor and vibrate the motor
            frameCounter = 0;
            while (frameCounter < flickeringPeriodFrames)
            {
                // Send the command to Arduino in frame (systemDelayFrames + delayFrames), to ensure the desired delay
                if ((frameCounter == (systemDelayFrames + delayFrames)) && !sentMsgFlag)
                {
                    hapticControl.comPort.Write(motor, 0, motor.Length);
                    sentMsgFlag = true;
                }

                // Flicker the visual distractor
                if (frameCounter % (activeFrames + inactiveFrames) < activeFrames)
                {
                    foreach (var distractor in visualDistractor)
                    {
                        distractor.SetActive(true);
                    }
                }
                else
                {
                    foreach (var distractor in visualDistractor)
                    {
                        distractor.SetActive(false);
                    }
                }
                frameCounter++;
                yield return null;
            }

            // Reset the flag after finishing the flickering sequence
            sentMsgFlag = false;
        
            // Destroy the visual distractor(s)
            foreach (var distractor in visualDistractor)
            {
                Destroy(distractor);
            }
        }

        yield return null;
    }

    IEnumerator readResponse()
    {
        while(!hapticControl.msgReceived)
        {
            yield return null;
        } 
        strMsg = hapticControl.msg;
        hapticControl.msgReceived = false;

        button = '\0';
        elapsedTimeStr = "";
        readingButton = true;
        foreach (char c in strMsg)
        {
            if (readingButton)
            {
                if (c == '@')
                {
                    readingButton = false;
                }
                else
                {
                    button += c;
                }
            }
            else
            {
                if (c == '#')
                {
                    break;
                }
                else
                {
                    elapsedTimeStr += c;
                }
            }
        }

        elapsedTime = int.Parse(elapsedTimeStr);
    }

    IEnumerator feedback(int trial)
    {
        
        if (elapsedTime<tooSlow)
        {
            renderer_fixationMark.sharedMaterial.color = Color.green;
            
            if(trials[1,trial] != noGo) // It is not a no-go trial
            {       
                if(trials[0,trial] != button)
                {
                    renderer_fixationMark.sharedMaterial.color = Color.red;
                }
            }
            else // It is a no-go trial - in this case, the participant needs to withhold the response, as a consequnce, response should be 'O'
            {
                if(button!='O')
                {
                    renderer_fixationMark.sharedMaterial.color = Color.red;
                }
            }
            
            fixationMark.SetActive(true);
            float flickerStartTime = Time.time;
            while (Time.time - flickerStartTime < 1.0f) // show feedback for 1s
            {
                fixationMark.SetActive(false);
                yield return new WaitForSeconds(0.05f); 
                fixationMark.SetActive(true);
                yield return new WaitForSeconds(0.05f);
            }
            fixationMark.SetActive(false);
        }
        else
        {
            if(trials[1,trial] != noGo) // It is not a no-go trial
            {       
                stopwatch.transform.LookAt(userEyes);
                // Get the current rotation of the object
                Quaternion currentRotation = stopwatch.transform.rotation;
                // Calculate the desired rotation by adding 90 degrees to the current rotation around the y-axis
                Quaternion desiredRotation = Quaternion.Euler(currentRotation.eulerAngles + new Vector3(-90f, 0f, 0f));
                // Apply the desired rotation to the object
                stopwatch.transform.rotation = desiredRotation;
                
                stopwatch.SetActive(true);
                yield return new WaitForSeconds(1.0f);
                stopwatch.SetActive(false);
            }
            else
            {
                renderer_fixationMark.sharedMaterial.color = Color.green;
                fixationMark.SetActive(true);
                float flickerStartTime = Time.time;
                while (Time.time - flickerStartTime < 1.0f) // show feedback for 1s
                {
                    fixationMark.SetActive(false);
                    yield return new WaitForSeconds(0.05f); 
                    fixationMark.SetActive(true);
                    yield return new WaitForSeconds(0.05f);
                }
                fixationMark.SetActive(false);
            }
        }

        yield return null;
    }

    IEnumerator saveAndStatus(int trial)
    {
        meanPeriod = deltaTime/frameCount;
        frameCount = 0;
        deltaTime = 0.0f;

        // Create a new trial data object
        TrialData trialData = new TrialData
        {
            Timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            Test = test.ToString(),
            Type = testType.ToString(),
            Vibration = trials[0,trial],
            Visual_Distractor = trials[1,trial],
            Response = button,
            ElapsedTime = elapsedTime,
            MeanUpdatePeriod = meanPeriod
        };

        // Write the trial data to the CSV file
        using (StreamWriter sw = new StreamWriter(filePath, true))
        {
            sw.WriteLine(trialData.ToString());
        }

        if(test.ToString() == "pre")
        {

            if(trial<nTrials_targetOnly)
            {
                targetOnly++;
            }
            else if(trial>=nTrials_targetOnly && trial<(nTrials_familiarization+nTrials_targetOnly))
            {
                familiarization++;
            }
            else
            {
                if(trials[1,trial] != noGo) // It is not a no-go trial
                {       
                    if(trials[0,trial] != trials[1,trial])
                    {
                        incongruent++;
                    }
                    else
                    {
                        congruent++;
                    }
                }
                else // It is a no-go trial - in this case, the participant needs to withhold the response, as a consequnce, response should be 'O'
                {
                    nogo++;
                }
            }

            // Set the CCT status
            experimentManager.TargetOnlyTrials = targetOnly + " out of " + nTrials_targetOnly + ".";
            experimentManager.FamiliarizationTrials = familiarization + " out of " + nTrials_familiarization + ".";
            experimentManager.CCTTrials = (incongruent+congruent) + " out of " + nTrials + ".";
            experimentManager.NoGoTrials = nogo + "  out of " + nTrials_noGo + ".";
        }
        else
        {
            if(trials[1,trial] != noGo) // It is not a no-go trial
            {       
                if(trials[0,trial] != trials[1,trial])
                {
                    incongruent++;
                }
                else
                {
                    congruent++;
                }
            }
            else // It is a no-go trial - in this case, the participant needs to withhold the response, as a consequnce, response should be 'O'
            {
                nogo++;
            }

            // Set the CCT status
            experimentManager.TargetOnlyTrials = "NA";
            experimentManager.FamiliarizationTrials = "NA";
            experimentManager.CCTTrials = (incongruent+congruent) + " out of " + nTrials + ".";
            experimentManager.NoGoTrials = nogo + "  out of " + nTrials_noGo + ".";
        }

        yield return null;
    }

    Vector3 CalculateMidpoint(Vector3 a, Vector3 b)
    {
        return new Vector3(
            (a.x + b.x) / 2,
            (a.y + b.y) / 2,
            (a.z + b.z) / 2
        );
    }

    static void AddTrials(char[,] trials, char value1, char value2, int start, int count)
    {
        for (int i = start; i < start + count; i++)
        {
            trials[0, i] = value1;
            trials[1, i] = value2;
        }
    }

    static void Shuffle(char[,] array, int linesToExcludeFromTop, int linesToExcludeFromBottom)
    {
        int length = array.GetLength(1);
        int start = linesToExcludeFromTop;
        int end = length - linesToExcludeFromBottom - 1;

        for (int i = end; i > start; i--)
        {
            int j = UnityEngine.Random.Range(start, i + 1);
            for (int k = 0; k < array.GetLength(0); k++)
            {
                char temp = array[k, i];
                array[k, i] = array[k, j];
                array[k, j] = temp;
            }
        }
    }

    static void PrintResults(char[,] results)
    {
        for (int i = 0; i < results.GetLength(1); i++)
        {
            Debug.Log($"{results[0, i]} {results[1, i]}");
        }
    }

    List<int> HandPosVec(int numberOfPossibilities, int totalLength)
    {
        List<int> vector = new List<int>();
        int repetition = totalLength / numberOfPossibilities;

        for (int i = 0; i < numberOfPossibilities; i++)
        {
            for (int j = 0; j < repetition; j++)
            {
                vector.Add(i);
            }
        }

        return vector;
    }

    void ShuffleVector(List<int> vector)
    {
        for (int i = 0; i < vector.Count; i++)
        {
            int temp = vector[i];
            int randomIndex = UnityEngine.Random.Range(i, vector.Count);
            vector[i] = vector[randomIndex];
            vector[randomIndex] = temp;
        }

        // Ensure no consecutive repeats
        for (int i = 0; i < vector.Count - 1; i++)
        {
            if (vector[i] == vector[i + 1])
            {
                int temp = vector[i];
                int randomIndex = UnityEngine.Random.Range(i + 1, vector.Count);
                vector[i] = vector[randomIndex];
                vector[randomIndex] = temp;
            }
        }
    }
    void Update()
    {
        if(startRecordingFPS)
        {
            // Record the time taken for the current frame and sum up to the previous frames within the current trial
            frameCount++;
            deltaTime += Time.deltaTime;
        }
    }
}
