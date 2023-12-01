using UnityEngine;

namespace DroneController
{
    [RequireComponent(typeof(DroneMovement))]
    public class PropellerMovement : MonoBehaviour
    {
        [Header("Local References:")]
        [SerializeField] private Transform[] _propellers = default;
        [Header("Settings:")]
        [SerializeField] private float _rotationSpeed = 3f;
        [SerializeField] private float _velocityMultiplier = .3f;

        private DroneMovement _droneMovement = default;

        private DroneMovement DroneMovement
        {
            get
            {
                if (_droneMovement == null)
                {
                    _droneMovement = GetComponent<DroneMovement>();
                }
                return _droneMovement;
            }
        }

        protected virtual void Update()
        {
            float calculatedRotationSpeed = _rotationSpeed + (DroneMovement.Velocity.magnitude * _velocityMultiplier);

            for (int i = 0; i < _propellers.Length; i++)
            {
                calculatedRotationSpeed *= (i % 2 == 0) ? 1 : -1;
                _propellers[i].Rotate(Vector3.up, calculatedRotationSpeed, Space.Self);
            }
        }
    }
}
