using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.IO.Ports;

public class RunCCT : MonoBehaviour
{
    // Path and file name to save the results (CSV file)
    private string filePath = @"C:\Users\s4659771\Documents\";
    private string fileName = "test.csv";

    // Parameters for the CCT ----------------------------------------------------------
    string test = "pre"; // Can be "pre" or "post"
    string testType = "H2S"; // Can be "H2H" (Hand-to-Hand) or "H2S" (Hand-to-Shoulder)
    int nTrials = 10; // Total number of trials, congruant + incongruent          
    int nTrials_noGo = 2; // Number of no go trials
    
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
    float fixationTime = 1.2f;

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

    // 
    private ExperimentManager experimentManager;

    // 
    private Transform userEyes;

    // GameObjects to load during the test
    private GameObject curtain;
    private GameObject fixationMark;
    private GameObject handRefPos;
    private GameObject stopwatch;

    // User's hand position (wrist)
    private Transform userHand; 
     
    // Messages to communicate with Arduino
    private byte[] thumbShoulder_CCT = new byte[] { 0x33 };
    private byte[] indexShoulder_CCT = new byte[] { 0x34 };
    private byte[] thumbHand_CCT = new byte[] { 0x35 };
    private byte[] indexHand_CCT = new byte[] { 0x36 };
    
    // Other variables
    bool sentMsgFlag = false;
    private char[,] trials;
    const char thumb = 'T';
    const char index = 'I';
    const char noGo = 'N';
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
    int nogo = 0;

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

        public override string ToString()
        {
            return $"{Timestamp},{Test},{Type},{Vibration},{Visual_Distractor},{Response},{ElapsedTime}";
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
            File.WriteAllText(filePath, "Timestamp,Test,Type,Vibration,Visual Distractor,Response,ElapsedTime\n");
        }

        // Find the necessary GameObjects
        curtain = Instantiate(Resources.Load<GameObject>("Prefabs/Canvas"), Vector3.zero, Quaternion.identity);
        userHand = GameObject.Find("Rig/Camera Offset/RightHand").transform;
        hapticControl = GameObject.Find("Haptic Control").GetComponent<HapticControl>();
        experimentManager = GameObject.Find("Experiment Manager").GetComponent<ExperimentManager>();
        fixationMark = Instantiate(Resources.Load<GameObject>("Prefabs/fixationMark"), Vector3.zero, Quaternion.identity);
        fixationMark.SetActive(false);
        stopwatch = Instantiate(Resources.Load<GameObject>("Models/Stopwatch/stopwatch"), Vector3.zero, Quaternion.identity);
        stopwatch.SetActive(false);
        userEyes = GameObject.Find("Rig/Camera Offset/Main Camera").transform;

        // Array to store position and rotation combinations
        PositionRotationCombo[] positionRotationArray = new PositionRotationCombo[numberOfPossibilities];
        // Initialize the array with desired combinations of position and rotation
        positionRotationArray[0] = new PositionRotationCombo(new Vector3(0.25f, 1.02f, 0.30f), new Vector3(307.77f, 302.50f, 27.84f));
        positionRotationArray[1] = new PositionRotationCombo(new Vector3 (-0.05f, 1.03f, 0.33f), new Vector3(306.80f, 264.32f, 28.40f));

        // Create a 2D array to store the trials matrix that stores the combinations of incongruent, congruent and nogo trials 
        trials = new char[2, nTrials + nTrials_noGo];

        // Define the balance among all conditions
        int nCongruentThumb = nTrials / 4;
        int nCongruentFinger = nTrials / 4;
        int nIncongruentThumbFinger = nTrials / 4;
        int nIncongruentFingerThumb = nTrials / 4;
        int nThumbNoGo = nTrials_noGo / 2;
        int nFingerNoGo = nTrials_noGo / 2;

        // Add all the possibilities to the matrix as per the parameters of the test
        AddTrials(trials, thumb, thumb, 0, nCongruentThumb); // Add congruent trials thumb-thumb
        AddTrials(trials, index, index, nCongruentThumb, nCongruentFinger); // Add congruent trials index-index
        AddTrials(trials, thumb, index, 2 * nCongruentThumb, nIncongruentThumbFinger); // Add incongruent trials thumb-index
        AddTrials(trials, index, thumb, 3 * nCongruentThumb, nIncongruentFingerThumb); // Add incongruent trials index-thumb
        AddTrials(trials, thumb, noGo, nTrials, nThumbNoGo); // Add noGo trials, thumb-N
        AddTrials(trials, index, noGo, nTrials + nThumbNoGo, nFingerNoGo); // Add noGo trials, index-N
        
        // Shuffle the trials using Fisher-Yates algorithm
        Shuffle(trials, nTrials + nTrials_noGo);
        
        // Display the results (for debugging)
        // PrintResults(trials);

