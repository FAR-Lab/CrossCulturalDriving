using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// code from SWAN2: https://github.gatech.edu/IMTC/SWAN2/blob/master/Assets/Scripts/SWANDemoAgent.cs

public class BeaconRelativePositioning : MonoBehaviour
{
    // we need to define the nav mesh agent type
    UnityEngine.AI.NavMeshAgent _agent;

    // stores all the waypoints in a game object:
    // we need to create the GameObject type if it isn't already present
    // im not sure if this is a default type in unity or not
    [SerializeField]
    GameObject[] _waypoints = new GameObject[0];

    // initialize a default current waypoint
    int currWaypoint = -1;

    [SerializeField]
    Animator _anim;

    [SerializeField]
    GameObject _beacon;

    [SerializeField]
    EmitterDemo _beaconEmitter;

    float epsilon = 0.01f;

    public float dist;
    public bool hasPath;

    // do we need to define a game object type?
    // if the waypoint is in the waypoints data structure index, then return the 
    // get the current waypoint 
    GameObject CurrentWaypoint
    {
        get
        {
            if (currWaypoint >= 0 && currWaypoint < _waypoints.Length)
                return _waypoints[currWaypoint];
            else
                return null;
        }
    }

    // set the next waypoint
    void setNextWaypoint()
    {
        if (_waypoints.Length <= 0)
            return;

        if (currWaypoint < 0 || currWaypoint >= _waypoints.Length -1)
            currWaypoint = 0;
        else
        {
            ++currWaypoint;
        }

        // set the destination as the next waypoint 
        Debug.Log("Destionation set");
        _agent.SetDestination(_waypoints[currWaypoint].transform.position);
    }

    // Start is called before the first frame update
    void Awake()
    {
        // we need to make sure the game component nav mesh agent is defined 
        _agent = GetComponent<UnityEngine.AI.NavMeshAgent>();

        if (_agent == null)
            Debug.Log("No agent");

        if (_anim == null)
            Debug.Log("No animator");

        if (_beacon == null)
            Debug.Log("No beacon");

        if (_beaconEmitter == null)
            Debug.Log("No beacon emitter");

        
    }
    void Start()
    {
        // add the Animator class 
        _anim.applyRootMotion = false;

        setNextWaypoint();
        
    }

    public Vector2 DEBUGwaypointDir;


    // Update is called once per frame
    void Update()
    {
        dist = _agent.remainingDistance;
        hasPath = _agent.hasPath;

        var waypointV = (CurrentWaypoint.transform.position - transform.position);
        var waypointDir = waypointV.normalized;
        Vector2 waypointDir2d = (new Vector2(waypointDir.x, waypointDir.z)).normalized;
        Vector2 currDir2d = (new Vector2(transform.forward.x, transform.forward.z)).normalized;

        DEBUGwaypointDir = waypointDir2d;

        var angle = Vector2.SignedAngle(waypointDir2d, currDir2d);

        //_beacon.transform.rotation.SetLookRotation(new Vector3(waypointDir2d.x, 0f, waypointDir2d.y));

        //_beacon.transform.LookAt(new Vector3(waypointDir2d.x, 0f, waypointDir2d.y));
        _beacon.transform.forward = new Vector3(waypointDir2d.x, 0f, waypointDir2d.y);
        _beaconEmitter.distance = waypointV.magnitude;

        _anim.SetFloat("Speed_f", 1f);

        if(!_agent.pathPending)
        {
            if ( _agent.remainingDistance <= epsilon)
            {
                Debug.Log("New Waypoint set");
                setNextWaypoint();
                EventManager.TriggerEvent<SpatialAudioEvent, SWANEngine.AudioType, Vector3, float, float>(SWANEngine.AudioType.Success, CurrentWaypoint.transform.position, 200f, 2000f);
            }
        }
    }
}
