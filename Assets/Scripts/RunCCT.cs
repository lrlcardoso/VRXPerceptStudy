using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.IO.Ports;

public class RunCCT : MonoBehaviour
{

    private GameObject pointerID;
    private HapticControl hapticControl; 
    private byte[] thumbShoulder_CCT = new byte[] { 0x33 };
    private byte[] indexShoulder_CCT = new byte[] { 0x34 };
    private byte[] thumbHand_CCT = new byte[] { 0x35 };
    private byte[] indexHand_CCT = new byte[] { 0x36 };
    int nTrials = 8;         
    int nTrials_noGo = 4;  
    char thumb = 'T';
    char finger = 'F';
    char noGo = 'N';


    void Start()
    {
        pointerID = GameObject.Find("Haptic Control");
        hapticControl = pointerID.GetComponent<HapticControl>();

        // Create a 2D array to store the trials
        char[,] trials = new char[2, nTrials + nTrials_noGo];

        // Calculate the number of trials for each category
        int nCongruentThumb = nTrials / 4;
        int nCongruentFinger = nTrials / 4;
        int nIncongruentThumbFinger = nTrials / 4;
        int nIncongruentFingerThumb = nTrials / 4;
        int nThumbNoGo = nTrials_noGo / 2;
        int nFingerNoGo = nTrials_noGo / 2;

        // Add congruent trials thumb-thumb
        AddTrials(trials, thumb, thumb, 0, nCongruentThumb);

        // Add congruent trials finger-finger
        AddTrials(trials, finger, finger, nCongruentThumb, nCongruentFinger);

        // Add incongruent trials thumb-finger
        AddTrials(trials, thumb, finger, 2 * nCongruentThumb, nIncongruentThumbFinger);

        // Add incongruent trials finger-thumb
        AddTrials(trials, finger, thumb, 3 * nCongruentThumb, nIncongruentFingerThumb);

        // Add noGo trials, thumb-N
        AddTrials(trials, thumb, noGo, nTrials, nThumbNoGo);

        // Add noGo trials, finger-N
        AddTrials(trials, finger, noGo, nTrials + nThumbNoGo, nFingerNoGo);

        // Shuffle the trials using Fisher-Yates algorithm
        Shuffle(trials, nTrials + nTrials_noGo);

        // Display the results
        PrintResults(trials);

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
    void Update()
    {
        if (Input.GetKeyDown("space"))
        {
            hapticControl.comPort.Write(thumbShoulder_CCT, 0, thumbShoulder_CCT.Length);
            
        }

        if (hapticControl.msgReceived)
        {
            Debug.Log(hapticControl.msg);
            hapticControl.msgReceived = false;
        }  
    }
}
