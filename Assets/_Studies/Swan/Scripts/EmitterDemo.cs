using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EmitterDemo : MonoBehaviour
{
    [SerializeField]
    bool doRotation = true;

    [SerializeField]
    float minPeriod = 0.1f;

    [SerializeField]
    float maxPeriod = 1f;

    [SerializeField]
    float minDistance = 0.1f;
    float maxDistance = 10f;

    public float distance = 1f;

    [SerializeField]
    float period = 1f;

    float lastPing;

    [SerializeField]
    SWANEngine.AudioType audioType = SWANEngine.AudioType.Beacon;

    // Start is called before the first frame update
    void Start()
    {
        lastPing = Time.timeSinceLevelLoad;
    }

    // Update is called once per frame
    void Update()
    {
        if(doRotation)      
            this.transform.RotateAround(this.transform.parent.transform.position, Vector3.up, Time.deltaTime * 60f);


        period = minPeriod + (maxPeriod - minPeriod) * (distance - minDistance) / (maxDistance - minDistance);


        if(Time.timeSinceLevelLoad - lastPing >= period)
        {
            lastPing = Time.timeSinceLevelLoad;

            EventManager.TriggerEvent<SpatialAudioEvent, SWANEngine.AudioType, Vector3, float, float>(audioType, this.transform.position, 1f, 20f);
        }
    }
}
