using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SC_TrackingManager : MonoBehaviour
{
    # region Singleton
    public static SC_TrackingManager Singleton { get; private set; }

    private void SetSingleton()
    {
        Singleton = this;
    }

    private void OnEnable()
    {
        if (Singleton != null && Singleton != this)
        {
            Destroy(gameObject);
            return;
        }
        SetSingleton();
    }

    private void OnDestroy()
    {
        if (Singleton != null && Singleton == this)
        {
            Singleton = null;
        }
    }
    #endregion

    public ZEDBodyTrackingManager zedBodyTrackingManager;
    [SerializeField] private Vector3 anchor;
    private GameObject player;
    [SerializeField]
    private Transform hip;
    [SerializeField]
    private Transform head;
    private SC_Container container;
    private Vector3 lookat;
    public Vector3 positionOffset = Vector3.zero;
    public bool useHead = true;

    // TODO: which avatar to calibrate - for multi participant applications
    // Foreseeable issue: ZED's code acutlly doesn't support moving avatars individually, major changes to ZED's code might be needed
    public ParticipantOrder participantToCalibrateFor;

    // where to calibrate the avatar to
    public ParticipantOrder spawnPositionToCalibrateTo;

    private ScenarioManager scenarioManager;

    private Transform VRHead;

    void start()
    {

    }

    void Update()
    {
        if (Input.GetKey(KeyCode.Tab))
        {
            if (Input.GetKeyDown(KeyCode.C))
            {
                CalibratePosition();
            }
            if (Input.GetKeyDown(KeyCode.R))
            {
                CalibrateRotation();
            }
            if (Input.GetKeyDown(KeyCode.H))
            {
                CalibrateHead();
            }
        }



    }

    private void CalibrateHead(){
        VRHead = ConnectionAndSpawing.Singleton.GetClientHead(participantToCalibrateFor);
        SC_Container container = FindObjectOfType<SC_Container>();
        container.VRcam = VRHead.gameObject;
        container.Calibrate();

    }

    void OnDrawGizmos()
    {
        if (hip != null & lookat != null & head != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(hip.position, hip.position + hip.forward * 1000);
            Gizmos.color = Color.red;
            Gizmos.DrawLine(hip.position, lookat);
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(head.position, head.position + head.forward * 1000);
        }

    }


    void CalibratePosition()
    {
        FindDependencies();
        // find the difference vector
        positionOffset = anchor - hip.position;
        // apply difference
        zedBodyTrackingManager.manualOffset += positionOffset;
    }

    void CalibrateRotation()
    {
        FindDependencies();
        player.GetComponent<ZEDSkeletonAnimator>().OffsetAngle(lookat, useHead);
    }

    public void FindDependencies()
    {
        // player Related
        player = GameObject.FindWithTag("Avatar");
        container = player.GetComponentInChildren<SC_Container>();
        hip = player.GetComponentInChildren<ZEDSkeletonAnimator>().animator.GetBoneTransform(HumanBodyBones.Hips);
        head = player.GetComponentInChildren<ZEDSkeletonAnimator>().animator.GetBoneTransform(HumanBodyBones.Head);

        zedBodyTrackingManager = transform.GetComponentInChildren<ZEDBodyTrackingManager>();

        scenarioManager = FindObjectOfType<ScenarioManager>();
        Pose spawnPose = scenarioManager.MySpawnPositions[spawnPositionToCalibrateTo];
        anchor = spawnPose.position;
        lookat = spawnPose.position + spawnPose.forward;

    }


}
