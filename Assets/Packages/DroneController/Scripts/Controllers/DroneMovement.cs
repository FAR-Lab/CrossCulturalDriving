using UnityEngine;

namespace DroneController
{
    [RequireComponent(typeof(Rigidbody))]

    public class DroneMovement : MonoBehaviour
    {
        [Header("Project References:")]
        [SerializeField] private DroneMovementData _droneMovementData = default;
        [Header("Local References:")]
        [SerializeField] private Transform _droneObject = default;

        // Component renferences.
        private Rigidbody _rigidbody = default;
        private InputManager _inputManager = default;

        // Calculation values.
        private Vector3 _smoothDampToStopVelocity = default;
        private float _currentRollAmount = default;
        private float _currentRollAmountVelocity = default;
        private float _currentPitchAmount = default;
        private float _currentPitchAmountVelocity = default;
        private float _currentYRotation = default;
        private float _targetYRotation = default;
        private float _targetYRotationVelocity = default;
        private float _currentUpForce = default;

        // Public properties.
        public float CurrentYRotation { get { return _currentYRotation; } }
        public Vector3 Velocity { get { return _rigidbody.velocity; } }

        // Private properties.
        public InputManager InputManager
        {
            get
            {
                if (_inputManager == null)
                {
                    _inputManager = InputManager.Instance;
                }
                return _inputManager;
            }
        }

        protected virtual void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
        }

        protected virtual void Start()
        {
            SetStartingRotation();
        }

        protected virtual void FixedUpdate()
        {
            ClampingSpeedValues();

            ThrottleForce(InputManager.ThrottleInput);
            RollForce(InputManager.RollInput);
            YawForce(InputManager.YawInput);
            PitchForce(InputManager.PitchInput);

            ApplyForces();
        }

        /// <summary>
        /// Fixes the starting rotation, sets the wanted and current rotation in the
        /// code so drone doesnt start with rotation of (0,0,0).
        /// </summary>
        private void SetStartingRotation()
        {
            _targetYRotation = transform.eulerAngles.y;
            _currentYRotation = transform.eulerAngles.y;
        }

        /// <summary>
        /// Applying upForce for hovering and keeping the drone in the air.
        /// Handles rotation and applies it here.
        /// Handles tilt values and applies it, gues where? here! :)
        /// </summary>
        public void ApplyForces()
        {
            _rigidbody.AddRelativeForce(Vector3.up * _currentUpForce);
            _rigidbody.rotation = Quaternion.Euler(new Vector3(0, _currentYRotation, 0));
            _rigidbody.angularVelocity = new Vector3(0, 0, 0);
            _droneObject.localRotation = Quaternion.Euler(new Vector3(_currentPitchAmount, 0, -_currentRollAmount));
        }

        /// <summary>
        /// Clamping speed values determined on what input is pressed
        /// </summary>
        public void ClampingSpeedValues()
        {
            _rigidbody.velocity = Vector3.ClampMagnitude(_rigidbody.velocity, Mathf.Lerp(_rigidbody.velocity.magnitude, _droneMovementData.MaximumPitchSpeed, Time.deltaTime * 5f));
            if (InputManager.Instance.IsInputIdle())
            {
                _rigidbody.velocity = Vector3.SmoothDamp(_rigidbody.velocity, Vector3.zero, ref _smoothDampToStopVelocity, _droneMovementData.SlowDownTime);
            }
        }

        /// <summary>
        /// Handling up down movement and applying needed force.
        /// </summary>
        public void ThrottleForce(float throttleInput)
        {
            float forceValue = (throttleInput > 0) ? _droneMovementData.UpwardMovementForce : (throttleInput < 0) ? _droneMovementData.DownwardMovementForce : 0f;
            _currentUpForce = _rigidbody.mass * 9.81f + throttleInput * forceValue;
        }

        /// <summary>
        /// Handling left right movement and appying forces, also handling the titls
        /// </summary>
        public void RollForce(float rollInput)
        {
            _rigidbody.AddRelativeForce(Vector3.right * rollInput * _droneMovementData.SidewardMovementForce);
            _currentRollAmount = Mathf.SmoothDamp(_currentRollAmount, _droneMovementData.MaximumRollAmount * rollInput, ref _currentRollAmountVelocity, _droneMovementData.PitchRollTiltSpeed);
        }

        /// <summary>
        /// Handling rotations
        /// </summary>
        public void YawForce(float yawInput)
        {
            _targetYRotation += yawInput * _droneMovementData.MaximumYawSpeed;
            _currentYRotation = Mathf.SmoothDamp(_currentYRotation, _targetYRotation, ref _targetYRotationVelocity, 0.25f);
        }

        /// <summary>
        /// Movement forwards and backwars and tilting
        /// </summary>
        public void PitchForce(float pitchInput)
        {
            _rigidbody.AddRelativeForce(Vector3.forward * pitchInput * _droneMovementData.ForwardMovementForce);
            _currentPitchAmount = Mathf.SmoothDamp(_currentPitchAmount, _droneMovementData.MaximumPitchAmount * pitchInput, ref _currentPitchAmountVelocity, _droneMovementData.PitchRollTiltSpeed);
        }
    }
}
