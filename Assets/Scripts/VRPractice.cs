using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.IO.Ports;

public enum practiceOptions
{
    None,
    primary,
    refresher,
    debrief
}
[RequireComponent(typeof(AudioSource))]
public class VRPractice : MonoBehaviour
{

    private AudioSource EndSound;
    // Needs to be defined for each prefab and match with the name of the prefab
    public practiceOptions practiceType = practiceOptions.None;

    private GameObject bubblePrefab;
    
    private Material material1;
    private Material material2;
    private GameObject indexSphere; // Reference to the index finger sphere
    private GameObject thumbSphere; // Reference to the thumb sphere
    private GameObject platform;
    private GameObject volumeControllerInstance;
    private Collider spawnVolume;
    private ScreenController screenController;
    public bool isAble2pinch = false;
    int nRepetitions;
    string id;
    string filePath;
    string fileName;
    int nRepetitions_primaryPrac;
    int nRepetitions_refresherPrac;
    int nRepetitions_debriefPrac;
    Vector3 handIniPos = new Vector3(0.06098f,0.14f,0.20876f); 
    Quaternion handIniRot = Quaternion.Euler(309.02655f,349.61621f,283.25412f); 
    // User's hand position (wrist)
    private GameObject userHand; 
    private PinchControl pinchControl;
    // GameObject to exchange info with the ExperimentManager
    private ExperimentManager experimentManager;
    private DetectObject platformCtr;
    private float detectionRadius = 0.1f; // Radius for detecting proximity
    private float requiredStayTime = 1f; // Time required to stay in position to trigger color change
    private float timeInPosition = 0f; // Variable to counts the time that stays in position
    private GameObject handIni;
    bool inBubbleStage = false;
    private GameObject platformPrefab;
    float startTime = 0f;
    float endTime = 0f;
    string stage = "";
    string text;

    // RepetitionData class to hold each repetition's data
    public class RepetitionData
    {
        public string Timestamp { get; set; }
        public string PracticeType { get; set; }
        public int Repetition { get; set; }
        public string Stage { get; set; }
        public float StartTime { get; set; }
        public float EndTime { get; set; }
        public override string ToString()
        {
            return $"{Timestamp},{PracticeType},{Repetition},{Stage},{StartTime},{EndTime}";
        }
    }
    void Awake() 
    {
        EndSound = GetComponent<AudioSource>();
    }

    private void Start()
    {
        // Get important data from ExperimentManager
        experimentManager = GameObject.Find("Experiment Manager").GetComponent<ExperimentManager>();
        id = experimentManager.ID;
        nRepetitions_primaryPrac = experimentManager.nRepetitions_primaryPrac;
        nRepetitions_refresherPrac = experimentManager.nRepetitions_refresherPrac;
        nRepetitions_debriefPrac = experimentManager.nRepetitions_debriefPrac;

        // Define the name of the file that will be saved
        fileName = id + "_PracticeTimes.csv";

        // Prepare the file to save data
        filePath = experimentManager.filePath + @"\" + id + @"\1_rawDATA";
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
            File.WriteAllText(filePath, "Timestamp, Practice Type, Repetition, Stage, Start Time, End Time\n");
        }

        // Find the necessary GameObjects
        pinchControl = GameObject.Find("Rig/Camera Offset/RightHand").GetComponent<PinchControl>();
        bubblePrefab = Resources.Load<GameObject>("Prefabs/Bubble");
        userHand = GameObject.Find("Rig/Camera Offset/RightHand");
        userHand.SetActive(true);

        pinchControl.enableUpdate = true;

        platform = Instantiate(Resources.Load<GameObject>("Prefabs/platform"));
        Vector3 originalPosition = platform.transform.position;
        // Set the desired Y position
        float newYPosition = originalPosition.y + experimentManager.TableHeight;
        // Set the new position vector with only Y-axis modified
        Vector3 newPosition = new Vector3(originalPosition.x, newYPosition, originalPosition.z);
        platform.transform.position = newPosition;
        platformCtr = platform.GetComponent<DetectObject>();

