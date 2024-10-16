using System;
using System.Collections;
using Unity.Mathematics;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Splines;

# if UNITY_EDITOR
using UnityEditor;
#endif

public class SC_AVStateMachine : NetworkBehaviour {
    
    private bool _ready = false;
    
    private SC_AVContext _context;
    
    public SO_FSMNodeContainer startNodeContainer;
    [SerializeField] private SO_FSMNode currentNode;

    public SplineContainer splineContainer; 
    private float _currentT = 0f;
    private int _currentSplineIndex = 0;
    private float _currentSplineLength = 0f; 
    
    public float rotationSpeed = 5f;
    public float startPointRatio = 0.5f;
    
    private void Start() {
        if (!IsServer) {
            enabled = false;
            _context.enabled = false;
            return;
        }
        
        _currentT = Mathf.Clamp01(startPointRatio); 
        _context = gameObject.GetComponent<SC_AVContext>();
        
        // calculate length such that the speed end up not affected by the distance between the points
        _currentSplineLength = splineContainer.CalculateLength(_currentSplineIndex);

        currentNode = startNodeContainer.startNode;
        currentNode.Action.OnEnter(_context);
        
        StartCoroutine(PrepToStart());
    }
    
    private IEnumerator PrepToStart() {
        yield return new WaitUntil(() => ConnectionAndSpawning.Singleton.ServerState == ActionState.DRIVE);
        _context.AssignVehicles();
        _ready = true;
    }
    
    private void Update() {
        if (!_ready) return;
        
        currentNode.Action.OnUpdate(_context);
        SO_FSMNode nextNode = currentNode.CheckTransitions(_context);
        
        if (nextNode != null) {
            currentNode.Action.OnExit(_context);
            currentNode = nextNode;
            Debug.Log("FSM: Transitioning to " + currentNode.name);
            currentNode.Action.OnEnter(_context);
        }
        
        DriveVehicle();

        // Debug.Log("Speed: " + _context.GetSpeed());
    }
    
    private void DriveVehicle() {
        if (splineContainer == null || splineContainer.Splines.Count == 0)
            return;

        float distanceToTravel = _context.GetSpeed() * Time.deltaTime;

        float distanceRatio = distanceToTravel / _currentSplineLength; 
        _currentT += distanceRatio;

        if (_currentT > 1f) {
            _currentT = 0f;
            _currentSplineIndex = (_currentSplineIndex + 1) % splineContainer.Splines.Count;
            _currentSplineLength = splineContainer.CalculateLength(_currentSplineIndex); 
        }

        float3 position = splineContainer.EvaluatePosition(_currentSplineIndex, _currentT);
    
        float3 tangent = splineContainer.EvaluateTangent(_currentSplineIndex, _currentT);
    
        transform.position = position;
    
        Quaternion targetRotation = Quaternion.LookRotation(new Vector3(tangent.x, 0, tangent.z), Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
    }
    
#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (_context == null) return;
        if (!_ready) return;

        Vector3 vehiclePosition = transform.position;
        float distanceToCenter = Vector3.Distance(_context.centerPos, vehiclePosition);
        float currentSpeed = _context.GetSpeed();

        string currentNodeName = currentNode != null ? currentNode.name : "No Current Node";
        string isFrontClear = _context.IsFrontClear() ? "Yes" : "No";

        Vector3 labelPosition = vehiclePosition + Vector3.up * 2f;

        string labelText = $"Distance to Center: {distanceToCenter:F2}\n" +
                           $"Other Distance: {_context.GetDistanceToCenter(_context.GetOtherNetworkVehicleController()):F2}\n" +
                           $"Speed: {currentSpeed:F2}\n" +
                           $"Current Node: {currentNodeName}\n" +
                            $" Is front clear: {isFrontClear}";

        GUIStyle style = new GUIStyle();
        style.fontSize = 20; 
        style.normal.textColor = Color.red; 

        Handles.Label(labelPosition, labelText, style);    
    }
#endif


}