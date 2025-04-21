using System.Collections;
using System.Collections.Generic;
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
    private SplineCenterlineUtility _splineCLCreator;

    private float _steeringInput;
    private float _throttleInput;
    
    public List<SO_FSMNodeContainer> nodeContainers;
    
    [SerializeField] private SO_FSMNodeContainer startNodeContainer;
    [SerializeField] private SO_FSMNode currentNode;
    
    public SO_AVConfig config;

    private Rigidbody _rb;
    
    private PID _speedPID;
    private PID _steeringPID;
    
    
    private void Start()
    {
        if (!IsServer)
        {
            Destroy(this);
            return;
        }
        
        nodeContainers = config.nodeContainers;
        startNodeContainer = nodeContainers[0];
        currentNode = startNodeContainer.startNode;
        
        _myVehicleController = GetComponent<NetworkVehicleController>();
        _vehicleController = GetComponent<VehicleController>();
        _splineCLCreator = _vehicleController.SplineCLCreator;
        _context = GetComponent<SC_AVContext>();
        _context.YieldThreshold = config.YieldThreshold;
        _rb = GetComponent<Rigidbody>();
        
        _speedPID = new PID(config.SpeedP, config.SpeedI, config.SpeedD);
        _steeringPID = new PID(config.SteeringP, config.SteeringI, config.SteeringD);
        
        UpdateBehaviorParameter(startNodeContainer.name);
        
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

        if(_context.GetDistanceToCenter(_vehicleController) < config.DistanceToShrinkCollider){
            _context.triggerPlayerTracker.ShrinkCollider();
        }
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

    private bool IsPathRelativelyStraight(Vector3 currentPosition, Vector3 lookaheadPoint)
    {
        Vector3 closestPoint = _splineCLCreator.GetClosestPointOnSpline(currentPosition);
        Vector3 directionToLookahead = (lookaheadPoint - closestPoint).normalized;
        Vector3 forwardDirection = transform.forward;
        
        float angle = Vector3.Angle(forwardDirection, directionToLookahead);
        return angle < config.StraightPathThreshold;
    }
    
    private bool turnOffAdjustment = false;
    private float _straightLineFactor = 0.0f;
    
private void DriveVehicle()
{
    #region Throttle
    float currentSpeed = _vehicleController.CurrentSpeed;
    float desiredSpeed = _context.GetSpeed();
    float throttlePIDOutput = _speedPID.Update(desiredSpeed, currentSpeed, Time.deltaTime);
    float throttleFeedforward = desiredSpeed * config.ThrottleFeedforward;
    float throttleInput = throttlePIDOutput + throttleFeedforward;
    _throttleInput = Mathf.Clamp(throttleInput, -1f, 1f);
    _myVehicleController.ThrottleInput = _throttleInput;
    #endregion

    Vector3 closestPoint = _splineCLCreator.GetClosestPointOnSpline(transform.position);
    Vector3 lookaheadPoint = GetSmoothedLookaheadPoint(closestPoint, config.LookaheadDistance, transform.position.y);
    Vector3 desiredDirection = (lookaheadPoint - transform.position).normalized;

    float headingError = Vector3.SignedAngle(transform.forward, desiredDirection, Vector3.up);
    float currentCenterlineOffset = _splineCLCreator.GetClosestDistanceToSpline(transform.position);

    float combinedSteeringError = (config.LateralErrorWeight * currentCenterlineOffset) + 
                                 (config.HeadingErrorWeight * Mathf.Sin(headingError * Mathf.Deg2Rad));

    float rawSteeringInput = _steeringPID.Update(0f, combinedSteeringError, Time.deltaTime);

    float distanceToCenter = _context.GetDistanceToCenter(_context.MyCtrl);
    if (distanceToCenter < 4f) {
        turnOffAdjustment = true;
    }
    
    if (IsPathRelativelyStraight(transform.position, lookaheadPoint)) {
        if (!turnOffAdjustment) {
            rawSteeringInput *= config.ReducedSteeringFactor;
        }
        else {
            rawSteeringInput *= 0.5f;
        }
    }

    _steeringInput = Mathf.Clamp(rawSteeringInput, -1f, 1f);
    
    _myVehicleController.SteeringInput = _steeringInput;
    _context.triggerPlayerTracker.RotateCollider(_steeringInput);
}
    
    Vector3 GetSmoothedLookaheadPoint(Vector3 currentPosition, float lookaheadDistance, float height)
    {
        Vector3 closestPoint = _splineCLCreator.GetClosestPointOnSpline(currentPosition);
    
        Vector3 point1 = _splineCLCreator.GetPointAtDistanceAlongSpline(closestPoint, lookaheadDistance * 0.8f, height);
        Vector3 point2 = _splineCLCreator.GetPointAtDistanceAlongSpline(closestPoint, lookaheadDistance, height);
        Vector3 point3 = _splineCLCreator.GetPointAtDistanceAlongSpline(closestPoint, lookaheadDistance * 1.2f, height);
        return (point1 + point2 + point3) / 3f;
    }
    
    

    private void UpdateBehaviorParameter(string behavior) {
        NetworkQNManager networkQNManager = FindObjectOfType<NetworkQNManager>();
        networkQNManager.SetParameters(behavior: behavior);
        
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (_context == null) return;
        if (!_ready) return;
        
        Vector3 closestPoint = _splineCLCreator.GetClosestPointOnSpline(transform.position);
        Vector3 lookaheadPoint = GetSmoothedLookaheadPoint(closestPoint, config.LookaheadDistance, transform.position.y);
        bool isStraight = IsPathRelativelyStraight(transform.position, lookaheadPoint) && !turnOffAdjustment;
        
        Vector3 vehiclePosition = transform.position;
        float distanceToCenter = Vector3.Distance(_context.IntersectionCenter.position, vehiclePosition);
        
        float desiredSpeed = _context.GetSpeed();
        float currentSpeed = _rb.velocity.magnitude;
        
        string currentNodeName = currentNode != null ? currentNode.name : "No Current Node";
        string isFrontClear = _context.IsFrontClear() ? "Yes" : "No";
        
        Vector3 labelPosition = vehiclePosition + Vector3.up * 2f;
        
        float relativeRotation = _context.MyCtrl.transform.rotation.eulerAngles.y - _context.OtherCtrl.transform.rotation.eulerAngles.y;
        
        if (relativeRotation > 180) {
            relativeRotation -= 360;
        }
        
        if (relativeRotation < -180) {
            relativeRotation += 360;
        }
        
        string labelText = $"Distance to Center: {distanceToCenter:F2}\n" +
                           $"Other Distance: {_context.GetDistanceToCenter(_context.OtherCtrl):F2}\n" +
                           $"Other Speed: {_context.OtherCtrl.CurrentSpeed:F2}\n" +
                           $"Desired speed: {desiredSpeed:F2}\n" +
                           $"Current speed: {currentSpeed:F2}\n" +
                           $"Current Node: {currentNodeName}\n" +
                           $"Yield possibility: {_context.YieldPossibility:F2}\n" +
                           $"FYield possibility: {_context._filteredYieldPossibility:F2}\n" +
                           $"Steering input: {_steeringInput:F2}\n" +
                           $"Throttle input: {_throttleInput:F2}\n" +
                           $"S: {_context.ShouldYield()}\n" +
                           $"Is front clear: {isFrontClear}\n" +
                           $"Is straight: {isStraight}\n" +
                           $"Relative Rotation: {relativeRotation:F2}\n"; 
        
                            
        
        GUIStyle style = new GUIStyle();
        style.fontSize = 16;
        style.normal.textColor = Color.red;
        
        // Handles.Label(labelPosition, labelText, style);
        
        Vector3 closestPointOnSpline = _splineCLCreator.GetClosestPointOnSpline(transform.position);
        Gizmos.DrawSphere(closestPointOnSpline, 1f);
        
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(lookaheadPoint, 1f);
        Gizmos.DrawLine(transform.position, lookaheadPoint);
        
    }
    
    private void OnGUI()
    {
        if (nodeContainers == null || nodeContainers.Count == 0) return;
        
        GUIStyle labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 28,
            normal = { textColor = Color.white }
        };
        
        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 24
        };
        GUILayout.BeginArea(new Rect(30, 100, 300, 250), GUI.skin.box); 
        GUILayout.Label("Current:", labelStyle);
        string currentNodeContainerName = startNodeContainer != null ? startNodeContainer.name : "None";
        GUILayout.Label(currentNodeContainerName, labelStyle);

        GUILayout.Space(10); 

        foreach (var container in nodeContainers)
        {
            if (GUILayout.Button(container.name, buttonStyle))
            {
                startNodeContainer = container;
                currentNode = startNodeContainer.startNode;
                currentNode.Action.OnEnter(_context);
                UpdateBehaviorParameter(container.name);
                Debug.Log("Node container switched to: " + container.name);
            }
        }
        GUILayout.EndArea();
    }

#endif
}