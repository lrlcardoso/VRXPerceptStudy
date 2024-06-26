using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class GrabManager : MonoBehaviour
{
    private Collider collided;
    private GameObject TipRightThumb;
    private GameObject TipRightIndex;
    private TouchDetection thumbContact;
    private TouchDetection indexContact;
    private Material grabbedObject;
    private Material previousColour;
    float dist = 0.0f;
    float size = 0.0f;
    float multi1 = 1.1f;
    float multi2 = 1.3f;
    //float multi = 1.3f;
    public bool isGrabbed = false;

    
    void Start()
    {
        grabbedObject = (Material)Resources.Load("Materials/GrabbedObject", typeof(Material));
        previousColour = (Material)Resources.Load("Materials/metallic", typeof(Material));
        TipRightIndex = GameObject.Find("Rig/Camera Offset/RightHand/R_Wrist/R_IndexMetacarpal/R_IndexProximal/R_IndexIntermediate/R_IndexDistal/R_IndexTip");
        TipRightThumb = GameObject.Find("Rig/Camera Offset/RightHand/R_Wrist/R_ThumbMetacarpal/R_ThumbProximal/R_ThumbDistal/R_ThumbTip");
        indexContact = GameObject.Find("Rig/Camera Offset/RightHand/R_Wrist/R_IndexMetacarpal/R_IndexProximal/R_IndexIntermediate/R_IndexDistal/R_IndexTip").GetComponent<TouchDetection>();
        thumbContact = GameObject.Find("Rig/Camera Offset/RightHand/R_Wrist/R_ThumbMetacarpal/R_ThumbProximal/R_ThumbDistal/R_ThumbTip").GetComponent<TouchDetection>();
    }   

    void Update()
    {
        if(isGrabbed)
        {
            dist = Vector3.Distance(TipRightIndex.transform.position, TipRightThumb.transform.position);
            //if(dist>(size*multi)) 
            if(dist>(size*multi2))
            {
                isGrabbed = false;
                collided.transform.parent = null; 
                collided.gameObject.GetComponent<Rigidbody>().isKinematic = false;
                collided.gameObject.GetComponent<Renderer>().sharedMaterial = previousColour;
            }
        }
        ///*
        else
        {
            if(indexContact.indexContact && thumbContact.thumbContact)
            {
                collided = indexContact.collided;
                size = collided.gameObject.transform.localScale.x;
                dist = Vector3.Distance(TipRightIndex.transform.position, TipRightThumb.transform.position);
                //Debug.Log("Size: " + size + " and Distance: " + dist);
                

                if(dist<(size*multi2) && dist>(size*multi1))
                {
                    if(!isGrabbed)
                    {
                        isGrabbed = true;

                        collided.gameObject.GetComponent<Rigidbody>().isKinematic = true;
                        collided.gameObject.GetComponent<Renderer>().sharedMaterial = grabbedObject;

                        collided.transform.SetParent(gameObject.transform);
                        //Debug.Log(collided);
                        //Debug.Log(gameObject);
                        //Debug.Log(gameObject.transform);
                    }
                }
            }
        }
        //*/
    }

    /*
    void OnTriggerStay(Collider other)
    {

        if(other.gameObject.tag == "object2move")
        {
            size = other.gameObject.transform.localScale.x;
            dist = Vector3.Distance(TipRightIndex.transform.position, TipRightThumb.transform.position);
            //Debug.Log("Size: " + size + " and Distance: " + dist);

            if(dist<(size*multi) && dist>(size))
            {
                if(!isGrabbed)
                {
                    isGrabbed = true;
                    collided = other;

                    other.gameObject.GetComponent<Rigidbody>().isKinematic = true;
                    other.gameObject.GetComponent<Renderer>().sharedMaterial = grabbedObject;

                    other.transform.SetParent(gameObject.transform);
                    //Debug.Log(gameObject);
                    //Debug.Log(gameObject.transform);
                }
            }
        }
    }
    */
    
    
}