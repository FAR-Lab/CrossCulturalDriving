using System.Collections;
using System.Collections.Generic;
using Oculus.Platform;
using Unity.Netcode;
using UnityEngine;

public class SimpleServerCameraScript : MonoBehaviour {
   
    // Start is called before the first frame update
    void Awake()
    {
        
        DontDestroyOnLoad(gameObject);
        
    }
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if ( NetworkManager.Singleton != null) {
            if (NetworkManager.Singleton.IsClient) {
                Destroy(gameObject);
            }
            else {
                
            }
        }
    }
}
