using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.XR.Hands;

#if BURST_PRESENT
using Unity.Burst;
#endif
public class XRHandsSender : MonoBehaviour, ISerializationCallbackReceiver
{
    /// <summary>
    /// The array of joint local poses indexed by the <see cref="XRHandJointID"/> which is updated by the method
    /// <see cref="UpdateJointLocalPoses"/> and then applied to the joint transforms by the
    /// method <see cref="ApplyUpdatedTransformPoses"/>.
    /// </summary>
    protected NativeArray<Pose> m_JointLocalPoses;
    
    
    [SerializeField]
    [Tooltip("The XR Hand Tracking Events component that will be used to subscribe to hand tracking events.")]
    XRHandTrackingEvents m_XRHandTrackingEvents;

    
    
    /// <summary>
    /// The <see cref="XRHandTrackingEvents"/> component that will be the source of hand tracking events for this driver.
    /// </summary>
    public XRHandTrackingEvents handTrackingEvents
    {
        get => m_XRHandTrackingEvents;
        set
        {
            if (Application.isPlaying)
                UnsubscribeFromHandTrackingEvents();

            m_XRHandTrackingEvents = value;

            if (Application.isPlaying && isActiveAndEnabled)
                SubscribeToHandTrackingEvents();
        }
    }
    
    
    void OnEnable()
    {
        m_JointLocalPoses = new NativeArray<Pose>(XRHandJointID.EndMarker.ToIndex(), Allocator.Persistent);

        if (m_XRHandTrackingEvents == null)
            TryGetComponent(out m_XRHandTrackingEvents);

        if (m_XRHandTrackingEvents == null)
        {
            Debug.LogError($"The {nameof(XRHandSkeletonDriver)} requires an {nameof(XRHandTrackingEvents)} component to subscribe to hand tracking events.", this);
            return;
        }

       

        SubscribeToHandTrackingEvents();
    }
    
    void UnsubscribeFromHandTrackingEvents()
    {
        if (m_XRHandTrackingEvents != null)
        {
            m_XRHandTrackingEvents.jointsUpdated.RemoveListener(OnJointsUpdated);
         //   m_XRHandTrackingEvents.poseUpdated.RemoveListener(OnRootPoseUpdated);
        }
    }
    
    void SubscribeToHandTrackingEvents()
    {
        if (m_XRHandTrackingEvents != null)
        {
            m_XRHandTrackingEvents.jointsUpdated.AddListener(OnJointsUpdated);
          //  m_XRHandTrackingEvents.poseUpdated.AddListener(OnRootPoseUpdated);
        }
    }
    
    
    void OnJointsUpdated(XRHandJointsUpdatedEventArgs args)
    {
        UpdateJointLocalPoses(args);

    }
    
   
     protected void UpdateJointLocalPoses(XRHandJointsUpdatedEventArgs args)
        {
            // Calculate the local poses for all the joints, accessing the internal joints array to enable burst compilation when available
            //CalculateJointTransformLocalPoses(ref args.hand.m_Joints, ref m_JointLocalPoses);
        }

#if BURST_PRESENT && UNITY_2022_1_OR_NEWER
        [BurstCompile]
#endif
        static void CalculateJointTransformLocalPoses(ref NativeArray<XRHandJoint> joints, ref NativeArray<Pose> jointLocalPoses)
        {
            var wristIndex = XRHandJointID.Wrist.ToIndex();
            if (joints[wristIndex].TryGetPose(out var wristJointPose))
            {
                jointLocalPoses[wristIndex] = wristJointPose;
                var palmIndex = XRHandJointID.Palm.ToIndex();

                if (joints[palmIndex].TryGetPose(out var palmJointPose))
                {
                    CalculateLocalTransformPose(wristJointPose, palmJointPose, out var palmPose);
                    jointLocalPoses[palmIndex] = palmPose;
                }

                for (var fingerIndex = (int)XRHandFingerID.Thumb;
                     fingerIndex <= (int)XRHandFingerID.Little;
                     ++fingerIndex)
                {
                    var parentPose = wristJointPose;
                    var fingerId = (XRHandFingerID)fingerIndex;

                    var jointIndexBack = fingerId.GetBackJointID().ToIndex();
                    var jointIndexFront = fingerId.GetFrontJointID().ToIndex();
                    for (var jointIndex = jointIndexFront;
                         jointIndex <= jointIndexBack;
                         ++jointIndex)
                    {
                        if (joints[jointIndex].TryGetPose(out var fingerJointPose))
                        {
                            CalculateLocalTransformPose(parentPose, fingerJointPose, out var jointLocalPose);
                            parentPose = fingerJointPose;
                            jointLocalPoses[jointIndex] = jointLocalPose;
                        }
                    }
                }
            }
        }

#if BURST_PRESENT
        [BurstCompile]
#endif
        static void CalculateLocalTransformPose(in Pose parentPose, in Pose jointPose, out Pose jointLocalPose)
        {
            var inverseParentRotation = Quaternion.Inverse(parentPose.rotation);
            jointLocalPose.position = inverseParentRotation * (jointPose.position - parentPose.position);
            jointLocalPose.rotation = inverseParentRotation * jointPose.rotation;
        }
    
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    

    public void OnBeforeSerialize() {
        
        
        
    }

    public void OnAfterDeserialize() {
        
        
    }
}
