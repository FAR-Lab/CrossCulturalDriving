using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReportToSceneManger : MonoBehaviour {
    //public ActionState StateToReportOnLoad;
	// Use this for initialization
	void Start () {
        SceneStateManager.Instance.SetWaiting();
        Time.timeScale = 1.0f;
        FindObjectOfType<Leap.Unity.NetworkHandModelManager>().init();

    }
	
	// Update is called once per frame
	void Update () {
		
	}
}
