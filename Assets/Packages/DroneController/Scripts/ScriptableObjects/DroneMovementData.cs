using UnityEngine;

namespace DroneController
{
    [CreateAssetMenu(fileName = nameof(DroneMovementData), menuName = nameof(DroneController) + "/" + nameof(DroneMovementData))]
    public class DroneMovementData : ScriptableObject
    {
        public float MaximumPitchSpeed = 15f;
        public float MaximumRollSpeed = 10f;
        public float MaximumYawSpeed = 3f;
        [Space]
        public float IdleUpForce = 98.10001f;
        public float ForwardMovementForce = 750f;
        public float SidewardMovementForce = 450f;
        public float UpwardMovementForce = 450f;
        public float DownwardMovementForce = 250f;
        [Space]
        [Range(0, 90)] public float MaximumPitchAmount = 30f;
        [Range(0, 90)] public float MaximumRollAmount = 30f;
        [Space]
        [Range(0.0f, 1.0f)] public float PitchRollTiltSpeed = 0.1f;
        [Space]
        [Range(0.0f, 2.0f)] public float SlowDownTime = 0.95f;
    }
}
