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
    private Transform hip;
    private SC_Container container;
    private Vector3 lookat;
    public Vector3 positionOffset = Vector3.zero;

    // set in inspector
    public ParticipantOrder participantOrder;

    private ScenarioManager scenarioManager;

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
        }


        // draw debug lines
        if (hip != null & lookat != null)
        {
            Debug.DrawLine(hip.position, hip.position + hip.forward, Color.green);
            Debug.DrawLine(hip.position, lookat, Color.red);
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
        player.GetComponent<ZEDSkeletonAnimator>().OffsetAngle(lookat);
    }

    public void FindDependencies()
    {
        // player Related
        player = GameObject.FindWithTag("Avatar");
        container = player.GetComponentInChildren<SC_Container>();
        hip = player.transform.Find("mixamorig:Hips");
        zedBodyTrackingManager = transform.GetComponentInChildren<ZEDBodyTrackingManager>();

        scenarioManager = FindObjectOfType<ScenarioManager>();
        Pose spawnPose = scenarioManager.MySpawnPositions[participantOrder];
        anchor = spawnPose.position;
        lookat = spawnPose.position + spawnPose.forward;

    }


}
