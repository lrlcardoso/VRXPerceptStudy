using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoseCalibration : MonoBehaviour
{
    private GameObject userHand;

    // Start is called before the first frame update
    void Start()
    {
        userHand = GameObject.Find("Rig/Camera Offset/RightHand");
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            Debug.Log(userHand.transform.position);
            Debug.Log(userHand.transform.rotation.eulerAngles);
        }
        
    }
}
