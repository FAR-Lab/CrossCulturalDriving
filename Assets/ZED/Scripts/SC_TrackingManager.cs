using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SC_TrackingManager : MonoBehaviour
{
    public ZEDBodyTrackingManager zedBodyTrackingManager;

    //[SerializeField] private Transform origin;
    
    [SerializeField] private Transform anchor;

    private GameObject player;
    private Transform hip;
    private SC_Container container;

    private Button calibrateButton;



    void Start()
    {
        calibrateButton = GameObject.Find("Calibrate").GetComponent<Button>();
        calibrateButton.onClick.AddListener(Calibrate);
    }

    void Update()
    {

    }

    void Calibrate(){
        player = GameObject.FindWithTag("Avatar");
        container = player.GetComponentInChildren<SC_Container>();
        hip = player.transform.Find("mixamorig:Hips");
        FindZedManager();
        OffsetPosition();
        container.Calibrate();
    }

    void OffsetPosition(){
        player.GetComponent<ZEDSkeletonAnimator>().OffsetAngle();


        Vector3 difference = anchor.position - hip.position;

        zedBodyTrackingManager.manualOffset = difference;

        zedBodyTrackingManager.manualOffset.y = 0;

    }

    public void FindZedManager(){
        // find component in scene
        zedBodyTrackingManager = transform.parent.GetComponentInChildren<ZEDBodyTrackingManager>();
    }


}
