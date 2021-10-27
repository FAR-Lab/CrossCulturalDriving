using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using  Unity.Netcode;
//using  Unity.Netcode.Serialization.Pooled;

//https://www.youtube.com/watch?v=lBzwUKQ3tbw

public class HandDataStreamer : MonoBehaviour, OVRSkeleton.IOVRSkeletonDataProvider,
    OVRSkeletonRenderer.IOVRSkeletonRendererDataProvider,
    OVRMesh.IOVRMeshDataProvider,
    OVRMeshRenderer.IOVRMeshRendererDataProvider {
    public OVRSkeleton HandSkeleton;
    private OVRBone[] HandBones;
    private OVRPlugin.Hand HandType = OVRPlugin.Hand.None;
    private OVRPlugin.HandState _handState = new OVRPlugin.HandState();

    public OVRSkeleton FakeRemoteHand;

    private OVRSkeleton.IOVRSkeletonDataProvider _iovrSkeletonDataProviderImplementation;
    private bool IsSystemGestureInProgress;
    private bool IsDominantHand;
    private bool IsPointerPoseValid;
    private bool IsTracked;
    private float HandScale;
    private OVRPlugin.TrackingConfidence HandConfidence;
    private bool IsDataValid;
    private bool IsDataHighConfidence;

    // Start is called before the first frame update
    void Start() {
       
    }

    // Update is called once per frame
    void Update() {
       // HandSkeleton.Bones.CopyTo(HandBones,0);

       // FakeRemoteHand.Bones = new List<OVRBone>(HandBones);
       // FakeRemoteHand.
       GetHandState(OVRPlugin.Step.Render);
    }
    private void GetHandState(OVRPlugin.Step step) {
        
        if (OVRPlugin.GetHandState(step, (OVRPlugin.Hand)HandType, ref _handState))
        {
            IsTracked = (_handState.Status & OVRPlugin.HandStatus.HandTracked) != 0;
            IsSystemGestureInProgress = (_handState.Status & OVRPlugin.HandStatus.SystemGestureInProgress) != 0;
            IsPointerPoseValid = (_handState.Status & OVRPlugin.HandStatus.InputStateValid) != 0;
            IsDominantHand = (_handState.Status & OVRPlugin.HandStatus.DominantHand) != 0;
          //  PointerPose.localPosition = _handState.PointerPose.Position.FromFlippedZVector3f();
           // PointerPose.localRotation = _handState.PointerPose.Orientation.FromFlippedZQuatf();
            HandScale = _handState.HandScale;
            HandConfidence = (OVRPlugin.TrackingConfidence)_handState.HandConfidence;

            IsDataValid = true;
            IsDataHighConfidence = IsTracked && HandConfidence == OVRPlugin.TrackingConfidence.High;
        }
        else
        {
            IsTracked = false;
            IsSystemGestureInProgress = false;
            IsPointerPoseValid = false;
           // PointerPose.localPosition = Vector3.zero;
           // PointerPose.localRotation = Quaternion.identity;
            HandScale = 1.0f;
            HandConfidence = OVRPlugin.TrackingConfidence.Low;

            IsDataValid = false;
            IsDataHighConfidence = false;
        }
    }

 


    OVRSkeleton.SkeletonType OVRSkeleton.IOVRSkeletonDataProvider.GetSkeletonType()
     	{
     		switch (HandType)
     		{
     		case OVRPlugin.Hand.HandLeft:
     			return OVRSkeleton.SkeletonType.HandLeft;
     		case OVRPlugin.Hand.HandRight:
     			return OVRSkeleton.SkeletonType.HandRight;
     		case OVRPlugin.Hand.None:
     		default:
     			return OVRSkeleton.SkeletonType.None;
     		}
     	}
     
     	OVRSkeleton.SkeletonPoseData OVRSkeleton.IOVRSkeletonDataProvider.GetSkeletonPoseData()
     	{
     		var data = new OVRSkeleton.SkeletonPoseData();
     
     		data.IsDataValid = IsDataValid;
     		if (IsDataValid)
     		{
     			data.RootPose = _handState.RootPose;
     			data.RootScale = _handState.HandScale;
     			data.BoneRotations = _handState.BoneRotations;
     			data.IsDataHighConfidence = IsTracked && HandConfidence == OVRPlugin.TrackingConfidence.High;
     		}
     
     		return data;
     	}
     
     	OVRSkeletonRenderer.SkeletonRendererData OVRSkeletonRenderer.IOVRSkeletonRendererDataProvider.GetSkeletonRendererData()
     	{
     		var data = new OVRSkeletonRenderer.SkeletonRendererData();
     
     		data.IsDataValid = IsDataValid;
     		if (IsDataValid)
     		{
     			data.RootScale = _handState.HandScale;
     			data.IsDataHighConfidence = IsTracked && HandConfidence == OVRPlugin.TrackingConfidence.High;
     			data.ShouldUseSystemGestureMaterial = IsSystemGestureInProgress;
     		}
     
     		return data;
     	}
     
     	OVRMesh.MeshType OVRMesh.IOVRMeshDataProvider.GetMeshType()
     	{
     		switch (HandType)
     		{
     		case OVRPlugin.Hand.None:
     			return OVRMesh.MeshType.None;
     		case OVRPlugin.Hand.HandLeft:
     			return OVRMesh.MeshType.HandLeft;
     		case OVRPlugin.Hand.HandRight:
     			return OVRMesh.MeshType.HandRight;
     		default:
     			return OVRMesh.MeshType.None;
     		}
     	}
     
     	OVRMeshRenderer.MeshRendererData OVRMeshRenderer.IOVRMeshRendererDataProvider.GetMeshRendererData()
     	{
     		var data = new OVRMeshRenderer.MeshRendererData();
     
     		data.IsDataValid = IsDataValid;
     		if (IsDataValid)
     		{
     			data.IsDataHighConfidence = IsTracked && HandConfidence == OVRPlugin.TrackingConfidence.High;
     			data.ShouldUseSystemGestureMaterial = IsSystemGestureInProgress;
     		}
     
     		return data;
     	}
}
