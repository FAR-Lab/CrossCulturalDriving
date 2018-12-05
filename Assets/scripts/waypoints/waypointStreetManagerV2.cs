using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class waypointStreetManagerV2 : MonoBehaviour
{
	
	public List<lane> lanes = new List<lane>();
	public waypointStreetManagerV2 NextDirectionA;
	public waypointStreetManagerV2 NextDirectionB;

	// Use this for initialization
	void Start(){
		lanes = new List<lane>(transform.GetComponentsInChildren<lane>());

	}

	public lane getForwardLane(Vector3 forward){
		float[] angles = new float[lanes.Count];
		int i = 0;
		foreach (lane ln in lanes) {
			Vector3 temp = ln.getDirectionVector();		
			angles[i]=(Vector3.Angle (forward, new Vector3 (temp.x, 0, temp.z).normalized));// This only works in the X-Z plane; computing the angle between the lane and the 
			i++;
		}
		return lanes [FAI(angles)];
	}
	public waypoint getclosestPointOnLane( int laneNum,int laneDirection,Vector3 currentWaypoint){
		waypoint returnWaypoint=null;
		bool success=false;
		foreach(lane ln in lanes){
			if(ln.laneNumber==laneNum && ln.myDirection==laneDirection){
				returnWaypoint = ln.getClosestWaypoint (currentWaypoint);
				success=true;
				break;
			}
		}

		return returnWaypoint;
	}

	public waypoint getClosestWaypoint(Vector3 lastWaypoint){
		//forward
		float[] distance = new float[lanes.Count];
		int i = 0;
		foreach (lane ln in lanes) {
			Vector3 temp = ln.getClosestWaypoint (lastWaypoint).position;		
			distance[i]=((temp - lastWaypoint).magnitude);//forward movment
			i++;
		}
		return lanes [FAI (distance)].getClosestWaypoint (lastWaypoint);
	}

	public waypoint getNextLane(waypoint currentWaypoint)
	{
		if (NextDirectionA != null && NextDirectionB != null) {
			waypoint returnCandidateA = NextDirectionA.getClosestWaypoint(currentWaypoint.position);
			waypoint returnCandidateB = NextDirectionB.getClosestWaypoint(currentWaypoint.position);
			float optionA = (returnCandidateA.position - currentWaypoint.position).magnitude;
			float optionB = (returnCandidateB.position - currentWaypoint.position).magnitude;

			if (optionA < optionB) {
				return returnCandidateA;
			} else if (optionA > optionB) {
				return returnCandidateB;
			} else {
				Debug.Log("Again we have an equal distance between two candidate next points. This should not be happening. /n Please check your road network and the waypoints");
				return null;
			}
		} else if (NextDirectionA != null && NextDirectionB == null) {
			waypoint returnCandidate = NextDirectionA.getClosestWaypoint(currentWaypoint.position);
			if ((returnCandidate.position - currentWaypoint.position).magnitude > 20f) {// If the distance is bigger then 20 meters we assume that there is a mistake
				Debug.Log("The closest option for a way point was more then 20 Meters away. We return NULL to stop the car!");
				returnCandidate = null;
			}
			return returnCandidate;

		} else if (NextDirectionA == null && NextDirectionB != null) {
			waypoint returnCandidate = NextDirectionB.getClosestWaypoint(currentWaypoint.position);
			if ((returnCandidate.position - currentWaypoint.position).magnitude > 20f) {
				Debug.Log("The closest option for a way point was more then 20 Meters away. We return NULL to stop the car!");
				returnCandidate = null;
			}
			return returnCandidate;
		} else {
			return null;
		}
	}

	// Update is called once per frame
	void Update()
	{
	
	}
	public int FAI(float[] input){
		if (input.Length > 2) {
			float t = input [0];
			int pointer = 0;
			int i = 0;
			foreach (float f in input) {
				
				if (t > f) {
					t = f;
					pointer = i;
				}
				i++;
			}
			return pointer;
		} else {
			return 0;
		}




	}
}







