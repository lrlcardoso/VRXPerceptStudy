// Developed by: Lucas Cardoso
// First version: 29/April/2024
// Latest release: 29/April/2024
// Description: This script need to be attached to the GameObject with the hand model (right or left). 
//              This script tracks the user's hand and updates the avatar's hand continuously. 

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Hands;

public class WristTracking : MonoBehaviour
{
    XRHandSubsystem m_HandSubsystem;


    void Update()
    {
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
        UpdateJoint(subsystem, XRHandJointID.Wrist);
        //UpdateJoint(subsystem, XRHandJointID.Palm);
    }

    void UpdateJoint(XRHandSubsystem subsystem, XRHandJointID joint)
    {

        var trackingData = subsystem.rightHand.GetJoint(XRHandJointIDUtility.FromIndex(joint.ToIndex()));

        if (trackingData.id == XRHandJointID.Invalid)
            return;

        if (!trackingData.TryGetPose(out var pose))
            return;

        gameObject.transform.localPosition = pose.position;
        gameObject.transform.localRotation = pose.rotation;

    }
}
