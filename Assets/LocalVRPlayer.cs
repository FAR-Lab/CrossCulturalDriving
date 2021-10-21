using System;
using System.Collections;
using System.Collections.Generic;
using MLAPI;
using MLAPI.SceneManagement;
using UnityEngine;

public class LocalVRPlayer : MonoBehaviour
{
    private NetworkManager _networkManager;
    // Start is called before the first frame update
    private bool loading = true;
    public ParticipantInputCapture PIC = null;
    public LanguageSelect lang { private set; get; }
    void Start()
    {
        DontDestroyOnLoad(gameObject);
        _networkManager = NetworkManager.Singleton;
        NetworkSceneManager.OnSceneSwitchStarted += SceneLoading;
        NetworkSceneManager.OnSceneSwitched += SceneLoaded;
    } 
    private void SceneLoading(AsyncOperation operation)
    {
        Debug.Log("Loading A new scene getting in to the Safe space before.");
        PIC = null;
        
        loading = true;
      
    }

   

    private void SceneLoaded()
    {
        loading = false;
      
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
                    
                    Debug.Log("Trying to get a folowe object! ");
                    PIC = pic_;
                  
                }
            }
        }
    }

    private void LateUpdate()
    {
        if (PIC != null)
        {
            var transform1 = transform;
            var transform2 = PIC.transform;
            transform1.position = transform2.position;
            transform1.rotation = transform2.rotation;
        }
       
    }

    public void Setlanguage(string lang_) { lang = lang_; }
}