        experimentManager.CongruentTrials = "0 out of " + nTrials/2 + ".";
        experimentManager.IncongruentTrials = "0 out of " + nTrials/2 + ".";
        experimentManager.NoGoTrials = "0  out of " + nTrials_noGo + ".";

        numberOfElements = nTrials + nTrials_noGo;
        List<int> vector = HandPosVec(numberOfPossibilities, numberOfElements);
        ShuffleVector(vector);

        // Display the shuffled vector in the console
        //foreach (int item in vector)
        //{
        //    Debug.Log(item);
        //}

        curtain.SetActive(true);
        
        // Start the coroutine to execute the CCT steps in sequence
        StartCoroutine(StepsCCT(positionRotationArray, vector));
    }

    IEnumerator StepsCCT(PositionRotationCombo[] positionRotationArray, List<int> vector)
    {
        yield return StartCoroutine(StartTest());

        for (int trial = 0; trial < trials.GetLength(1); trial++){

            yield return StartCoroutine(positionHands(trial, positionRotationArray, vector));
        
            yield return StartCoroutine(runTrial(trial));

            yield return StartCoroutine(readResponse());

            yield return StartCoroutine(feedback(trial));

            yield return StartCoroutine(saveAndStatus(trial));
        }

        Debug.Log("CCT successfully completed!");
    }

    IEnumerator StartTest()
    {
        // Loop until the spacebar is pressed
        while (!Input.GetKeyDown(KeyCode.Space))
        {
            // Wait for the next frame
            yield return null;
        }
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
        
        // The next piece of code was commented because we do not need to waint until the participant's hand is in the right position during the time delay estimation
        /*
        while (true)
        {
        // Check if the player is within the detection radius of the target position
        float distanceToTarget = Vector3.Distance(userHand.position, handRefPos.transform.position);
        
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
        */
        yield return new WaitForSeconds(2.0f); 
    }

    IEnumerator runTrial(int trial)
    {
        // Get the positions in with the visual distractors will appear
        indexTip = userHand.transform.Find("R_Wrist/R_IndexMetacarpal/R_IndexProximal/R_IndexIntermediate/R_IndexDistal/R_IndexTip");
        thumbTip = userHand.transform.Find("R_Wrist/R_ThumbMetacarpal/R_ThumbProximal/R_ThumbDistal/R_ThumbTip");

        // Define the motor to vibrate in this trial
        switch (testType)
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
        }

        // Change the colour of the fixation mark to green
        renderer_fixationMark.sharedMaterial.color = Color.green;

        // Wait for the fixationTime
        yield return new WaitForSeconds(fixationTime);
        
        // Turn off the fixation mark
        fixationMark.SetActive(false);

        // Start the loop to show the visual distractor and vibrate the motor
        int frameCounter = 0;
        while (frameCounter < flickeringPeriodFrames)
        {
            // Send the command to Arduino in frame (systemDelayFrames + delayFrames), to ensure the desired delay
            if ((frameCounter == (systemDelayFrames + delayFrames)) && !sentMsgFlag)
            {
                hapticControl.comPort.Write(thumbShoulder_CCT, 0, thumbShoulder_CCT.Length);
                sentMsgFlag = true;
            }

            // Flicker the visual distractor
            if (frameCounter % (activeFrames + inactiveFrames) < activeFrames)
            {
                foreach (var distractor in visualDistractor)
                {
                    curtain.SetActive(false);
                }
            }
            else
            {
                foreach (var distractor in visualDistractor)
                {
                    curtain.SetActive(true);
                }
            }
            yield return null;
            frameCounter++;
        }

        // Reset the flag after finishing the flickering sequence
        sentMsgFlag = false;
    
        // Destroy the visual distractor(s)
        foreach (var distractor in visualDistractor)
        {
            Destroy(distractor);
        }

        // Destroy the curtain
        curtain.SetActive(true);

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
            
            if(trials[1,trial] != 'N') // It is not a no-go trial
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
            if(trials[1,trial] != 'N') // It is not a no-go trial
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
        // Create a new trial data object
        TrialData trialData = new TrialData
        {
            Timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            Test = test,
            Type = testType,
            Vibration = trials[0,trial],
            Visual_Distractor = trials[1,trial],
            Response = button,
            ElapsedTime = elapsedTime
        };

        // Write the trial data to the CSV file
        using (StreamWriter sw = new StreamWriter(filePath, true))
        {
            sw.WriteLine(trialData.ToString());
        }

        if(trials[1,trial] != 'N') // It is not a no-go trial
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

        experimentManager.CongruentTrials = congruent + " out of " + nTrials/2 + ".";
        experimentManager.IncongruentTrials = incongruent + " out of " + nTrials/2 + ".";
        experimentManager.NoGoTrials = nogo + "  out of " + nTrials_noGo + ".";

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

    static void Shuffle(char[,] array, int length)
    {
        for (int i = length - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
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
}