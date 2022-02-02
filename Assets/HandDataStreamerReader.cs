using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using Unity.Netcode;

//using  Unity.Netcode.Serialization.Pooled;

//https://www.youtube.com/watch?v=lBzwUKQ3tbw

public class HandDataStreamerReader : NetworkBehaviour, OVRSkeleton.IOVRSkeletonDataProvider,
    OVRSkeletonRenderer.IOVRSkeletonRendererDataProvider,
    OVRMesh.IOVRMeshDataProvider,
    OVRMeshRenderer.IOVRMeshRendererDataProvider {
    public OVRSkeleton HandSkeleton;
    private OVRBone[] HandBones;

    [SerializeField] private OVRPlugin.SkeletonType HandType = OVRPlugin.SkeletonType.None;

    private OVRSkeleton.IOVRSkeletonDataProvider _iovrSkeletonDataProviderImplementation;


    public Vector3 RootPos;
    public Quaternion RootRot;
    public float RootScale;
    public Quaternion[] BoneRotations;

    private float lastUpdate = 0;
    public float HandTimeout = 1;

    private bool IsDataValid = false;
    private bool ready = false;

    // Start is called before the first frame update
    void Start() {
        BoneRotations = new Quaternion[24];
        for (int i = 0; i < 24; i++) { BoneRotations[i] = new Quaternion(); }

        ready = true;
    }

    // Update is called once per frame
    void Update() {
        IsDataValid = true;
      // if (IsDataValid && Time.time < lastUpdate) { IsDataValid = false; }
    }

    public void GetNewData(NetworkSkeletonPoseData newRemoteHandData) {
       if(! ready) {
           return;
       }

       if (newRemoteHandData.HandType != (OVRPlugin.Hand) HandType) {

           Debug.Log("Data miss match left right hand aborting!!");
           return;
       }
      // Debug.Log("Should be a right hand");
      lastUpdate= Time.time+HandTimeout;
        IsDataValid = true;
       // Debug.Log("Should be a right hand1");
        RootPos = newRemoteHandData.RootPos;
        RootRot = newRemoteHandData.RootRot;
        RootScale = newRemoteHandData.RootScale;
      //  Debug.Log("Should be a right hand2");
        newRemoteHandData.BoneRotations.CopyTo(BoneRotations, 0);

    }


    OVRSkeleton.SkeletonType OVRSkeleton.IOVRSkeletonDataProvider.GetSkeletonType() {
       // Debug.Log("GetSkeletonType");
        switch (HandType) {
            case OVRPlugin.SkeletonType.HandLeft:
                return OVRSkeleton.SkeletonType.HandLeft;

            case OVRPlugin.SkeletonType.HandRight:
                return OVRSkeleton.SkeletonType.HandRight;
            case OVRPlugin.SkeletonType.None:
            default:
                return OVRSkeleton.SkeletonType.None;
        }
    }

    OVRSkeleton.SkeletonPoseData OVRSkeleton.IOVRSkeletonDataProvider.GetSkeletonPoseData() {
       // Debug.Log("GetSkeletonPoseData Only right hand ??");
        var data = new OVRSkeleton.SkeletonPoseData();

        data.IsDataValid = IsDataValid;
        if (IsDataValid) {
            data.RootPose = new OVRPlugin.Posef() {Orientation = RootRot.ToQuatf(), Position = RootPos.ToVector3f()};
            data.RootScale = RootScale;
            data.BoneRotations = Array.ConvertAll(BoneRotations, s => s.ToQuatf());
            data.IsDataHighConfidence =
                true; // this is obviusly not communicate but we do not send data if thats false.
        }

        // Debug.Log("Sending Skelton Data" + IsDataValid);
        return data;
    }
    public OVRSkeleton.SkeletonPoseData GetSkeletonPoseData() {
        // Debug.Log("GetSkeletonPoseData");
        var data = new OVRSkeleton.SkeletonPoseData();

        data.IsDataValid = IsDataValid;
        if (IsDataValid) {
            data.RootPose = new OVRPlugin.Posef() {Orientation = RootRot.ToQuatf(), Position = RootPos.ToVector3f()};
            data.RootScale = RootScale;
            data.BoneRotations = Array.ConvertAll(BoneRotations, s => s.ToQuatf());
            data.IsDataHighConfidence =
                true; // this is obviusly not communicate but we do not send data if thats false.
        }

        // Debug.Log("Sending Skelton Data" + IsDataValid);
        return data;
    }

    OVRSkeletonRenderer.SkeletonRendererData OVRSkeletonRenderer.IOVRSkeletonRendererDataProvider.
        GetSkeletonRendererData() {
     //   Debug.Log("GetSkeletonRendererData");
        var data = new OVRSkeletonRenderer.SkeletonRendererData();

        data.IsDataValid = IsDataValid;
        if (IsDataValid) {
            data.RootScale = RootScale;
            data.IsDataHighConfidence = true;
            data.ShouldUseSystemGestureMaterial = false; // no idea tbh
        }

        return data;
    }

    OVRMesh.MeshType OVRMesh.IOVRMeshDataProvider.GetMeshType() {
       // Debug.Log("GetMeshType");
        switch (HandType) {
            case OVRPlugin.SkeletonType.None:
                return OVRMesh.MeshType.None;
            case OVRPlugin.SkeletonType.HandLeft:
                return OVRMesh.MeshType.HandLeft;
            case OVRPlugin.SkeletonType.HandRight:
                return OVRMesh.MeshType.HandRight;
            default:
                return OVRMesh.MeshType.None;
        }
    }

    OVRMeshRenderer.MeshRendererData OVRMeshRenderer.IOVRMeshRendererDataProvider.GetMeshRendererData() {
       // Debug.Log("GetMeshRendererData");
        var data = new OVRMeshRenderer.MeshRendererData();

        data.IsDataValid = IsDataValid;
        if (IsDataValid) {
            data.IsDataHighConfidence = true;
            data.ShouldUseSystemGestureMaterial = false; // again no idea lol
        }

        return data;
    }
}
