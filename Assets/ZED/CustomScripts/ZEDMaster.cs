#if USING_ZED
using System;
using UnityEngine;
using UnityEngine.Serialization;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine.InputSystem;
using UnityEngine.PlayerLoop;


public class ZEDMaster : Interactable_Object {
   
    
    
    private List<ZEDSkeletonAnimator> zedAvatars;
    private int currentAvatarIndex = 0;

    
    [SerializeField] private GameObject storedTarget;
    private ZEDSkeletonAnimator targetAnimator;
    private ZEDBodyTrackingManager zedBodyTrackingManager;
    private Transform targetHip;
    
    private Pose destinationPose;

    private Transform CameraTrackingTransform;
 
    
    public override void OnNetworkSpawn()
    {
        
        base.OnNetworkSpawn();
        if (IsServer)
        {
            zedBodyTrackingManager =GetComponent<ZEDBodyTrackingManager>();
            destinationPose = new Pose(transform.position, transform.rotation);
            CameraTrackingTransform = transform;
        }
        else
        {
            GetComponent<ZEDBodyTrackingManager>().enabled = false;
            GetComponent<ZEDStreamingClient>().enabled = false;
            
        }
        Debug.Log($"Setting up syc reference");
           

    }

    private void Update()
    {
        if (IsServer)
        {
            //HandleMouseClick();
            CycleThroughAvatars();
        }
      
            HeadClientRef = GetCameraPositionObject();
            LeftClientRef = GetLeftHandRoot();
            RightClientRef = GetRightHandRoot();
            
        
    }

    private void LateUpdate() {
        if (IsServer && CameraTrackingTransform != null)
        {
            transform.position = CameraTrackingTransform.position;
        }
    }

    private void OnDisable()
    {  if (IsServer)
        {
            // destroy all avatars
            ZEDSkeletonAnimator[] avatars = GameObject.FindObjectsOfType<ZEDSkeletonAnimator>();
            foreach (var avatar in avatars)
            {
                Destroy(avatar.gameObject);
            }
        }
    }

    private void CycleThroughAvatars()
    {
        if (IsServer)
        {
            // if press right arrow, go to next avatar
            if (Keyboard.current.rightArrowKey.wasPressedThisFrame)
            {
                zedAvatars = new List<ZEDSkeletonAnimator>(FindObjectsOfType<ZEDSkeletonAnimator>());
                if (currentAvatarIndex >= zedAvatars.Count) return;
                   storedTarget = zedAvatars[currentAvatarIndex].gameObject;
                foreach (ZEDSkeletonAnimator zed in zedAvatars)
                {
                    SetMaterialsColor(zed.gameObject, Color.white);
                }

                SetMaterialsColor(storedTarget, Color.red);
                currentAvatarIndex++;
                if (currentAvatarIndex == zedAvatars.Count)
                {
                    currentAvatarIndex = 0;
                }
            }

            if (Keyboard.current.upArrowKey.wasPressedThisFrame)
            {
                StartCoroutine(CalibrateSequence());
            }
        }
    }

    public void e_StartCallibrationSequence() {
        StartCoroutine(CalibrateSequence());
    }
    
    private IEnumerator CalibrateSequence()
    {
        if (NetworkManager.Singleton.IsServer)
        {
           
            yield return new WaitUntil(()=> FindDependencies());
            
            
            yield return new WaitUntil(()=>CalibratePosition());
            yield return new WaitForEndOfFrame();
            yield return new WaitUntil(()=>CalibrateRotation());
            yield return new WaitForEndOfFrame();
            yield return new WaitUntil(()=>CalibratePosition());
            yield return new WaitForEndOfFrame();
            if (targetAnimator != null) {
                Debug.Log("Found a target, syncoironizing references");
                NetworkObject someNetworkObject = targetAnimator.GetComponent<NetworkObject>();
                AnimatorObjectNOID.Value = someNetworkObject.NetworkObjectId;
            }

            Debug.Log($"Finished Calibrating!");
             
        }
    }

   
    
    private bool FindDependencies()
    {
        // if there is only one avatar, assign it to storedTarget
        if (storedTarget == null)
        {
            ZEDSkeletonAnimator[] allAvatars = FindObjectsOfType<ZEDSkeletonAnimator>();
            if (allAvatars.Length == 1)
            {
                storedTarget = allAvatars[0].gameObject;
            }
            else
            {
                return false;
            }
        }

        zedBodyTrackingManager = GetComponent<ZEDBodyTrackingManager>();
        
        // get the current hip position of target
        targetAnimator = storedTarget.GetComponent<ZEDSkeletonAnimator>();
        targetHip = targetAnimator.animator.GetBoneTransform(HumanBodyBones.Hips);
        CameraTrackingTransform = targetAnimator.animator.GetBoneTransform(HumanBodyBones.Head);

        if (targetAnimator != null && targetHip != null && CameraTrackingTransform != null) {
            return true;
        }

        return false;
        

    }
    private bool CalibratePosition()
    {
        // calculate the difference vector
        if (destinationPose != null && targetHip != null) {
            Vector3 positionOffset = destinationPose.position - targetHip.position;
            // apply difference
            zedBodyTrackingManager.positionOffset += positionOffset;
            return true;
        }
        
            return false;
        
    }

