using System.Collections;
using UnityEngine;

public class SC_AVContext : MonoBehaviour {
        
    [SerializeField]private VehicleController _otherCtrl;
    public VehicleController OtherCtrl => _otherCtrl;
    
    [SerializeField]private VehicleController _myCtrl;
    public VehicleController MyCtrl => _myCtrl;
    
    private Rigidbody _myRb => _myCtrl.GetComponent<Rigidbody>();
    private Rigidbody _otherRb => _otherCtrl.GetComponent<Rigidbody>();
    
    private Transform _intersectionCenter => FindObjectOfType<IntersectionCenter>().transform;
    public Transform IntersectionCenter => _intersectionCenter;
    
    private float _speed;
    public TriggerPlayerTracker triggerPlayerTracker;
    private UdpSocket _udpSocket;
    
    // if data is greater than yield threshold, yield
    [SerializeField] private float YieldThreshold = 0.2f;
    private float _yieldPossibility;
    public float YieldPossibility => _yieldPossibility;
    
    public void Initialize() {
        _myCtrl = GetComponent<VehicleController>();
        
        Interactable_Object obj = ConnectionAndSpawning.Singleton.GetInteractableObject_For_Participant(ParticipantOrder.A);

        if (obj != null) {
            _otherCtrl = obj.GetComponent<VehicleController>();
        }
        
        _udpSocket = gameObject.AddComponent<UdpSocket>();
        _udpSocket.GotNewAiData += HandleReceivedData;
        
        StartCoroutine(SendArtificialData(_otherCtrl));
    }
    
    private void OnDestroy() {
        _udpSocket.GotNewAiData -= HandleReceivedData;
    }
    
    private void HandleReceivedData(float[] data)
    {
        _yieldPossibility = data[1];
        Debug.Log("YieldPossibility: " + _yieldPossibility);
    }

    public bool ShouldYield() {
        return _yieldPossibility > YieldThreshold;
    }
    
    private IEnumerator SendArtificialData(VehicleController otherCar) {
        Vector3 distance, relVelocity;
        float dot, rel_pos_magnitude, approachRate, relativeRotation;
        
        yield return new WaitForSeconds(0.1f);
        float[] outdata = new float[7];

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
            // 1 : "Rel_Pos_Magnitude"
            outdata[1] = rel_pos_magnitude;
            // "1_Head_Center_Distance", 
            outdata[2] = (_myRb.position-IntersectionCenter.position).magnitude; 
            // "2_Head_Center_Distance", 
            outdata[3] = (_otherRb.position-IntersectionCenter.position).magnitude;
            // "Filtered_2_Head_Velocity_Total"
            outdata[4] = _otherRb.velocity.magnitude;
            
            // debug log all the data in one line
            string debugString = "";
            foreach (var data in outdata) {
                debugString += data + ", ";
            }
            // Debug.Log(debugString);
            
            // fillers cuz python expects 7 values
            outdata[5] = 0;
            outdata[6] = 0;
            
            _udpSocket.SendDataToPython(outdata);
            yield return new WaitForSeconds(1f / 18f);
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
    
    public float GetSpeed() {
        return _speed;
    }
    
    public bool IsFrontClear() {
        return triggerPlayerTracker.IsFrontClear();
    }
    
    
    
    
}
