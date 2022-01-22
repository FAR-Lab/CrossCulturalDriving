using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class tempDebug : MonoBehaviour {
    public float interval=1.0f;

    private float lastTime = 0;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (lastTime + interval < Time.time) {
            lastTime = Time.time;
            Debug.Log("My x position at the time:"+transform.position.x.ToString());
            Debug.Log("My y rotation at the time:"+transform.rotation.y.ToString());
        }
    }
}
