using UnityEngine;
using System.Collections;

public class streetManagerConnector : MonoBehaviour {

	// Use this for initialization
	void Start () {
		
		waypointStreetManagerV2 previous = transform.GetComponentInChildren<waypointStreetManagerV2> ();

		foreach(waypointStreetManagerV2 wp in transform.GetComponentsInChildren<waypointStreetManagerV2>()){
			if (wp == previous) {
				wp.NextDirectionB = null;
			} else {
				previous.NextDirectionA = wp;
				wp.NextDirectionB = previous;
				previous = wp;
			}

		}
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
