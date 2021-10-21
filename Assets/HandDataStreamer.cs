using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAPI;
using MLAPI.Serialization.Pooled;

//https://www.youtube.com/watch?v=lBzwUKQ3tbw

public class HandDataStreamer : MonoBehaviour, OVRSkeleton.IOVRSkeletonDataProvider,
    OVRSkeletonRenderer.IOVRSkeletonRendererDataProvider,
    OVRMesh.IOVRMeshDataProvider,
    OVRMeshRenderer.IOVRMeshRendererDataProvider {
    public OVRSkeleton HandSkeleton;
    private OVRBone[] HandBones;
    private OVRHand.Hand HandType = OVRHand.Hand.None;
    private OVRPlugin.HandState _handState = new OVRPlugin.HandState();

    public OVRSkeleton FakeRemoteHand;

    private OVRSkeleton.IOVRSkeletonDataProvider _iovrSkeletonDataProviderImplementation;

    // Start is called before the first frame update
    void Start() {
       
    }

    // Update is called once per frame
    void Update() {
       // HandSkeleton.Bones.CopyTo(HandBones,0);

       // FakeRemoteHand.Bones = new List<OVRBone>(HandBones);
       // FakeRemoteHand.

    }

    OVRSkeleton.SkeletonType OVRSkeleton.IOVRSkeletonDataProvider.GetSkeletonType()
    {
        switch (HandType)
        {
            case OVRHand.Hand.HandLeft:
                return OVRSkeleton.SkeletonType.HandLeft;
            case OVRHand.Hand.HandRight:
                return OVRSkeleton.SkeletonType.HandRight;
            case OVRHand.Hand.None:
            default:
                return OVRSkeleton.SkeletonType.None;
        }
    }
    OVRSkeleton.SkeletonPoseData OVRSkeleton.IOVRSkeletonDataProvider.GetSkeletonPoseData()
    {
        var data = new OVRSkeleton.SkeletonPoseData();
/*
        data.IsDataValid = HandSkeleton.IsDataValid;
        if (HandSkeleton.IsDataValid)
        {
            data.RootPose = HandSkeleton. _handState.RootPose;
            data.RootScale = _handState.HandScale;
            data.BoneRotations = _handState.BoneRotations;
            data.IsDataHighConfidence = IsTracked && HandConfidence == TrackingConfidence.High;
        }
*/
        return data;
    }

    public OVRSkeletonRenderer.SkeletonRendererData GetSkeletonRendererData() { throw new System.NotImplementedException(); }

    public OVRMesh.MeshType GetMeshType() { throw new System.NotImplementedException(); }

    public OVRMeshRenderer.MeshRendererData GetMeshRendererData() { throw new System.NotImplementedException(); }
}