        // Set the volume to randomly spawn the bubbles
        volumeControllerInstance = Instantiate(Resources.Load<GameObject>("Prefabs/VolumeController"));
        spawnVolume = volumeControllerInstance.GetComponent<BoxCollider>();
        
        // Load materials from Resources folder
        material1 = Resources.Load<Material>("Materials/BubbleFinger");
        material2 = Resources.Load<Material>("Materials/BubbleThumb");

        // Find the spheres in the hierarchy
        indexSphere = GameObject.Find("Rig/Camera Offset/RightHand/R_Wrist/R_IndexMetacarpal/R_IndexProximal/R_IndexIntermediate/R_IndexDistal/R_IndexTip/IndexSphere");
        thumbSphere = GameObject.Find("Rig/Camera Offset/RightHand/R_Wrist/R_ThumbMetacarpal/R_ThumbProximal/R_ThumbDistal/R_ThumbTip/ThumbSphere");


        // Check if practiceType was defined
        if(practiceType.ToString() != "None")
        {
            if(practiceType.ToString()=="primary")
            {
                nRepetitions = nRepetitions_primaryPrac;
            }
            else if(practiceType.ToString()=="refresher")
            {
                nRepetitions = nRepetitions_refresherPrac;
            }
            else if(practiceType.ToString()=="debrief")
            {
                nRepetitions = nRepetitions_debriefPrac;
            }
            
            experimentManager.Repetition = "0  out of " + nRepetitions + ".";
            screenController = GameObject.Find("Scene/Screen").GetComponent<ScreenController>();
            UpdateScreen(practiceType);
            StartCoroutine(runVRPractice());

        }
        else
        {
            Debug.LogError("Variable practiceType must be defined.");
        }

    }

    IEnumerator runVRPractice()
    {

        for (int repetition = 0; repetition < nRepetitions; repetition++)
        {
            startTime = Time.time;
            stage = "Hand Positioning";

            yield return StartCoroutine(positionHands());

            endTime = Time.time;

            yield return StartCoroutine(saveData(repetition));

            startTime = Time.time;
            stage = "Bubble Popping";
        
            yield return StartCoroutine(bubble());

            endTime = Time.time;

            yield return StartCoroutine(saveData(repetition));

            startTime = Time.time;
            stage = "Pick and Place";

            yield return StartCoroutine(pickNplace());

            endTime = Time.time;

            yield return StartCoroutine(saveData(repetition));

            yield return StartCoroutine(showStatus(repetition));

            screenController.SetText((repetition+1).ToString(),24);

        }

        EndSound.Play();
        yield return StartCoroutine(ContinueAfterSound());

        Destroy(platform);
        pinchControl.enableUpdate = false;
        pinchControl.x = 0.0f;
        userHand.GetComponent<Animator>().SetFloat("Blend", 0.0f);

        // Wait for the end of the frame
        yield return null;

        userHand.SetActive(false);

        experimentManager.nextStage();
    }

    //void OnDestroy()
    //{
    //    userHand.SetActive(false);
    //}

    IEnumerator positionHands()
    {
        Vector3 newHandIniPos = new Vector3(handIniPos.x, handIniPos.y + experimentManager.TableHeight, handIniPos.z); // adjust position according to table height
        handIni = Instantiate(Resources.Load<GameObject>("Prefabs/Close_Pinch"), newHandIniPos, handIniRot);
        handIni.GetComponentInChildren<SkinnedMeshRenderer>().material = (Material)Resources.Load("Materials/Clear", typeof(Material));

        timeInPosition = 0f;
        while(true)
        {
        // Check if the player is within the detection radius of the target position
        float distanceToTarget = Vector3.Distance(userHand.transform.position, handIni.transform.position);
        
            if ((distanceToTarget <= detectionRadius) & pinchControl.x < 0.1f)
            {
                // If the player is within range, start counting time
                timeInPosition += Time.deltaTime;
                // If the required time is reached, change the color of the object
                if (timeInPosition >= requiredStayTime)
                {
                    Destroy(handIni);
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

    IEnumerator bubble()
    {
        inBubbleStage = true;

        Bounds bounds = spawnVolume.bounds;

        Vector3 spawnPosition = new Vector3(
            UnityEngine.Random.Range(bounds.min.x, bounds.max.x),
            UnityEngine.Random.Range(bounds.min.y + experimentManager.TableHeight, bounds.max.y + experimentManager.TableHeight),
            UnityEngine.Random.Range(bounds.min.z, bounds.max.z)
        );

        // Instantiate the bubble at the random position
        GameObject bubble = Instantiate(bubblePrefab, spawnPosition, Quaternion.identity);

        // Randomly choose between the two conditions
        if (UnityEngine.Random.value < 0.5f) // Random.value returns a float between 0.0 and 1.0
        {
            bubble.tag = "R_IndexTip";
            bubble.GetComponent<Renderer>().material = material1;
        }
        else
        {
            bubble.tag = "R_ThumbTip";
            bubble.GetComponent<Renderer>().material = material2;
        }

        Bubble bubbleCtr = bubble.GetComponent<Bubble>();

        while(!bubbleCtr.popped)
        {
            yield return null;
        }
        inBubbleStage = false;
    }

    IEnumerator pickNplace()
    {
        while(!platformCtr.objectInPlatform)
        {
            yield return null;
        }
        platformCtr.objectInPlatform = false;
    }

    IEnumerator saveData(int repetition)
    {
        // Create a new trial data object
        RepetitionData repetitionData = new RepetitionData
        {
            Timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            PracticeType = practiceType.ToString(),
            Repetition = repetition+1,
            Stage = stage,
            StartTime = startTime,
            EndTime = endTime
        };

        // Write the trial data to the CSV file
        using (StreamWriter sw = new StreamWriter(filePath, true))
        {
            sw.WriteLine(repetitionData.ToString());
        }

        yield return null;
    }

    IEnumerator showStatus(int repetition)
    {
        experimentManager.Repetition = repetition+1 + "  out of " + nRepetitions + ".";
        yield return null;
    }

    IEnumerator ContinueAfterSound() 
    {   
        // Wait until the sound is played
        while (EndSound.isPlaying) 
        {
            yield return null;
        }
    } 

    void Update()
    {
        if(inBubbleStage)
        {
            if (pinchControl.x > 0.6f)
            {
                // Turn on the spheres
                indexSphere.SetActive(true);
                thumbSphere.SetActive(true);
                isAble2pinch = true;
            }
            else
            {
                // Turn off the spheres
                indexSphere.SetActive(false);
                thumbSphere.SetActive(false);
                isAble2pinch = false;
            }
        }
        else
        {
            // Turn off the spheres
            indexSphere.SetActive(false);
            thumbSphere.SetActive(false);
            isAble2pinch = false;
        } 
    }
    void UpdateScreen(practiceOptions type)
    {
        if (type.ToString() == "primary")
        {
            text = "Now, let's play!.\n\n" +

            "Match the pose and position of the hand in front of you.\n\n" +

            "Pop the bubble with your index finger or thumb based on its color.\n\n" +

            "Note that you need to open the pinch to be able to pop the bubbles.\n\n" +

            "Finally, move the block to the yellow platform in front of you.\n\n" + 
            
            "We are going to repeat this " + nRepetitions_primaryPrac + " times.";
        }
        else if (type.ToString() == "refresher")
        {
            text = "Let's do " + nRepetitions_refresherPrac + " more repetitions.";
        }
        else if (type.ToString() == "debrief")
        {
            text = "We are almost there.!\n\n" +
            
            "We just need to do " + nRepetitions_debriefPrac + " more repetitions.";
        }
                    
        screenController.SetText(text,3);
    }
}
