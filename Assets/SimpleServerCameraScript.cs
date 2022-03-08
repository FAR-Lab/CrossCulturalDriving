using System.Collections;
using System.Collections.Generic;
using Oculus.Platform;
using Unity.Netcode;
using UnityEngine;
using Application = UnityEngine.Application;

public class SimpleServerCameraScript : MonoBehaviour {
    // Start is called before the first frame update
    void Awake() {
       DontDestroyOnLoad(gameObject);
      
    }

    void Start()
    {
        NetworkManager.Singleton.OnClientConnectedCallback += DisableMe; 
      
        
    }


    private void DisableMe(ulong obj) {
        if (NetworkManager.Singleton.IsClient) { Destroy(gameObject); }
    }


// Update is called once per frame
    void Update() { }
}

