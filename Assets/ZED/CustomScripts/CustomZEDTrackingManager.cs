using System.Collections;
using UnityEngine;

public class CustomZEDTrackingManager : MonoBehaviour
{
    // storedTarget is only used for VR headset assignment
    [SerializeField] private GameObject storedTarget;
    private ZEDSkeletonAnimator targetAnimator;
    private ZEDBodyTrackingManager zedBodyTrackingManager;
    private Transform targetHip;
    
    public ParticipantOrder POToMatch;
    private Pose destinationPose;

    private ScenarioManager scenarioManager;
    
    private void Update()
    {
        HandleMouseClick();

        if (Input.GetKey(KeyCode.Tab) && Input.GetKeyDown(KeyCode.C))
        {
            FindDependencies();
            StartCoroutine(CalibrateSequence());

        }
        if (Input.GetKey(KeyCode.Tab) && Input.GetKeyDown(KeyCode.R))
        {
            FindDependencies();
            CalibrateRotation();
        }
    }

    private IEnumerator CalibrateSequence()
    {
        CalibratePosition();
        yield return new WaitForSeconds(0.05f);
        CalibrateRotation();
        yield return new WaitForSeconds(0.05f);
        CalibratePosition();
    }
    
    private void FindDependencies()
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
                Debug.LogError("There are more than one avatars in the scene. Please assign the target avatar by clicking.");
                return;
            }
        }
        
        zedBodyTrackingManager = GetComponent<ZEDBodyTrackingManager>();
        scenarioManager = FindObjectOfType<ScenarioManager>();
        
        // get the current hip position of target
        targetAnimator = storedTarget.GetComponent<ZEDSkeletonAnimator>();
        targetHip = targetAnimator.animator.GetBoneTransform(HumanBodyBones.Hips);
        
        scenarioManager.GetSpawnPose(POToMatch, out destinationPose);
    }
    private void CalibratePosition()
    {
        // calculate the difference vector
        Vector3 positionOffset = destinationPose.position - targetHip.position;
        // apply difference
        zedBodyTrackingManager.positionOffset += positionOffset;
    }

    private void CalibrateRotation()
    {
        // calculate the difference vector
        Vector3 rotationOffset = destinationPose.rotation.eulerAngles - targetHip.rotation.eulerAngles;
        Debug.Log("rotationOffset: " + rotationOffset);
        // apply difference
        zedBodyTrackingManager.rotationOffset += new Vector3(0, rotationOffset.y, 0);
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
}