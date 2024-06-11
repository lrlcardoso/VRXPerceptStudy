using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class GrabManager : MonoBehaviour
{
    private Collider collided;
    private GameObject TipRightThumb;
    private GameObject TipRightIndex;
    private Material grabbedObject;
    private Material previousColour;
    float dist = 0.0f;
    float size = 0.0f;
    float multi = 1.65f;
    float min_size = 0.019f;
    bool isGrabbed = false;

    
    void Start()
    {
        grabbedObject = (Material)Resources.Load("Materials/GrabbedObject", typeof(Material));
        previousColour = (Material)Resources.Load("Materials/metallic", typeof(Material));
        TipRightIndex = GameObject.Find("Rig/Camera Offset/RightHand/R_Wrist/R_IndexMetacarpal/R_IndexProximal/R_IndexIntermediate/R_IndexDistal/R_IndexTip");
        TipRightThumb = GameObject.Find("Rig/Camera Offset/RightHand/R_Wrist/R_ThumbMetacarpal/R_ThumbProximal/R_ThumbDistal/R_ThumbTip");
    }   

    void Update()
    {
        if(isGrabbed)
        {
            dist = Vector3.Distance(TipRightIndex.transform.position, TipRightThumb.transform.position);
            if(dist>(size*multi))
            {
                isGrabbed = false;
                collided.transform.parent = null; 
                collided.gameObject.GetComponent<Rigidbody>().isKinematic = false;
                collided.gameObject.GetComponent<Renderer>().sharedMaterial = previousColour;
            }
        }
    }
    void OnTriggerStay(Collider other)
    {
        if(other.gameObject.tag == "object2move")
        {
            size = other.gameObject.transform.localScale.x;
            dist = Vector3.Distance(TipRightIndex.transform.position, TipRightThumb.transform.position);

            if(dist<(size*multi) && dist>(min_size*multi))
            {
                if(!isGrabbed)
                {
                    isGrabbed = true;
                    collided = other;

                    other.gameObject.GetComponent<Rigidbody>().isKinematic = true;
                    other.gameObject.GetComponent<Renderer>().sharedMaterial = grabbedObject;

                    other.transform.SetParent(gameObject.transform);
                }
            }
        }
    }
}