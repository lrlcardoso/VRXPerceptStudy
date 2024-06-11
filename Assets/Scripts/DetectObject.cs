using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class DetectObject : MonoBehaviour
{

    public bool objectInPlatform = false;
    private TouchDetection indexTouch;
    private TouchDetection thumbTouch;
    AudioSource successSound; // Sound to play on collision

    void Awake() 
    {
        successSound = GetComponent<AudioSource>();
    }

    void Start()
    {
        indexTouch = GameObject.Find("Rig/Camera Offset/RightHand/R_Wrist/R_IndexMetacarpal/R_IndexProximal/R_IndexIntermediate/R_IndexDistal/R_IndexTip").GetComponent<TouchDetection>();
        thumbTouch = GameObject.Find("Rig/Camera Offset/RightHand/R_Wrist/R_ThumbMetacarpal/R_ThumbProximal/R_ThumbDistal/R_ThumbTip").GetComponent<TouchDetection>();
    }

    void OnCollisionEnter(Collision collision) {

        if(collision.gameObject.tag == "object2move")
        {
            objectInPlatform = true;
            successSound.Play();
            Destroy(collision.gameObject);

            if (indexTouch.indexON)
            {
                indexTouch.HapticControl("R_IndexTip");
            }
            else if (thumbTouch.thumbON)
            {
                thumbTouch.HapticControl("R_ThumbTip");
            }
        }            
    } 
}
