using System.Collections;
using Unity.Netcode;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class SC_AVStateMachine : NetworkBehaviour
{
    private bool _ready = false;

    private SC_AVContext _context;

    private NetworkVehicleController _myVehicleController;
    private VehicleController _vehicleController;
    public SplineCenterlineUtility _splineCLCreator;

    private float _steeringInput;
    private float _throttleInput;
    
    public SO_FSMNodeContainer startNodeContainer;
    [SerializeField] private SO_FSMNode currentNode;

    // PID Controllers for speed and steering
    [SerializeField] private float _speedP = 0.2f, _speedI = 0.005f, _speedD = 0.05f;
    [SerializeField] private float _steeringP = 0.2f, _steeringI = 0.005f, _steeringD = 0.05f;
    [SerializeField] private float _throttleFeedforward = 0.1f;
    [SerializeField] private float _lookaheadDistance = 5f;
    
    [SerializeField] private float _lateralErrorWeight = 1.0f; 
    [SerializeField] private float _headingErrorWeight = 0.5f;

    private Rigidbody _rb;
    private PID _speedPID;
    private PID _steeringPID;
    
    private void Start()
    {
        if (!IsServer)
        {
            enabled = false;
            return;
        }

        _myVehicleController = GetComponent<NetworkVehicleController>();
        _vehicleController = GetComponent<VehicleController>();
        _splineCLCreator = GameObject.Find("AV").GetComponent<SplineCenterlineUtility>();
        _context = GetComponent<SC_AVContext>();
        _rb = GetComponent<Rigidbody>();
        
        _speedPID = new PID(_speedP, _speedI, _speedD);
        _steeringPID = new PID(_steeringP, _steeringI, _steeringD);

        currentNode = startNodeContainer.startNode;
        currentNode.Action.OnEnter(_context);

        StartCoroutine(PrepToStart());
    }

    private IEnumerator PrepToStart()
    {
        yield return new WaitUntil(() => ConnectionAndSpawning.Singleton.ServerState == ActionState.DRIVE);
        _context.Initialize();
        _ready = true;
    }

    private void Update()
    {
        if (!_ready) return;

        currentNode.Action.OnUpdate(_context);
        SO_FSMNode nextNode = currentNode.CheckTransitions(_context);

        if (nextNode != null)
        {
            currentNode.Action.OnExit(_context);
            currentNode = nextNode;
            Debug.Log("FSM: Transitioning to " + currentNode.name);
            currentNode.Action.OnEnter(_context);
        }

        DriveVehicle();
    }

  private void DriveVehicle()
{
    # region Throttle
    float currentSpeed = _rb.velocity.magnitude;

    float desiredSpeed = _context.GetSpeed();

    float throttlePIDOutput = _speedPID.Update(desiredSpeed, currentSpeed, Time.deltaTime);

    float throttleFeedforward = desiredSpeed * _throttleFeedforward;

    float throttleInput = throttlePIDOutput + throttleFeedforward;

    _throttleInput = Mathf.Clamp(throttleInput, -1f, 1f);
    
    _myVehicleController.ThrottleInput = _throttleInput;
    #endregion


    Vector3 closestPoint = _splineCLCreator.GetClosestPointOnSpline(transform.position);
    
    Vector3 lookaheadPoint = _splineCLCreator.GetPointAtDistanceAlongSpline(closestPoint, _lookaheadDistance);

    Vector3 desiredDirection = (lookaheadPoint - transform.position).normalized;

    float headingError = Vector3.SignedAngle(transform.forward, desiredDirection, Vector3.up);

    float currentCenterlineOffset = _splineCLCreator.GetClosestDistanceToSpline(transform.position);

    float combinedSteeringError = (_lateralErrorWeight * currentCenterlineOffset) + (_headingErrorWeight * Mathf.Sin(headingError * Mathf.Deg2Rad));

    float steeringInput = _steeringPID.Update(0f, combinedSteeringError, Time.deltaTime);

    _steeringInput = Mathf.Clamp(steeringInput, -1f, 1f);
    
    _myVehicleController.SteeringInput = _steeringInput;
}

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (_context == null) return;
        if (!_ready) return;

        Vector3 vehiclePosition = transform.position;
        float distanceToCenter = Vector3.Distance(_context.IntersectionCenter.position, vehiclePosition);
        
        float desiredSpeed = _context.GetSpeed();
        float currentSpeed = _rb.velocity.magnitude;

        string currentNodeName = currentNode != null ? currentNode.name : "No Current Node";
        string isFrontClear = _context.IsFrontClear() ? "Yes" : "No";

        Vector3 labelPosition = vehiclePosition + Vector3.up * 2f;

        string labelText = $"Distance to Center: {distanceToCenter:F2}\n" +
                           $"Other Distance: {_context.GetDistanceToCenter(_context.OtherCtrl):F2}\n" +
                           $"Desired speed: {desiredSpeed:F2}\n" +
                           $"Current speed: {currentSpeed:F2}\n" +
                           $"Current Node: {currentNodeName}\n" +
                           $"Yield possibility: {_context.YieldPossibility:F2}\n" +
                           $"Steering input: {_steeringInput:F2}\n" +
                            $"Throttle input: {_throttleInput:F2}\n" +
                           $"Is front clear: {isFrontClear}";

        GUIStyle style = new GUIStyle();
        style.fontSize = 20;
        style.normal.textColor = Color.red;

        Handles.Label(labelPosition, labelText, style);
    }
#endif
}
