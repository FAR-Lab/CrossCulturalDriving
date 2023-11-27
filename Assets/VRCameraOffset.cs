using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VRCameraOffset : MonoBehaviour {
    public Transform AvatarHead;
    public Transform VRCamera;
    
    void Start()
    {
        
    }

    void Update()
    {
        if (AvatarHead == null) {
            var animator = FindObjectOfType<ZEDSkeletonAnimator>();
            if (animator != null && animator.animator!=null) {
                AvatarHead = animator.animator.GetBoneTransform(HumanBodyBones.Head);
            }
        }
        else {
            if (Input.GetMouseButtonDown(0)) {
                AlignTransforms(VRCamera, AvatarHead);
            }
            
        }
    }

    void AlignTransforms(Transform from, Transform to) {
        Vector3 posDiff = to.position - from.position;
        transform.position += posDiff;


        float rotDiffY = to.eulerAngles.y - from.eulerAngles.y;

        Vector3 currentRotation = transform.eulerAngles;
        currentRotation.y += rotDiffY;
        transform.eulerAngles = currentRotation;

    }
}
