#if USING_ZED

using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using sl;
using Unity.Netcode;
using UnityEngine.InputSystem;


public class ZEDMaster : MonoBehaviour {
    private List<ZEDSkeletonAnimator> zedAvatars;
    private int currentAvatarIndex = 0;


    [SerializeField] private GameObject storedTarget;
    private ZEDSkeletonAnimator targetAnimator;
    private ZEDBodyTrackingManager zedBodyTrackingManager;
    private Transform targetHip;

    private Pose destinationPose;

    private Transform CameraTrackingTransform;

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
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy() {
        if (Singleton != null && Singleton == this) Singleton = null;
    }

    #endregion

    private void Start() {
        zedBodyTrackingManager = GetComponent<ZEDBodyTrackingManager>();
        zedBodyTrackingManager.OnSkeletonChange += OnDetectionChange;
        ConnectionAndSpawning.Singleton.ServerStateChange += ServerStateChange;
    }

    private void ServerStateChange(ActionState state) {
        switch (state) {
            case ActionState.DEFAULT:
                break;
            case ActionState.WAITINGROOM:
                break;
            case ActionState.LOADINGSCENARIO:
                break;
            case ActionState.LOADINGVISUALS:
                break;
            case ActionState.READY:
                var  re= FindObjectOfType<ZedSpaceReference>();
                if (re != null) {
                    zedBodyTrackingManager.positionOffset = re.transform.position;
                    zedBodyTrackingManager.rotationOffset = re.transform.rotation.eulerAngles;
                }
                break;
            case ActionState.DRIVE:
                break;
            case ActionState.QUESTIONS:
                break;
            case ActionState.POSTQUESTIONS:
                break;
            case ActionState.RERUN:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(state), state, null);
        }
    }

    public Vector3 GetHipPosition() {
      return  targetAnimator.animator.GetBoneTransform(HumanBodyBones.Hips).position;
    }
    private void OnDetectionChange(UpdateType state, int id) {
        switch (state) {
            case UpdateType.NEWSKELETON:
              
                break;
            case UpdateType.DELETESKELETON:
                if (targetAnimator != null &&  id == targetAnimator.Skhandler.m_person_id) {
                    OnChangedTrackingReferrence.Invoke(UpdateType.DELETESKELETON);
                    targetAnimator = null;
                }
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(state), state, null);
        }
    }

    private void Update() {
        //HandleMouseClick();
        CycleThroughAvatars();
        
    }


    private void OnDisable() {
        // destroy all avatars
        ZEDSkeletonAnimator[] avatars = FindObjectsOfType<ZEDSkeletonAnimator>();
        foreach (var avatar in avatars) {
            Destroy(avatar.gameObject);
        }
    }

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

        if (Keyboard.current.upArrowKey.wasPressedThisFrame) {
            StartCoroutine(CalibrateSequence());
        }
    }

    public void e_StartCallibrationSequence() {
        StartCoroutine(CalibrateSequence());
    }

    private IEnumerator CalibrateSequence() {
        if (NetworkManager.Singleton.IsServer) {
            yield return new WaitUntil(() => FindDependencies());
            yield return new WaitUntil(() => CalibratePosition());
            yield return new WaitForEndOfFrame();
            yield return new WaitUntil(() => CalibrateRotation());
            yield return new WaitForEndOfFrame();
            yield return new WaitUntil(() => CalibratePosition());
            yield return new WaitForEndOfFrame();
            OnChangedTrackingReferrence.Invoke(UpdateType.NEWSKELETON);

            Debug.Log($"Finished Calibrating!");
        }
    }


    private bool FindDependencies() {
        // if there is only one avatar, assign it to storedTarget
        if (storedTarget == null) {
            ZEDSkeletonAnimator[] allAvatars = FindObjectsOfType<ZEDSkeletonAnimator>();
            if (allAvatars.Length == 1) {
                storedTarget = allAvatars[0].gameObject;
            }
            else {
                return false;
            }
        }

        
        // get the current hip position of target
        targetAnimator = storedTarget.GetComponent<ZEDSkeletonAnimator>();
        targetHip = targetAnimator.animator.GetBoneTransform(HumanBodyBones.Hips);
        CameraTrackingTransform = targetAnimator.animator.GetBoneTransform(HumanBodyBones.Head);

        if (targetAnimator != null && targetHip != null && CameraTrackingTransform != null) {
            return true;
        }

        return false;
    }

    private bool CalibratePosition() {
        // calculate the difference vector
        if (destinationPose != null && targetHip != null) {
            Vector3 positionOffset = destinationPose.position - targetHip.position;
            // apply difference
            zedBodyTrackingManager.positionOffset += positionOffset;
            return true;
        }

        return false;
    }

    private bool CalibrateRotation() {
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

    private void HandleMouseClick() {
        if (Input.GetMouseButtonDown(0)) {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            bool hasHit = Physics.Raycast(ray, out hit);
            GameObject hitObject = hasHit ? hit.collider.gameObject : null;

            if (storedTarget != null && (hitObject == null || hitObject != storedTarget)) {
                SetMaterialsColor(storedTarget, Color.white);
                storedTarget = null;
            }

            if (hitObject && hitObject.GetComponentInParent<ZEDSkeletonAnimator>()) {
                GameObject currentTarget = hitObject.GetComponentInParent<ZEDSkeletonAnimator>().gameObject;
                SetMaterialsColor(currentTarget, Color.red);
                storedTarget = currentTarget;
                StartCoroutine(CalibrateSequence());
            }
        }
    }

    public void SetMaterialsColor(GameObject target, Color color) {
        SkinnedMeshRenderer skinnedMeshRenderer = target.GetComponentInChildren<SkinnedMeshRenderer>();
        Material[] mats = skinnedMeshRenderer.materials;
        foreach (Material mat in mats) {
            mat.color = color;
        }

        skinnedMeshRenderer.materials = mats;
    }


    public enum UpdateType {
        NEWSKELETON,
        DELETESKELETON
    }
    public delegate void d_OnNewTrackingReferenceAcquired(UpdateType ud);


    public d_OnNewTrackingReferenceAcquired OnChangedTrackingReferrence;



    public Transform GetCameraPositionObject() {
        return CameraTrackingTransform;
    }

    public Transform GetLeftHandRoot() {
      if(targetAnimator!=null)
        return targetAnimator.animator.GetBoneTransform(HumanBodyBones.LeftHand);
      return null;
    }

    public Transform GetRightHandRoot() {
        if(targetAnimator!=null)
            return targetAnimator.animator.GetBoneTransform(HumanBodyBones.RightHand);
        return null;
        
    }
}

#endif