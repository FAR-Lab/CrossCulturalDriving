using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConfigFileTester : MonoBehaviour
{
    // Start is called before the first frame update
    public Transform Parent;
    public Transform Child;

    public bool load;
    public bool store;
    [HideInInspector]
    public bool StoringComplete;

    private ConfigFileLoading conf;
    void Start()
    {
        StoringComplete = false;
        conf = GetComponent<ConfigFileLoading>();
        Child.parent = Parent;
    }

    // Update is called once per frame
    void Update()
    {
        if (load)
        {
            load = false;
           
            conf.LoadLocalOffset(out Vector3 pos,out Quaternion rot);
            Child.localPosition = pos;
            Child.localRotation = rot;
        }
        else if (store)
        {
            store = false;
           
            conf.StoreLocalOffset(Child.localPosition,Child.localRotation );
            StoringComplete = true;
        }
       
    }
}
