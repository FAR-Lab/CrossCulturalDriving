using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Timeline;
using Random = UnityEngine.Random;

public class DummyTrafficLight : MonoBehaviour
{
    // Start is called before the first frame update
    private Transform _transform;
    public GameObject signalPrefab;
    private GameObject _trafficSignal;
    public bool isRed = true;
    public float interval = 6f;
    
    public bool wasOff = false;
    public bool turnOn = true;

    private void Awake() {
        _transform = gameObject.GetComponent<Transform>();
        if (!_transform) {
            Debug.Log("Cannot create dummy traffic signal.");
        }
        //instantiate a ball a little bit above the crosswalk
        Vector3 position = new Vector3(_transform.position.x, _transform.position.y + 3.5f, _transform.position.z);
        _trafficSignal = Instantiate(signalPrefab,position,Quaternion.identity);
        _trafficSignal.GetComponent<Renderer>().material.color = Color.red;
        interval += Random.Range(1, 5);
    }

    void Start() {
        StartCoroutine(SwitchTrafficLight());
    }

    // Update is called once per frame
    void Update()
    {
        if (wasOff && turnOn) {
            StartCoroutine(SwitchTrafficLight());
            wasOff = false;
        }else if (!turnOn) {
            StopCoroutine(SwitchTrafficLight());
            wasOff = true;
        }
    }

    IEnumerator SwitchTrafficLight() {
        while (true) {
            yield return new WaitForSeconds(interval);
            isRed = !isRed;
            if (isRed) {
                _trafficSignal.GetComponent<Renderer>().material.color = Color.red;
            }
            else {
                _trafficSignal.GetComponent<Renderer>().material.color = Color.green;
            }
        }
    }
}
