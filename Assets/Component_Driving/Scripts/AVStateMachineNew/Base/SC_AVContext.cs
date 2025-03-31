 using System;
using System.Collections;
using UnityEngine;

public class SC_AVContext : MonoBehaviour {
        
    [SerializeField]private VehicleController _otherCtrl;
    public VehicleController OtherCtrl => _otherCtrl;
    
    [SerializeField]private VehicleController _myCtrl;
    public VehicleController MyCtrl => _myCtrl;
    [SerializeField] private NetworkVehicleController _myNetCtrl;
    public NetworkVehicleController MyNetCtrl => _myNetCtrl;
    
    private Rigidbody _myRb => _myCtrl.GetComponent<Rigidbody>();
    public Rigidbody MyRb => _myRb;
    private Rigidbody _otherRb => _otherCtrl.GetComponent<Rigidbody>();
    public Rigidbody OtherRb => _otherRb;
    
    private Transform _intersectionCenter => FindObjectOfType<IntersectionCenter>().transform;
    public Transform IntersectionCenter => _intersectionCenter;
    
    private float _speed;
    public TriggerPlayerTracker triggerPlayerTracker;
    private UdpSocket _udpSocket;
    
    [SerializeField] private float yieldThreshold = 0.5f;
    public float YieldThreshold
    {
        get => yieldThreshold;
        set => yieldThreshold = value;
    }
    
    bool recordYieldPossibility = false;
    float yieldPossibilitySum = 0;
    int recordCount = 0;
    float averageYieldPossibility = 0;

    private void Update() {
        // if (Input.GetKeyDown(KeyCode.Y)) {
        //     recordYieldPossibility = !recordYieldPossibility;
        // }
        
        // if (recordYieldPossibility) {
        //     yieldPossibilitySum += _yieldPossibility;
        //     recordCount++;
        //     
        //     averageYieldPossibility = yieldPossibilitySum / recordCount;
        //     Debug.Log($"Average Yield Possibility: {averageYieldPossibility}");
        // }
        
    }


    private float _yieldPossibility;
    public float YieldPossibility => _yieldPossibility;
    
    public float _filteredYieldPossibility = 0f;
    [SerializeField]
    private float alpha = 0.5f;
    
    public void Initialize() {
        _myCtrl = GetComponent<VehicleController>();
        _myNetCtrl = GetComponent<NetworkVehicleController>();
        
        Interactable_Object obj = ConnectionAndSpawning.Singleton.GetInteractableObject_For_Participant(ParticipantOrder.A);

        if (obj != null) {
            _otherCtrl = obj.GetComponent<VehicleController>();
        }
        
        _udpSocket = gameObject.AddComponent<UdpSocket>();
        _udpSocket.GotNewAiData += HandleReceivedData;
        
        StartCoroutine(SendArtificialData(_otherCtrl));
    }


    private void OnDestroy() {
        if (_udpSocket != null){
            _udpSocket.GotNewAiData -= HandleReceivedData;
        }
    }
    
    // yield & accel
    private void HandleReceivedData(float[] data) {
        // float newGoPossibility = data[0];
        // Debug.Log($"Go: {newGoPossibility}");

        _yieldPossibility = data[0];
        var data1 = data[1];
        Debug.Log($"Yield: {_yieldPossibility}, accel: {data1}");

        // float newYieldPossibility = data[1];
        _filteredYieldPossibility = alpha * _yieldPossibility + (1 - alpha) * _filteredYieldPossibility;
        // Debug.Log($"Go: {newGoPossibility} Original: {newYieldPossibility} Filtered: {_filteredYieldPossibility} Sum: {newGoPossibility+newYieldPossibility}");
    }

    public bool ShouldYield() {
        if (Input.GetKey(KeyCode.Y)) {
            return true;
        }
        
        if (Input.GetKey(KeyCode.N)) {
            return false;
        }
        
        return _filteredYieldPossibility > yieldThreshold;
    }

    
    private IEnumerator SendArtificialData(VehicleController otherCar) {
        Vector3 distance, relVelocity;
        float dot, rel_pos_magnitude, approachRate;
        
        yield return new WaitForSeconds(0.1f);
        float[] outdata = new float[6];

        while (true) {
            distance = _myRb.position - _otherRb.position;
            // Debug.Log("Distance: " + distance);
            relVelocity = _myRb.velocity - _otherRb.velocity;
            // Debug.Log("RelVelocity: " + relVelocity);
            dot = Vector3.Dot(distance, relVelocity);
            // Debug.Log("Dot: " + dot);
            rel_pos_magnitude = distance.magnitude; 
            
            // 0 : "ApproachRateOther" 
            approachRate = dot / rel_pos_magnitude;
            outdata[0] = - approachRate;
            // Debug.Log("ApproachRate: " + approachRate);
            // 1 : "Rel_Pos_Magnitude"
            outdata[1] = rel_pos_magnitude;
            // Debug.Log("Rel_Pos_Magnitude: " + rel_pos_magnitude);
            // "1_Head_Center_Distance", 
            outdata[2] = (_myRb.position-IntersectionCenter.position).magnitude; 
            // Debug.Log("1_Head_Center_Distance: " + outdata[2]);
            // "2_Head_Center_Distance", 
            outdata[3] = (_otherRb.position-IntersectionCenter.position).magnitude;
            // Debug.Log("2_Head_Center_Distance: " + outdata[3]);
            // "Filtered_2_Head_Velocity_Total"
            outdata[4] = _otherRb.velocity.magnitude;
            Debug.Log("Filtered_2_Head_Velocity_Total: " + outdata[4]);
            
            // debug log all the data in one line
            string debugString = "";
            foreach (var data in outdata) {
                debugString += data + ", ";
            }
            // Debug.Log(debugString);
            
            // fillers cuz python expects 7 values
            // radian of approach angle
            float relativeRot = _myRb.rotation.eulerAngles.y - _otherRb.rotation.eulerAngles.y;
            
            // normalize rotation from -180 to 180
            if (relativeRot > 180) {
                relativeRot -= 360;
            }
            
            
            outdata[5] = relativeRot;
            
            _udpSocket.SendDataToPython(outdata);
            yield return new WaitForSeconds(1f / 19f);
        }
    }
    
    public float GetDistanceToCenter(VehicleController vehicleController) {
        if (vehicleController == null) {
            return 0;
        }
        return Vector3.Distance(_intersectionCenter.position, vehicleController.transform.position);
    } 
    
    public float GetDistanceBetween(VehicleController vehicleController1, VehicleController vehicleController2) {
        return Vector3.Distance(vehicleController1.transform.position, vehicleController2.transform.position);
    }
    
    public void SetSpeed(float speed) {
        _speed = speed;
    }
    
    public void SetSteering(float steering) {
        _myNetCtrl.SteeringInput = steering;
    }
    
    public float GetSpeed() {
        return _speed;
    }
    
    public bool IsFrontClear() {
        return triggerPlayerTracker.IsFrontClear();
    }
    
    
    
    
}
