using UnityEngine;
using UnityEngine.InputSystem;

namespace DroneController
{
    public class InputManager : MonoBehaviour
    {
        public static InputManager Instance;

        [SerializeField] private InputActionAsset _inputActionAsset = default;
        [SerializeField] private InputActionReference _inputPitch = default;
        [SerializeField] private InputActionReference _inputRoll = default;
        [SerializeField] private InputActionReference _inputYaw = default;
        [SerializeField] private InputActionReference _inputThrottle = default;

        [SerializeField] private float _pitchInput = default;
        [SerializeField] private float _rollInput = default;
        [SerializeField] private float _yawInput = default;
        [SerializeField] private float _throttleInput = default;

        public float PitchInput { get { return _pitchInput; } }
        public float RollInput { get { return _rollInput; } }
        public float YawInput { get { return _yawInput; } }
        public float ThrottleInput { get { return _throttleInput; } }

        private void Awake()
        {
            if (InputManager.Instance == null)
            {
                Instance = this;
            }
            else if (InputManager.Instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void OnEnable()
        {
            _inputActionAsset.Enable();

            _inputPitch.action.canceled += OnPitchInputChanged;
            _inputPitch.action.performed += OnPitchInputChanged;
            _inputPitch.action.started += OnPitchInputChanged;

            _inputRoll.action.canceled += OnRollInputChanged;
            _inputRoll.action.performed += OnRollInputChanged;
            _inputRoll.action.started += OnRollInputChanged;

            _inputYaw.action.canceled += OnYawInputChanged;
            _inputYaw.action.performed += OnYawInputChanged;
            _inputYaw.action.started += OnYawInputChanged;

            _inputThrottle.action.canceled += OnThrottleInputChanged;
            _inputThrottle.action.performed += OnThrottleInputChanged;
            _inputThrottle.action.started += OnThrottleInputChanged;
        }

        private void OnDisable()
        {
            _inputPitch.action.canceled -= OnPitchInputChanged;
            _inputPitch.action.performed -= OnPitchInputChanged;
            _inputPitch.action.started -= OnPitchInputChanged;

            _inputRoll.action.canceled -= OnRollInputChanged;
            _inputRoll.action.performed -= OnRollInputChanged;
            _inputRoll.action.started -= OnRollInputChanged;

            _inputYaw.action.canceled -= OnYawInputChanged;
            _inputYaw.action.performed -= OnYawInputChanged;
            _inputYaw.action.started -= OnYawInputChanged;

            _inputThrottle.action.canceled -= OnThrottleInputChanged;
            _inputThrottle.action.performed -= OnThrottleInputChanged;
            _inputThrottle.action.started -= OnThrottleInputChanged;

            _inputActionAsset.Disable();
        }

        public bool IsInputIdle()
        {
            return Mathf.Approximately(_pitchInput, 0f) && Mathf.Approximately(_rollInput, 0f) && Mathf.Approximately(_throttleInput, 0f);
        }

        private void SetInputValue(ref float axis, float value)
        {
            axis = value;
        }

        private void OnPitchInputChanged(InputAction.CallbackContext eventData)
        {
            SetInputValue(ref _pitchInput, eventData.ReadValue<float>());
        }

        private void OnRollInputChanged(InputAction.CallbackContext eventData)
        {
            SetInputValue(ref _rollInput, eventData.ReadValue<float>());
        }

        private void OnYawInputChanged(InputAction.CallbackContext eventData)
        {
            SetInputValue(ref _yawInput, eventData.ReadValue<float>());
        }

        private void OnThrottleInputChanged(InputAction.CallbackContext eventData)
        {
            SetInputValue(ref _throttleInput, eventData.ReadValue<float>());
        }
    }
}
