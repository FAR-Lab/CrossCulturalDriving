using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public struct AI_Drive_Component : IComponentData {
    public float maxMotorTorque;
    public float maxBrakeTorque;
    public float maxSteerAngle;
    public float maxSpeed;
    public float maxAcceleration;

    public float3 currentVelocity;
    public float currentSpeed;

    public float3 agentPosition;
    public float3 inverseTransformPoint;
    public LocalToWorld rbLocalToWorld;
    public float rb_agent_distance;
    public float dotProduct;
    public float3 worldDownVector;

    public float speedParameter;
    public float brakeParameter;
    public float steerParameter;
}
