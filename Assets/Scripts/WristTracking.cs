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
        
        var trackingData = subsystem.rightHand.GetJoint(XRHandJointIDUtility.FromIndex(XRHandJointID.Wrist.ToIndex()));

        //Debug.Log(trackingData);

        if (trackingData.TryGetPose(out Pose pose))
        {
            gameObject.transform.localPosition = pose.position;
            gameObject.transform.localRotation = pose.rotation;
        }

    }
}
