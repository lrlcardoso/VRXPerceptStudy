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


    void Start()
    {
        pointerID = GameObject.Find("Haptic Control");
        hapticControl = pointerID.GetComponent<HapticControl>();
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
