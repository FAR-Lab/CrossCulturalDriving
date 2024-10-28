using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SO_AVConfig : ScriptableObject
{
    public float SpeedP = 0.2f, SpeedI = 0.005f, SpeedD = 0.05f;
    public float SteeringP = 0.2f, SteeringI = 0.005f, SteeringD = 0.05f;
    public float ThrottleFeedforward = 0.1f;
    public float LookaheadDistance = 5f;
    
    public float LateralErrorWeight = 1.0f; 
    public float HeadingErrorWeight = 0.5f;
    
    public float YieldThreshold = 0.5f;
    
    public List<SO_FSMNodeContainer> nodeContainers;
}
