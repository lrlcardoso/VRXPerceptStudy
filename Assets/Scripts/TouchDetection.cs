using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.IO.Ports;

public class TouchDetection : MonoBehaviour
{
    private GameObject pointerID;
    private HapticControl comSetup; 
    protected SerialPort comPort;
    private byte[] thumb_touch = new byte[] { 0x31 };
    private byte[] index_touch = new byte[] { 0x32 };

    // Start is called before the first frame update
    void Start()
    {
        pointerID = GameObject.Find("Haptic Control");
        comSetup = pointerID.GetComponent<HapticControl>();
        comPort = comSetup.comPort;
    }

    void OnTriggerEnter(Collider other)
    {
        HapticControl();
    }

    void OnTriggerExit(Collider other)
    {
        HapticControl();
    }

    void HapticControl(){

        if(gameObject.name == "R_IndexTip")
        {
            comPort.Write(index_touch, 0, index_touch.Length);
        } 
        else
        {
            comPort.Write(thumb_touch, 0, thumb_touch.Length);
        }
    }   
}
