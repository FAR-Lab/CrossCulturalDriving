using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
//using  Unity.Netcode.SceneManagement;
using UnityEngine;

public class LocalVRPlayer : MonoBehaviour {
    private NetworkManager _networkManager;

    // Start is called before the first frame update
    public bool loading = true;
    public ParticipantInputCapture PIC = null;
    public LanguageSelect lang { private set; get; }
    public ParticipantOrder MyOrder{ private set; get; }
    private bool CallBackSet = false;

    void Start() {
        DontDestroyOnLoad(gameObject);
      //  _networkManager = NetworkManager.Singleton;
       // NetworkManager.Singleton.SceneManager.OnSceneEvent += SceneLoading;
        
    }

    public void SetParticipantOrder(ParticipantOrder or) {
        MyOrder = or;
    }

    private void SceneLoading(SceneEvent sceneevent) {
        switch (sceneevent.SceneEventType) {
            case SceneEventType.Load: break;
            case SceneEventType.Unload:
                PIC = null;
                loading = true;
                break;
            case SceneEventType.Synchronize: break;
            case SceneEventType.ReSynchronize: break;
            case SceneEventType.LoadEventCompleted: break;
            case SceneEventType.UnloadEventCompleted: break;
            case SceneEventType.LoadComplete:
                loading = false;
                break;
            case SceneEventType.UnloadComplete: break;
            case SceneEventType.SynchronizeComplete: break;
            default: throw new ArgumentOutOfRangeException();
        }
    }


    private void OnDestroy() {  }

    // Update is called once per frame
    void Update() {
        if (!CallBackSet && NetworkManager.Singleton.SceneManager != null) {
            NetworkManager.Singleton.SceneManager.OnSceneEvent += SceneLoading;
            CallBackSet = true;
        }
        if (PIC == null && loading == false) {
            foreach (ParticipantInputCapture pic_ in FindObjectsOfType<ParticipantInputCapture>()) {
                if (pic_.IsLocalPlayer && pic_.ReadyForAssignment) {
                   
                    PIC = pic_;
                }
            }
        }
    }

    private void LateUpdate() {
        if (PIC != null) {
            var transform1 = transform;
            var transform2 = PIC.transform;
            transform1.position = transform2.position;
            transform1.rotation = transform2.rotation;
        }
    }

    public void Setlanguage(string lang_) { lang = lang_; }
}