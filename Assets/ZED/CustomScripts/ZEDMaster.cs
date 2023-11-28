/*

#if USING_ZED

using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Serialization;
using sl;
using Unity.Netcode;
using UnityEngine.InputSystem;


public class ZEDMaster : MonoBehaviour {
    private List<ZEDSkeletonAnimator> zedAvatars;
    private int currentAvatarIndex = 0;
    
    [SerializeField] private GameObject storedTarget;
    private ZEDSkeletonAnimator targetAnimator;
    private ZEDBodyTrackingManager zedBodyTrackingManager;
 
   
    #region SingeltonManagment

    public static ZEDMaster Singleton { get; private set; }

    private void SetSingleton() {
        Singleton = this;
    }

    private void OnEnable() {
        if (Singleton != null && Singleton != this) {
            Destroy(gameObject);
            return;
        }

        SetSingleton();
        
    }

    private void OnDestroy() {
        if (Singleton != null && Singleton == this) Singleton = null;
            Debug.Log("About to destroy abunch of Skeletons");
        ZEDSkeletonAnimator[] avatars = FindObjectsOfType<ZEDSkeletonAnimator>();
        foreach (var avatar in avatars) {
            avatar.GetComponent<NetworkObject>().Despawn();
            Destroy(avatar.gameObject);
        }
        
    }

    #endregion

    private void Start() {
        zedBodyTrackingManager = GetComponent<ZEDBodyTrackingManager>();
     
       
        TryToFindGlobalReference();
    }

 
   

    public void TryToFindGlobalReference() {
        var  re= FindObjectOfType<ZedSpaceReference>();
        if (re != null) {
            //zedBodyTrackingManager.positionOffset = re.transform.position;
            //zedBodyTrackingManager.rotationOffset = re.transform.rotation.eulerAngles;
        }
    }
    public Vector3 GetHipPosition() {
      return  targetAnimator.animator.GetBoneTransform(HumanBodyBones.Hips).position;
    }
  

    private void Update() {
        //HandleMouseClick();
        CycleThroughAvatars();
        
    }


 
  bool  ReconnectAvatar=false;
    private void CycleThroughAvatars() {
        // if press right arrow, go to next avatar
        if (Keyboard.current.rightArrowKey.wasPressedThisFrame) {
            zedAvatars = new List<ZEDSkeletonAnimator>(FindObjectsOfType<ZEDSkeletonAnimator>());
            if (currentAvatarIndex >= zedAvatars.Count) return;
            storedTarget = zedAvatars[currentAvatarIndex].gameObject;
            
            foreach (ZEDSkeletonAnimator zed in zedAvatars) {
                SetMaterialsColor(zed.gameObject, Color.white);
            }

            SetMaterialsColor(storedTarget, Color.red);
            currentAvatarIndex++;
            if (currentAvatarIndex == zedAvatars.Count) {
                currentAvatarIndex = 0;
            }
        }

       

       //if (Keyboard.current.upArrowKey.wasPressedThisFrame) {
       //     StartCoroutine(InitalCalibrateSequence());
      //  }
    }

    public void e_StartCalibrationSequence(Action<int> setSkeletonID,Action<Transform> CalibrationFinished) {
        if (NetworkManager.Singleton.IsServer) {
            StartCoroutine(InitalCalibrateSequence(setSkeletonID, CalibrationFinished));
        }
    }
    public void e_ReconnectionStart(int skeletonID, Action<Transform> triggerCalibration) {
        if (NetworkManager.Singleton.IsServer) {
            
            
        }
    }
    
    private IEnumerator InitalCalibrateSequence(Action<int> setSkeletonID,Action<Transform> CalibrationFinished) {
        
            yield return new WaitUntil(() => FindDependencies());
            setSkeletonID.Invoke(targetAnimator.Skhandler.m_person_id);
            CalibrationFinished.Invoke(GetCameraPositionObject());
        
    }
    
    private IEnumerator ReconnectCalibrateSequence(int setSkeletonID,Action<Transform> CalibrationFinished) {
        if (FindDependencies(setSkeletonID)) {
            yield return new WaitForEndOfFrame();
            CalibrationFinished.Invoke(GetCameraPositionObject());
        }
    }


    private bool FindDependencies(int skeletonid =-1) {
        if (storedTarget == null) {
            ZEDSkeletonAnimator[] allAvatars = FindObjectsOfType<ZEDSkeletonAnimator>();
            if (allAvatars.Length == 1) {
                storedTarget = allAvatars[0].gameObject;
            }
            else if (skeletonid != -1) {
                storedTarget = allAvatars.Where(x => x.Skhandler.m_person_id == skeletonid).First().gameObject;
            }
            else {
                return false;
            }
           
        }
        targetAnimator = storedTarget.GetComponent<ZEDSkeletonAnimator>();
        if (targetAnimator != null) {
            return true;
        }
        return false;
    }
    public void SetMaterialsColor(GameObject target, Color color) {
        SkinnedMeshRenderer skinnedMeshRenderer = target.GetComponentInChildren<SkinnedMeshRenderer>();
        Material[] mats = skinnedMeshRenderer.materials;
        foreach (Material mat in mats) {
            mat.color = color;
        }
        skinnedMeshRenderer.materials = mats;
    }
    public Transform GetCameraPositionObject() {
        return targetAnimator.animator.GetBoneTransform(HumanBodyBones.Head);
    }

   
}

#endif

*/