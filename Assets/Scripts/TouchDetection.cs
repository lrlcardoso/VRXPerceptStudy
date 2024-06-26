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
    byte[] msg;
    public bool indexON = false;
    public bool thumbON = false;
    public bool indexContact = false;
    public bool thumbContact = false;
    public Collider collided;

    // Start is called before the first frame update
    void Start()
    {
        pointerID = GameObject.Find("Haptic Control");
        comSetup = pointerID.GetComponent<HapticControl>();
        comPort = comSetup.comPort;
    }

    void OnTriggerEnter(Collider other)
    {
        if(gameObject.name == "R_IndexTip" | gameObject.name == "R_ThumbTip" && other.tag != "VolumeController" && other.tag != "platform" && other.tag != "TableSurface")
        {
            HapticControl(gameObject.name);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if(gameObject.name == "R_IndexTip" | gameObject.name == "R_ThumbTip" && other.tag != "VolumeController" && other.tag != "platform" && other.tag != "TableSurface")
        {
            HapticControl(gameObject.name);
        }

        if(other.gameObject.tag == "object2move")
        {
            if(gameObject.name == "R_IndexTip")
            {
                indexContact = false;
            }
            else if(gameObject.name == "R_ThumbTip")
            {
                thumbContact = false;
            }
        }  
    }

    public void HapticControl(string whatTouched){

        switch (whatTouched)
        {
            case "R_IndexTip":
                msg = index_touch;
                indexON = !indexON;
                break;
            case "R_ThumbTip":
                msg = thumb_touch;
                thumbON = !thumbON;
                break;
        }
        comPort.Write(msg, 0, msg.Length);
    }  

    void OnTriggerStay(Collider other)
    {
        if(other.gameObject.tag == "object2move")
        {
            collided = other;
            if(gameObject.name == "R_IndexTip")
            {
                indexContact = true;
            }
            else if(gameObject.name == "R_ThumbTip")
            {
                thumbContact = true;
            }
        }  
    }

}