    private bool CalibrateRotation()
    {
        if (destinationPose != null && targetHip != null) {
            // calculate the difference vector
            Vector3 rotationOffset = destinationPose.rotation.eulerAngles - targetHip.rotation.eulerAngles;
            Debug.Log("rotationOffset: " + rotationOffset);
            // apply difference
            zedBodyTrackingManager.rotationOffset += new Vector3(0, rotationOffset.y, 0);
            return true;
        }

        return false;
    }

    private void HandleMouseClick()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            bool hasHit = Physics.Raycast(ray, out hit);
            GameObject hitObject = hasHit ? hit.collider.gameObject : null;

            if (storedTarget != null && (hitObject == null || hitObject != storedTarget))
            {
                SetMaterialsColor(storedTarget, Color.white);
                storedTarget = null; 
            }

            if (hitObject && hitObject.GetComponentInParent<ZEDSkeletonAnimator>())
            {
                GameObject currentTarget = hitObject.GetComponentInParent<ZEDSkeletonAnimator>().gameObject;
                SetMaterialsColor(currentTarget, Color.red);
                storedTarget = currentTarget;
                StartCoroutine(CalibrateSequence());
            }
        }
    }
    
    public void SetMaterialsColor(GameObject target, Color color)
    {
        SkinnedMeshRenderer skinnedMeshRenderer = target.GetComponentInChildren<SkinnedMeshRenderer>();
        Material[] mats = skinnedMeshRenderer.materials;
        foreach (Material mat in mats)
        {
            mat.color = color;
        }
        skinnedMeshRenderer.materials = mats;
    }

    public override void Stop_Action()
    {
    }

    private ParticipantOrder _participantOrder;
    private ulong CLID_;
    public override void AssignClient(ulong _CLID_, ParticipantOrder _participantOrder_)
    {
        _participantOrder = _participantOrder_;
        CLID_= _CLID_;
    }

    
    public NetworkVariable<ulong> AnimatorObjectNOID = new NetworkVariable<ulong>();
    public Transform HeadClientRef;
    public Transform LeftClientRef;
    public Transform RightClientRef;
    public override Transform GetCameraPositionObject()
    {
        if (IsServer) {
            return CameraTrackingTransform;
        }

        if (IsClient) {
            if (NetworkManager.SpawnManager.SpawnedObjects.ContainsKey(AnimatorObjectNOID.Value)) {
                return NetworkManager.SpawnManager.SpawnedObjects[AnimatorObjectNOID.Value].transform
                    .Find("mixamorig:Hips/" +
                          "mixamorig:Spine/" +
                          "mixamorig:Spine1/" +
                          "mixamorig:Spine2/" +
                          "mixamorig:Neck/" +
                          "mixamorig:Head/" +
                          "AvatarAnchor");
                
                
                // .GetComponent<ZEDSkeletonAnimator>().animator.GetBoneTransform(HumanBodyBones.Head);
                
            }
        }

        return null;
         
     
    }

    public Transform GetLeftHandRoot() {
        if (IsClient) {
            if (NetworkManager.SpawnManager.SpawnedObjects.ContainsKey(AnimatorObjectNOID.Value)) {
                return NetworkManager.SpawnManager.SpawnedObjects[AnimatorObjectNOID.Value].transform
                    .Find("mixamorig:Hips/" +
                          "mixamorig:Spine/" +
                          "mixamorig:Spine1/" +
                          "mixamorig:Spine2/" +
                          "mixamorig:LeftShoulder/" +
                          "mixamorig:LeftArm/" +
                          "mixamorig:LeftForeArm/"+
                          "mixamorig:LeftHand");
                   
              //  "Camera Offset/Right Hand Tracking/R_Wrist/R_Palm"
                
                // .GetComponent<ZEDSkeletonAnimator>().animator.GetBoneTransform(HumanBodyBones.LeftHand);
                
            }
        }

        return null;
    }
    public Transform GetRightHandRoot() {
        if (IsClient) {
            if (NetworkManager.SpawnManager.SpawnedObjects.ContainsKey(AnimatorObjectNOID.Value)) {
                return NetworkManager.SpawnManager.SpawnedObjects[AnimatorObjectNOID.Value].transform
                    .Find("mixamorig:Hips/" +
                          "mixamorig:Spine/" +
                          "mixamorig:Spine1/" +
                          "mixamorig:Spine2/" +
                          "mixamorig:RightShoulder/" +
                          "mixamorig:RightArm/" +
                          "mixamorig:RightForeArm/"+
                          "mixamorig:RightHand");
                    
                    //.GetComponent<ZEDSkeletonAnimator>().animator.GetBoneTransform(HumanBodyBones.RightHand);
                
            }
        }

        return null;
    }
    public override void SetStartingPose(Pose _pose)
    {
       
    }

    public override bool HasActionStopped()
    {
        return true;
    }
}

#endif