using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UpdatePose : MonoBehaviour
{

    public Animator handAnimator; 
    
    private float x = 0.0f;

    // Update is called once per frame
    void Update()
    {

        CheckKeyKeyboard();
        handAnimator.SetFloat("Blend", x);
        
    }

    private void CheckKeyKeyboard()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            x=x+0.1f;
        }
    }
}
