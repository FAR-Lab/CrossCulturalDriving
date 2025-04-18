using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DisableOnAndroid : MonoBehaviour
{
    private void Awake() {
        if (Application.platform == RuntimePlatform.Android) Destroy(gameObject);
        else{DontDestroyOnLoad(gameObject);}
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
