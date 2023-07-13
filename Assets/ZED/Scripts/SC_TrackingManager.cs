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

    //[SerializeField] private Transform origin;
    
    [SerializeField] private Transform anchor;

    private GameObject player;
    private Transform hip;
    private SC_Container container;
    private Transform lookat;
    public Vector3 positionOffset = Vector3.zero;

    void Update()
    {
        if (Input.GetKey(KeyCode.Tab))
        {
            if(Input.GetKeyDown(KeyCode.C)){
                Calibrate();}
            if(Input.GetKeyDown(KeyCode.R)){
                CalibrateRotation();
            }
        }


        // draw debug lines
        if(hip != null & lookat != null)
        {
        Debug.DrawLine (hip.position, hip.position + hip.forward, Color.green);
        Debug.DrawLine (hip.position, lookat.position, Color.red);
        }

    }

    void Calibrate(){

        FindDependencies();
        OffsetPosition();
        //container.Calibrate();
    }

    void CalibrateRotation(){
                FindDependencies();

        player.GetComponent<ZEDSkeletonAnimator>().OffsetAngle(lookat);

    }

    void OffsetPosition(){

        // offset position
        positionOffset = anchor.position - hip.position;
        zedBodyTrackingManager.manualOffset += positionOffset;
        //zedBodyTrackingManager.manualOffset.y = 0;

        // offset rotation

    }

    public void FindDependencies(){
        // player Related
        player = GameObject.FindWithTag("Avatar");
        container = player.GetComponentInChildren<SC_Container>();
        hip = player.transform.Find("mixamorig:Hips");
        zedBodyTrackingManager = transform.GetComponentInChildren<ZEDBodyTrackingManager>();
    
        // Calibration Related
        anchor = GameObject.FindWithTag("Anchor").transform;
        lookat = GameObject.FindWithTag("Lookat").transform;

    }


}
