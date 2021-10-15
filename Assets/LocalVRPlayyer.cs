using System;
using System.Collections;
using System.Collections.Generic;
using MLAPI;
using MLAPI.SceneManagement;
using UnityEngine;

public class LocalVRPlayyer : MonoBehaviour
{
    private NetworkManager _networkManager;
    // Start is called before the first frame update
    private bool loading = true;
    private ParticipantInputCapture PIC = null;
    void Start()
    {
        _networkManager = NetworkManager.Singleton;
        NetworkSceneManager.OnSceneSwitchStarted += SceneLoading;
        NetworkSceneManager.OnSceneSwitched += SceneLoaded;
    } 
    private void SceneLoading(AsyncOperation operation)
    {
        Debug.Log("Loading A new scene getting in to the safespace befor.");
        PIC = null;
        transform.parent = null;
        DontDestroyOnLoad(gameObject);
        loading = true;
    }


    private void SceneLoaded()
    {
        loading = false;
        Debug.Log("Scene loaded !");
    }

    private void OnDestroy()
    {
        Debug.Log("I am getting destroyed this should not happen!");
    }

    // Update is called once per frame
    void Update()
    {
        if (PIC == null && loading == false)
        {
            foreach (ParticipantInputCapture pic_ in FindObjectsOfType<ParticipantInputCapture>())
            {
                if (pic_.IsLocalPlayer && pic_.ReadyForAssignment)
                {
                    
                    Debug.Log("Trying to get a new parent!");
                    PIC = pic_;
                    transform.parent = PIC.transform;
                    transform.localPosition = Vector3.zero;
                    transform.localRotation=Quaternion.identity;
                }
            }
        }
    }
}
