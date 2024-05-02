using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Hands;

public class UpdatePose : MonoBehaviour
{
    XRHandSubsystem m_HandSubsystem;
    public Animator handAnimator; 
    
    private float x = 0.0f;

    // Update is called once per frame
    void Update()
    {

        //CheckKeyKeyboard();
        handAnimator.SetFloat("Blend", x);

        if (m_HandSubsystem != null && m_HandSubsystem.running)
            return;
        
        var handSubsystems = new List<XRHandSubsystem>();
        SubsystemManager.GetSubsystems(handSubsystems);

        for (var i = 0; i < handSubsystems.Count; ++i)
        {
            var handSubsystem = handSubsystems[i];

            if (handSubsystem.running)
            {
                m_HandSubsystem = handSubsystem;
                break;
            }
        }

        if (m_HandSubsystem != null)
            m_HandSubsystem.updatedHands += OnUpdatedHands;

    }
        


    void OnUpdatedHands(XRHandSubsystem subsystem,
        XRHandSubsystem.UpdateSuccessFlags updateSuccessFlags,
        XRHandSubsystem.UpdateType updateType)
    {
        
        var indexTip = subsystem.rightHand.GetJoint(XRHandJointIDUtility.FromIndex(XRHandJointID.IndexTip.ToIndex()));
        var thumbTip = subsystem.rightHand.GetJoint(XRHandJointIDUtility.FromIndex(XRHandJointID.ThumbTip.ToIndex()));

        indexTip.TryGetPose(out Pose poseIndex);
        thumbTip.TryGetPose(out Pose poseThumb);


        //if (indexTip.TryGetPose(out Pose pose))
        //{
            x = Vector3.Distance(poseIndex.position, poseThumb.position)/0.17f;
            print("Distance: " + x);
        //}

    }

    private void CheckKeyKeyboard()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            x=x+0.1f;
        }
    }
}
