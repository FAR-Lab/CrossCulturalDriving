using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using RVP;


public class waypointMovementManagerV2 : MonoBehaviour
{
	public waypoint startWaypoint;
	waypoint previousWaypoint;
	public waypoint nextWaypoint;
	public bool fullStop=false;
	void Start ()
	{
		if (startWaypoint != null) {
			nextWaypoint = startWaypoint;
		} else {
			//initialize();
		}


	}
	public void stop(){
		fullStop = true;
		transform.GetComponent<AIInput>().stopTheCar ();
	}

	public void initialize ()
	{
       
//		Debug.Log ("uninitialized street waypoint target. Automatic init attempted");
		RaycastHit hit;

		//Debug.DrawRay (transform.position , -transform.up);
		if (Physics.Raycast (transform.position, -transform.up, out hit, 50)) {
			//	Debug.DrawLine (transform.position, hit.point);

			if (hit.transform.GetComponent<waypointStreetManagerV2> () != null) {
				//Debug.Break ();
				Vector3 forward = new Vector3 (transform.forward.x, 0, transform.forward.z).normalized;
				//lane lane=hit.transform.GetComponent<waypointStreetManagerV2> ().getForwardLane(forward);
				nextWaypoint = hit.transform.GetComponent<waypointStreetManagerV2> ().getClosestWaypoint (transform.position);


				moveToNextWaypoint ();// we need a direction and the clossesd point will not help us with that.
				if (!fullStop) {
					transform.GetComponent<AIInput> ().startTheCar ();// tell the car controller to start again
				}
				transform.GetComponent<AIInput> ().Move (nextWaypoint.position); // and then to move
			} else 				{
				Debug.Log ("I am not standing on a waypoint Street ManagerV2. Automatic Init Failed. Where should I move?"+transform.name);
			}

		} else {
			transform.GetComponent<AIInput> ().stopTheCar ();

			Debug.Log ("I am not standing on anything. Automatic Init Failed. Where should I move?");
			//Debug.Log ("What we hit was called:" +hit.transform.name);
		}
		/// This function needs to A-calculate direction vectors for each lane(by looking at the first and last waypoint object in the list(maybe this should run in the waypoint class))
		/// B-compare them to the vehicles forward direction or velocity
		/// C- Based on that pick a road side
		/// and D- retrieve the neareast way point
		/// E- update so that not th nearest way point is the next but the nexzt one in that direction



	}
	public void changeLaneTo(int lanNum){
		waypoint nextCandidate = nextWaypoint.changeLaneTo(lanNum);

		float previousDistance	= (transform.position - previousWaypoint.position).magnitude;
		float nextDistance = (transform.position - nextCandidate.position).magnitude;
		OverrideNextWaypoint (nextCandidate);
		//Debug.Log (previousDistance + "and" + nextDistance);
		if (Vector3.Angle (transform.GetComponent<VehicleParent> ().localVelocity, nextCandidate.position-transform.position ) > 20) {
			Debug.Log("angle:"+Vector3.Angle (transform.GetComponent<VehicleParent>().localVelocity, transform.position - nextCandidate.position));
			OverrideNextWaypoint (nextCandidate.getNextWaypoint());
		}

		if(previousDistance+nextDistance< (previousWaypoint.position-nextCandidate.position).magnitude){// here we make sure that we are acually not too far ahead... 
			if (nextDistance <= previousDistance * 3f) {//if we moved 75% towards the next waypoint we jump to the next one
				Debug.Log ("we are jumping ahead");
				if (moveToNextWaypoint ()) {
					transform.GetComponent<AIInput> ().Move (nextWaypoint.position);
				}
			}
		}


	}



	public void GetSpeed ()
	{//This function returns the speed stored in each waypoint //this is not implemented yet//should be easy though


	}

	// Update is called once per frame
	void Update ()
	{



		if (previousWaypoint != null && nextWaypoint != null) {
			//we Can set here a host parameter to tell the car that its going in the wrong direction
			Debug.DrawLine (transform.position, previousWaypoint.position, Color.red);
			Debug.DrawLine (transform.position, nextWaypoint.position, Color.blue);

		}
		if ((previousWaypoint == null && nextWaypoint == null) || (previousWaypoint != null && nextWaypoint == null)) {
			// here we need to call reinitialize

			initialize ();

		} else if (previousWaypoint == null && nextWaypoint != null) {

			if ((nextWaypoint.position - transform.position).magnitude < 3) {
				moveToNextWaypoint ();

			}
			if (nextWaypoint != null) {
				transform.GetComponent<AIInput> ().Move (nextWaypoint.position);
			}
		} else if (previousWaypoint != null && nextWaypoint != null) {
			float previousDistance	= (transform.position - previousWaypoint.position).magnitude;
			float nextDistance = (transform.position - nextWaypoint.position).magnitude;

			if (1f * nextDistance < previousDistance) {//if we moved 75% towards the next waypoint we jump to the next one
				if (moveToNextWaypoint ()) {
					transform.GetComponent<AIInput> ().Move (nextWaypoint.position);
				} else {
					transform.GetComponent<AIInput> ().stopTheCar ();

				}
			}
		}
	}

	public void OverrideNextWaypoint (waypoint input)
	{
		nextWaypoint = input;
		if (nextWaypoint != null) {
			transform.GetComponent<AIInput> ().Move (nextWaypoint.position);

		}
	}

	bool moveToNextWaypoint (){
		previousWaypoint = nextWaypoint;
		nextWaypoint = previousWaypoint.getNextWaypoint ();
		if (nextWaypoint == null) {
           

            return false;
		} else {
        //    Debug.Log("Getting new speed info");
            transform.GetComponent<AIInput>().desiredSpeed = nextWaypoint.getWaypointSpeed();
         //   Debug.Log("Getting new speed info" + nextWaypoint.getWaypointSpeed());
            return true;
		}
	}


}
