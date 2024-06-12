using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class DetectObject : MonoBehaviour
{

    public bool objectInPlatform = false;
    private TouchDetection indexTouch;
    private TouchDetection thumbTouch;
    private GrabManager indexGrab;
    private GrabManager thumbGrab;
    public AudioSource successSound; // Sound to play on collision

    void Awake() 
    {
        successSound = GetComponent<AudioSource>();
    }

    void Start()
    {
        indexTouch = GameObject.Find("Rig/Camera Offset/RightHand/R_Wrist/R_IndexMetacarpal/R_IndexProximal/R_IndexIntermediate/R_IndexDistal/R_IndexTip").GetComponent<TouchDetection>();
        thumbTouch = GameObject.Find("Rig/Camera Offset/RightHand/R_Wrist/R_ThumbMetacarpal/R_ThumbProximal/R_ThumbDistal/R_ThumbTip").GetComponent<TouchDetection>();
        indexGrab = GameObject.Find("Rig/Camera Offset/RightHand/R_Wrist/R_IndexMetacarpal/R_IndexProximal/R_IndexIntermediate/R_IndexDistal/R_IndexTip").GetComponent<GrabManager>();
        thumbGrab = GameObject.Find("Rig/Camera Offset/RightHand/R_Wrist/R_ThumbMetacarpal/R_ThumbProximal/R_ThumbDistal/R_ThumbTip").GetComponent<GrabManager>();
    }

    void OnCollisionEnter(Collision collision) {

        if(collision.gameObject.tag == "object2move")
        {
            if(!indexGrab.isGrabbed && !thumbGrab.isGrabbed)
            {
                objectInPlatform = true;
                successSound.Play();
                Destroy(collision.gameObject);

                if (indexTouch.indexON)
                {
                    indexTouch.HapticControl("R_IndexTip");
                }
                if (thumbTouch.thumbON)
                {
                    thumbTouch.HapticControl("R_ThumbTip");
                }
            }
        }            
    }
}
