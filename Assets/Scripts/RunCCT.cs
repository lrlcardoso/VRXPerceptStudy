using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.IO.Ports;
using System.Collections;

public class RunCCT : MonoBehaviour
{
    // Parameters for the CCT:
    string testType = "H2H"; // Can be "H2H" (Hand-to-Hand) or "H2S" (Hand-to-Shoulder)
    int nTrials = 4; // Total number of trials, congruant + incongruent          
    int nTrials_noGo = 2; // Number of no go trials

    // GameObject that communicates with Arduino (separate thread)
    private HapticControl hapticControl;

    // GameObjects to load during the test
    private GameObject fixationMark;
    private GameObject handRefPos;
    //private GameObject[] visualDistractor = new GameObject[2];

    // User's hand position (wrist)
    private Transform userHand; 
     
    // Messages to communicate with Arduino
    private byte[] thumbShoulder_CCT = new byte[] { 0x33 };
    private byte[] indexShoulder_CCT = new byte[] { 0x34 };
    private byte[] thumbHand_CCT = new byte[] { 0x35 };
    private byte[] indexHand_CCT = new byte[] { 0x36 };
    
    // Other variables
    private char[,] trials;
    char thumb = 'T';
    char finger = 'F';
    char noGo = 'N';
    private float detectionRadius = 0.1f; // Radius for detecting proximity
    private float requiredStayTime = 1f; // Time required to stay in position to trigger color change
    private float timeInPosition = 0f; // Variable to counts the time that stays in position
    private Transform indexTip;
    private Transform thumbTip;
    private byte[] motor;
    int distractorCount;
    Vector3[] position;


    void Start()
    {
        // Find the necessary GameObjects
        userHand = GameObject.Find("Rig/Camera Offset/RightHand").transform;
        hapticControl = GameObject.Find("Haptic Control").GetComponent<HapticControl>();

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
        AddTrials(trials, finger, finger, nCongruentThumb, nCongruentFinger); // Add congruent trials finger-finger
        AddTrials(trials, thumb, finger, 2 * nCongruentThumb, nIncongruentThumbFinger); // Add incongruent trials thumb-finger
        AddTrials(trials, finger, thumb, 3 * nCongruentThumb, nIncongruentFingerThumb); // Add incongruent trials finger-thumb
        AddTrials(trials, thumb, noGo, nTrials, nThumbNoGo); // Add noGo trials, thumb-N
        AddTrials(trials, finger, noGo, nTrials + nThumbNoGo, nFingerNoGo); // Add noGo trials, finger-N
        
        // Shuffle the trials using Fisher-Yates algorithm
        Shuffle(trials, nTrials + nTrials_noGo);
        
        // Display the results (for debugging)
        PrintResults(trials);

        // Start the coroutine to execute the CCT steps in sequence
        StartCoroutine(StepsCCT());
    
    }

    IEnumerator StepsCCT()
    {
        for (int i = 0; i < 6; i++){

            // Step 1: show 
            yield return StartCoroutine(positionHands());
        
            yield return StartCoroutine(runTrial(i));

            //yield return StartCoroutine(Function3());
        }

        Debug.Log("CCT successfully completed!");
    }

    IEnumerator positionHands()
    {
        handRefPos = Instantiate(Resources.Load<GameObject>("Prefabs/Open_Pinch"), new Vector3(0.15f,0.96f,0.15f), Quaternion.Euler(new Vector3(311.11f,303.52f,15.66f)));
        handRefPos.GetComponentInChildren<SkinnedMeshRenderer>().material = (Material)Resources.Load("Materials/Clear", typeof(Material));

        timeInPosition = 0f;
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
    }

    IEnumerator runTrial(int i)
    {
        indexTip = userHand.transform.Find("R_Wrist/R_IndexMetacarpal/R_IndexProximal/R_IndexIntermediate/R_IndexDistal/R_IndexTip");
        thumbTip = userHand.transform.Find("R_Wrist/R_ThumbMetacarpal/R_ThumbProximal/R_ThumbDistal/R_ThumbTip");

        // Define the motor to vibrate in this trial
        switch (testType)
        {
            case "H2H":
                switch (trials[0,i])
                {
                    case 'T':
                        motor = thumbHand_CCT;
                        break;
                    case 'F':
                        motor = indexHand_CCT;
                        break;
                }
                break;
            
            case "H2S":
                switch (trials[0,i])
                {
                    case 'T':
                        motor = thumbShoulder_CCT;
                        break;
                    case 'F':
                        motor = indexShoulder_CCT;
                        break;
                }
                break;
        }

        // Define the visual distractor to show in this trial
        switch (trials[1,i])
        {
            case 'T':
                distractorCount = 1;
                position = new Vector3[distractorCount];
                position[0] = thumbTip.position;
                break;
            case 'F':
                distractorCount = 1;
                position = new Vector3[distractorCount];
                position[0] = indexTip.position;
                break;
            case 'N':
                distractorCount = 2;
                position = new Vector3[distractorCount];
                position[0] = indexTip.position;
                position[1] = thumbTip.position;
                break;
        }

        Vector3 fixationMarkPosition = CalculateMidpoint(indexTip.position, thumbTip.position);
        
        fixationMark = Instantiate(Resources.Load<GameObject>("Prefabs/fixationMark"), fixationMarkPosition, Quaternion.identity);
        Renderer renderer_fixationMark = fixationMark.GetComponent<MeshRenderer>();
        renderer_fixationMark.sharedMaterial.color = Color.green;

        yield return new WaitForSeconds(2.0f);

        hapticControl.comPort.Write(thumbShoulder_CCT, 0, thumbShoulder_CCT.Length);

        //yield return new WaitForSeconds(0.5f);
        
        fixationMark.SetActive(false);
        
        GameObject[] visualDistractor = new GameObject[distractorCount];
        for (int ii = 0; ii < visualDistractor.Length; ii++)
        {
            visualDistractor[ii] = Instantiate(Resources.Load<GameObject>("Prefabs/visualDistractor"), position[ii], Quaternion.identity); 
        }

        float flickerStartTime = Time.time;

        while (Time.time - flickerStartTime < 0.2f)
        {
            foreach (var distractor in visualDistractor)
            {
                distractor.SetActive(false);
            }
            yield return new WaitForSeconds(0.005f); 

            foreach (var distractor in visualDistractor)
            {
                distractor.SetActive(true);
            }
            yield return new WaitForSeconds(0.005f);
        }
            foreach (var distractor in visualDistractor)
            {
                distractor.SetActive(false);
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

    // Update is called once per frame
    //void Update()
    //{
        //if (Input.GetKeyDown("space"))
        //{
        //    hapticControl.comPort.Write(thumbShoulder_CCT, 0, thumbShoulder_CCT.Length);
            
        //}

        //if (hapticControl.msgReceived)
        //{
        //    Debug.Log(hapticControl.msg);
        //    hapticControl.msgReceived = false;
        //}
        
    //}
}
